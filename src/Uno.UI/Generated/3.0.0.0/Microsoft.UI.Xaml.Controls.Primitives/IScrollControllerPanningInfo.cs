// <auto-generated>
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls.Primitives
{
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial interface IScrollControllerPanningInfo
	{
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsRailEnabled
		{
			get;
		}
#endif
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Microsoft.UI.Xaml.Controls.Orientation PanOrientation
		{
			get;
		}
#endif
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Microsoft.UI.Xaml.UIElement PanningElementAncestor
		{
			get;
		}
#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.IsRailEnabled.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.PanOrientation.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.PanningElementAncestor.get
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void SetPanningElementExpressionAnimationSources(global::Microsoft.UI.Composition.CompositionPropertySet propertySet, string minOffsetPropertyName, string maxOffsetPropertyName, string offsetPropertyName, string multiplierPropertyName);
#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.Changed.add
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.Changed.remove
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.PanRequested.add
		// Forced skipping of method Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo.PanRequested.remove
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		event global::Windows.Foundation.TypedEventHandler<global::Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo, object> Changed;
#endif
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		event global::Windows.Foundation.TypedEventHandler<global::Microsoft.UI.Xaml.Controls.Primitives.IScrollControllerPanningInfo, global::Microsoft.UI.Xaml.Controls.Primitives.ScrollControllerPanRequestedEventArgs> PanRequested;
#endif
	}
}
