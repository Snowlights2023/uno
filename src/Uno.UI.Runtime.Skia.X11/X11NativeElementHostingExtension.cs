using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
namespace Uno.WinUI.Runtime.Skia.X11;

// https://www.x.org/releases/X11R7.6/doc/xextproto/shape.html
// Thanks to Jörg Seebohn for providing an example on how to use X SHAPE
// https://gist.github.com/je-so/903479/834dfd78705b16ec5f7bbd10925980ace4049e17
internal partial class X11NativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
	// private static string SampleVideoLink = "https://uno-assets.platform.uno/tests/uno/big_buck_bunny_720p_5mb.mp4";
	private static string SampleVideoLink = "/home/ramez/Downloads/big_buck_bunny_720p_5mb.mp4";
#pragma warning restore CS0414 // Field is assigned but its value is never used

	private static Dictionary<X11XamlRootHost, HashSet<X11NativeElementHostingExtension>> _hostToNativeElementHosts = new();
	private Rect? _lastFinalRect;
	private Rect? _lastArrangeRect;
	private Rect? _lastClipRect;
	private XamlRoot? _xamlRoot;
	private X11Window? _content;
	private bool _layoutDirty = true;
	private bool? _xShapesPresent;
	private ContentPresenter _presenter;

	public X11NativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	internal static IEnumerable<XRectangle> GetNativeElementRects(X11XamlRootHost host)
	{
		if (_hostToNativeElementHosts.TryGetValue(host, out var set))
		{
			foreach (var hostingExtension in set)
			{
				if (hostingExtension._lastFinalRect is { } rect)
				{
					yield return new XRectangle
					{
						X = (short)rect.X,
						Y = (short)rect.Y,
						H = (short)rect.Height,
						W = (short)rect.Width
					};
				}
			}
		}
	}

	public bool IsNativeElement(object content)
	{
		if (content is not X11Window x11Window)
		{
			return false;
		}

		using var _1 = X11Helper.XLock(x11Window.Display);

		var _3 = XLib.XQueryTree(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), out IntPtr root, out _, out var children, out _);
		XLib.XFree(children);

		// _NET_CLIENT_LIST only identifies top-level windows, not subwindows.
		var status = XLib.XGetWindowProperty(
			x11Window.Display,
			root,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_CLIENT_LIST),
			0,
			new IntPtr(0x7fffffff),
			false,
			X11Helper.AnyPropertyType,
			out _,
			out _,
			out var length,
			out IntPtr _,
			out IntPtr windowArray);

		if (status == X11Helper.Success)
		{
			unsafe
			{
				var span = new Span<IntPtr>(windowArray.ToPointer(), (int)length);
				foreach (var window in span)
				{
					if (window == x11Window.Window)
					{
						return true;
					}
				}
			}
		}

		return FindWindowById(x11Window.Display, x11Window.Window, root) != IntPtr.Zero;
	}
	public void AttachNativeElement(XamlRoot owner, object content)
	{
		Debug.Assert(!IsNativeElementAttached(owner, content));
		if (content is X11Window x11Window
			&& X11Manager.XamlRootMap.GetHostForRoot(owner) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);

			// this seems to be necessary or else the WM will keep detaching the subwindow
			XWindowAttributes attributes = default;
			var _2 = XLib.XGetWindowAttributes(x11Window.Display, x11Window.Window, ref attributes);
			attributes.override_direct = /* True */ 1;

			unsafe
			{
				IntPtr attr = Marshal.AllocHGlobal(Marshal.SizeOf(attributes));
				Marshal.StructureToPtr(attributes, attr, false);
				X11Helper.XChangeWindowAttributes(x11Window.Display, x11Window.Window, (IntPtr)XCreateWindowFlags.CWOverrideRedirect, (XSetWindowAttributes*)attr.ToPointer());
				Marshal.FreeHGlobal(attr);
			}

			var _3 = X11Helper.XReparentWindow(x11Window.Display, x11Window.Window, host.RootX11Window.Window, 0, 0);
			XLib.XSync(x11Window.Display, false); // XSync is necessary after XReparent for unknown reasons

			using var _4 = X11Helper.XLock(x11Window.Display);
			var _5 = X11Helper.XRaiseWindow(host.TopX11Window.Display, host.TopX11Window.Window);

			if (!_hostToNativeElementHosts.TryGetValue(host, out var set))
			{
				set = _hostToNativeElementHosts[host] = new HashSet<X11NativeElementHostingExtension>();
			}
			set.Add(this);

			_xamlRoot = owner;
			_content = x11Window;
			
			owner.InvalidateRender += UpdateLayout;
		}
		else
		{
			throw new InvalidOperationException($"{nameof(AttachNativeElement)} called in an invalid state.");
		}
	}

	public void DetachNativeElement(XamlRoot owner, object content)
	{
		Debug.Assert(IsNativeElementAttached(owner, content));
		
		if (content is X11Window x11Window
			&& X11Manager.XamlRootMap.GetHostForRoot(owner) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);
			var _2 = XLib.XQueryTree(x11Window.Display, x11Window.Window, out IntPtr root, out _, out var children, out _);
			XLib.XFree(children);
			var _3 = X11Helper.XReparentWindow(x11Window.Display, x11Window.Window, root, 0, 0);
			XLib.XSync(x11Window.Display, false);

			var set = _hostToNativeElementHosts[host];
			set.Remove(this);
			if (set.Count == 0)
			{
				_hostToNativeElementHosts.Remove(host);
			}

			_lastClipRect = null;
			_lastArrangeRect = null;
			_lastFinalRect = null;
			_xamlRoot = null;
			_content = null;

			owner.InvalidateRender -= UpdateLayout;
			host.QueueUpdateTopWindowClipRect();
		}
		else
		{
			 throw new InvalidOperationException($"{nameof(DetachNativeElement)} called in an invalid state.");
		}
	}

	public void ArrangeNativeElement(XamlRoot owner, object content, Rect arrangeRect, Rect clipRect)
	{
		_lastArrangeRect = arrangeRect;
		_lastClipRect = clipRect;
		_layoutDirty = true;
		// we don't update the layout right now. We wait for the next render to happen, as
		// xlib calls are expensive and it's better to update the layout once at the end when multiple arrange
		// calls are fired sequentially.
	}
	
	private void UpdateLayout()
	{
		if (!_layoutDirty)
		{
			return;
		}
		_layoutDirty = false;
		if (_content is { } x11Window &&
			_lastArrangeRect is { } arrangeRect &&
			_lastClipRect is { } clipRect &&
			_xamlRoot is { } xamlRoot &&
			X11Manager.XamlRootMap.GetHostForRoot(xamlRoot) is X11XamlRootHost host)
		{
			using var _1 = X11Helper.XLock(x11Window.Display);
			if (arrangeRect.Width <= 0 || arrangeRect.Height <= 0)
			{
				arrangeRect.Size = new Size(1, 1);
			}
			var _2 = XLib.XResizeWindow(x11Window.Display, x11Window.Window, (int)arrangeRect.Width, (int)arrangeRect.Height);
			var _3 = X11Helper.XMoveWindow(x11Window.Display, x11Window.Window, (int)arrangeRect.X, (int)arrangeRect.Y);

			_xShapesPresent ??= X11Helper.XShapeQueryExtension(x11Window.Display, out _, out _);
			if (_xShapesPresent.Value)
			{
				var region = X11Helper.CreateRegion((short)clipRect.Left, (short)clipRect.Top, (short)clipRect.Width, (short)clipRect.Height);
				using var _4 = Disposable.Create(() => X11Helper.XDestroyRegion(region));
				X11Helper.XShapeCombineRegion(x11Window.Display, x11Window.Window, X11Helper.ShapeBounding, 0, 0, region, X11Helper.ShapeSet);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Unable to clip an embedded X11 window, the X server doesn't support the X Nonrectangular Window Shape Extension Protocol.");
				}
			}

			XLib.XSync(x11Window.Display, false);

			var clipInGlobalCoordinates = new Rect(
				arrangeRect.X + clipRect.X,
				arrangeRect.Y + clipRect.Y,
				clipRect.Width,
				clipRect.Height);
			_lastFinalRect = arrangeRect.IntersectWith(clipInGlobalCoordinates);;

			host.QueueUpdateTopWindowClipRect();
		}
	}

	public Size MeasureNativeElement(XamlRoot owner, object content, Size childMeasuredSize, Size availableSize) => availableSize;

	public bool IsNativeElementAttached(XamlRoot owner, object nativeElement)
	{
		// Querying the X server every time is really expensive, so let's not do that.
		// if (nativeElement is X11Window x11Window
		// 	&& X11Manager.XamlRootMap.GetHostForRoot(owner) is X11XamlRootHost host)
		// {
		// 	using var _1 = X11Helper.XLock(x11Window.Display);
		// 	var _2 = XLib.XQueryTree(x11Window.Display, x11Window.Window, out _, out IntPtr parent, out var children, out _);
		// 	XLib.XFree(children);
		// 	return parent == host.RootX11Window.Window;
		// }
		//
		// return false;

		return _content == (X11Window?)nativeElement && owner == _xamlRoot;
	}

	public void ChangeNativeElementVisibility(XamlRoot owner, object content, bool visible)
	{
		if (content is X11Window x11Window)
		{
			if (visible)
			{
				var _3 = XLib.XMapWindow(x11Window.Display, x11Window.Window);
			}
			else
			{
				var _3 = X11Helper.XUnmapWindow(x11Window.Display, x11Window.Window);
			}
		}
	}

	// This doesn't seem to work as most (all?) WMs won't change the opacity for subwindows, only top-level windows
	public void ChangeNativeElementOpacity(XamlRoot owner, object content, double opacity)
	{
		// if (IsNativeElementAttached(owner, content) && content is X11Window x11Window)
		// {
		// 	// The spec requires a value between 0 and max int, not 0 and 1
		// 	var actualOpacity = (IntPtr)(opacity * uint.MaxValue);
		//
		// 	// if (opacity == 1)
		// 	// {
		// 	// 	XLib.XDeleteProperty(
		// 	// 		x11Window.Display,
		// 	// 		x11Window.Window,
		// 	// 		X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_WINDOW_OPACITY));
		// 	// }
		// 	// else
		// 	{
		// 		var tmp = new IntPtr[]
		// 		{
		// 			actualOpacity
		// 		};
		// 		XLib.XChangeProperty(
		// 			x11Window.Display,
		// 			x11Window.Window,
		// 			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_WINDOW_OPACITY),
		// 			X11Helper.GetAtom(x11Window.Display, X11Helper.XA_CARDINAL),
		// 			32,
		// 			PropertyMode.Replace,
		// 			actualOpacity,
		// 			1);
		// 	}
		// }
	}
}
