﻿#nullable enable

using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal class TextBoxView
	{
		private ITextBoxViewExtension _textBoxExtension = null;

		private readonly WeakReference<TextBox> _textBox;

		public TextBoxView(TextBox textBox)
		{
			_textBox = new WeakReference<TextBox>(textBox);
			if (!ApiExtensibility.CreateInstance(this, out _textBoxExtension))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning(
						"No TextBoxView implementation is available " +
						"for this Skia target. Functionality will be limited.");
				}
			}
		}

		public TextBox? TextBox
		{
			get
			{
				if (_textBox.TryGetTarget(out var target))
				{
					return target;
				}
				return null;
			}
		}

		public TextBlock DisplayBlock { get; } = new TextBlock();

		internal void SetTextNative(string text) => DisplayBlock.Text = text;

		internal void OnForegroundChanged(Brush brush) => DisplayBlock.Foreground = brush;

		internal void OnFocusStateChanged(FocusState focusState)
		{
			if (focusState != FocusState.Unfocused)
			{
				DisplayBlock.Opacity = 0;
				_textBoxExtension.StartEntry();
			}
			else
			{
				_textBoxExtension.EndEntry();
				DisplayBlock.Opacity = 1;
			}
		}

		internal void UpdateText(string newText)
		{
			var textBox = _textBox?.GetTarget();
			if (textBox != null)
			{
				var text = textBox.ProcessTextInput(newText);
				SetTextNative(newText);
			}
		}

		public void UpdateMaxLength() => _textBoxExtension.UpdateNativeView();
	}
}
