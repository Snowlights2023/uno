#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2WebMessageReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2WebMessageReceivedEventArgs.Source is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2WebMessageReceivedEventArgs.Source");
			}
		}
		#endif
		// Skipping already declared property WebMessageAsJson
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs.Source.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs.WebMessageAsJson.get
		// Skipping already declared method Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs.TryGetWebMessageAsString()
	}
}