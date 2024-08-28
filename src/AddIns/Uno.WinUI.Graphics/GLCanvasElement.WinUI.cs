﻿#if WINAPPSDK

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.OpenGL;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.WinUI.Graphics;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on skia-based targets and when running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets.
/// </remarks>
public abstract partial class GLCanvasElement
{

	private readonly Window _window;
	private readonly WriteableBitmap _backBuffer;
	private readonly Image _image;
	private readonly ScaleTransform _scaleTransform;

	private nint _hwnd;
	private nint _hdc;
	private int _pixelFormat;
	private nint _glContext;

	/// <param name="width">The width of the backing framebuffer.</param>
	/// <param name="height">The height of the backing framebuffer.</param>
	/// <param name="window">The window that this element will belong to.</param>
	protected GLCanvasElement(uint width, uint height, Window window)
	{
		_width = width;
		_height = height;
		_window = window;

		_backBuffer = new WriteableBitmap((int)width, (int)height);

		_scaleTransform = new ScaleTransform { ScaleX = 1, ScaleY = -1 }; // because OpenGL coordinates go bottom-to-top
		_image = new Image
		{
			Source = _backBuffer,
			RenderTransform = _scaleTransform
		};
		Content = _image;

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		SizeChanged += OnSizeChanged;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SetupOpenGlContext();
		OnLoadedShared();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e) => OnUnloadedShared();

	private void SetupOpenGlContext()
	{
		_hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);

		_hdc = NativeMethods.GetDC(_hwnd);

		NativeMethods.PIXELFORMATDESCRIPTOR pfd = new();
        pfd.nSize = (ushort)Marshal.SizeOf(pfd);
        pfd.nVersion = 1;
        pfd.dwFlags = NativeMethods.PFD_DRAW_TO_WINDOW | NativeMethods.PFD_SUPPORT_OPENGL | NativeMethods.PFD_DOUBLEBUFFER;
        pfd.iPixelType = NativeMethods.PFD_TYPE_RGBA;
        pfd.cColorBits = 32;
        pfd.cRedBits = 8;
        pfd.cGreenBits = 8;
        pfd.cBlueBits = 8;
        pfd.cAlphaBits = 8;
        pfd.cDepthBits = 16;
        pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
        pfd.iLayerType = NativeMethods.PFD_MAIN_PLANE;

        _pixelFormat = NativeMethods.ChoosePixelFormat(_hdc, ref pfd);

        // To inspect the chosen pixel format:
        // NativeMethods.PIXELFORMATDESCRIPTOR temp_pfd = default;
        // NativeMethods.DescribePixelFormat(_hdc, _pixelFormat, (uint)Marshal.SizeOf<NativeMethods.PIXELFORMATDESCRIPTOR>(), ref temp_pfd);

        if (_pixelFormat == 0)
        {
	        if (this.Log().IsEnabled(LogLevel.Error))
	        {
		        this.Log().Error($"ChoosePixelFormat failed");
	        }
	        throw new InvalidOperationException("ChoosePixelFormat failed");
        }

        if (NativeMethods.SetPixelFormat(_hdc, _pixelFormat, ref pfd) == 0)
        {
	        if (this.Log().IsEnabled(LogLevel.Error))
	        {
		        this.Log().Error($"SetPixelFormat failed");
	        }
	        throw new InvalidOperationException("ChoosePixelFormat failed");
        }

        _glContext = NativeMethods.wglCreateContext(_hdc);

        if (_glContext == IntPtr.Zero)
        {
	        if (this.Log().IsEnabled(LogLevel.Error))
	        {
		        this.Log().Error($"wglCreateContext failed");
	        }
	        throw new InvalidOperationException("ChoosePixelFormat failed");
        }

        _gl = GL.GetApi(new WinUINativeContext());
	}

	public partial void Invalidate() => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Render);

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		_scaleTransform.CenterY = args.NewSize.Height / 2;
	}

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	private class WinUINativeContext : INativeContext
	{
		private readonly UnmanagedLibrary _l;

		public WinUINativeContext()
		{
			_l = new UnmanagedLibrary("opengl32.dll");
			if (_l.Handle == IntPtr.Zero)
			{
				throw new PlatformNotSupportedException("Unable to load opengl32.dll. Make sure you're running on a system with OpenGL support");
			}
		}

		public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
		{
			if (_l.TryLoadFunction(proc, out addr))
			{
				return true;
			}

			addr = NativeMethods.wglGetProcAddress(proc);
			return addr != IntPtr.Zero;
		}

		public nint GetProcAddress(string proc, int? slot = null)
		{
			if (TryGetProcAddress(proc, out var address, slot))
			{
				return address;
			}

			throw new InvalidOperationException("No function was found with the name " + proc + ".");
		}

		public void Dispose() => _l.Dispose();
	}

	private static class NativeMethods
    {
    	[DllImport("user32.dll")]
    	internal static extern IntPtr GetDC(IntPtr hWnd);

    	[DllImport("gdi32.dll")]
    	internal static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

    	[DllImport("gdi32.dll")]
    	internal static extern int SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR ppfd);

    	[DllImport("gdi32.dll")]
    	internal static extern int DescribePixelFormat(IntPtr hdc, int iPixelFormat, uint nBytes, ref PIXELFORMATDESCRIPTOR ppfd);

	    [DllImport("opengl32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
	    public static extern IntPtr wglGetProcAddress(string functionName);

    	[DllImport("opengl32.dll")]
    	internal static extern IntPtr wglCreateContext(IntPtr hdc);

    	[DllImport("opengl32.dll")]
    	public static extern IntPtr wglGetCurrentDC();

	    [DllImport("opengl32.dll")]
	    public static extern IntPtr wglGetCurrentContext();

    	[DllImport("opengl32.dll")]
    	internal static extern int wglDeleteContext(IntPtr hglrc);

    	[DllImport("opengl32.dll")]
    	internal static extern int wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

    	[StructLayout(LayoutKind.Sequential)]
    	internal struct PIXELFORMATDESCRIPTOR
    	{
    		public ushort nSize;
    		public ushort nVersion;
    		public uint dwFlags;
    		public byte iPixelType;
    		public byte cColorBits;
    		public byte cRedBits;
    		public byte cRedShift;
    		public byte cGreenBits;
    		public byte cGreenShift;
    		public byte cBlueBits;
    		public byte cBlueShift;
    		public byte cAlphaBits;
    		public byte cAlphaShift;
    		public byte cAccumBits;
    		public byte cAccumRedBits;
    		public byte cAccumGreenBits;
    		public byte cAccumBlueBits;
    		public byte cAccumAlphaBits;
    		public byte cDepthBits;
    		public byte cStencilBits;
    		public byte cAuxBuffers;
    		public byte iLayerType;
    		public byte bReserved;
    		public uint dwLayerMask;
    		public uint dwVisibleMask;
    		public uint dwDamageMask;
    	}

    	internal const int PFD_DRAW_TO_WINDOW = 0x00000004;
    	internal const int PFD_SUPPORT_OPENGL = 0x00000020;
    	internal const int PFD_DOUBLEBUFFER = 0x00000001;
    	internal const int PFD_TYPE_RGBA = 0;

    	internal const int PFD_MAIN_PLANE = 0;
    	internal const int WGL_SWAP_MAIN_PLANE = 1;
    }
}

#endif
