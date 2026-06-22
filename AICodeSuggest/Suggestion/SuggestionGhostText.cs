using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using AICodeSuggest.Options;

namespace AICodeSuggest.Suggestion
{
    internal class SuggestionGhostText
    {
        private readonly IWpfTextView _textView;
        private readonly IAdornmentLayer _layer;
        private readonly SuggestionTagger _tagger;
        private UIElement _currentAdornment;
        private int _maxDisplayLines = 5;

        public SuggestionGhostText(IWpfTextView textView, SuggestionTagger tagger)
        {
            _textView = textView;
            _tagger = tagger;
            _layer = textView.GetAdornmentLayer(PredefinedAdornmentLayers.Text);

            _tagger.TagsChanged += OnTagsChanged;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.Closed += OnTextViewClosed;
        }

        private void OnTagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            RemoveAdornment();
            RenderSuggestion();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_currentAdornment != null)
            {
                RemoveAdornment();
                RenderSuggestion();
            }
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            RemoveAdornment();
            _tagger.TagsChanged -= OnTagsChanged;
            _textView.LayoutChanged -= OnLayoutChanged;
            _textView.Closed -= OnTextViewClosed;
        }

        private void RenderSuggestion()
        {
            var suggestion = _tagger.CurrentSuggestion;
            if (string.IsNullOrEmpty(suggestion)) return;

            var acceptedCount = _tagger.AcceptedCharCount;
            var remaining = suggestion.Substring(acceptedCount);
            if (string.IsNullOrEmpty(remaining)) return;

            var pos = _tagger.SuggestionPosition;
            if (!pos.HasValue) return;

            var caretPoint = pos.Value.TranslateTo(_textView.TextSnapshot, PointTrackingMode.Positive);
            var startLine = _textView.GetTextViewLineContainingBufferPosition(caretPoint);
            if (startLine == null) return;

            var package = AICodeSuggestPackage.Instance;
            var options = package?.GetDialogPage(typeof(GeneralOptions)) as GeneralOptions;
            _maxDisplayLines = options?.MaxSuggestionDisplayLines ?? 5;

            var rawLines = remaining.Split('\n');
            int displayLineCount = Math.Min(rawLines.Length, _maxDisplayLines);

            var fontFamily = _textView.FormattedLineSource?.DefaultTextProperties?.Typeface?.FontFamily
                ?? new FontFamily("Consolas");
            var fontSize = _textView.FormattedLineSource?.DefaultTextProperties?.FontRenderingEmSize ?? 14;
            var lineHeight = _textView.LineHeight;

            var bounds = startLine.GetCharacterBounds(caretPoint);

            var container = new Canvas { IsHitTestVisible = false, ClipToBounds = false };

            for (int i = 0; i < displayLineCount; i++)
            {
                var lineText = rawLines[i];
                if (i == displayLineCount - 1 && displayLineCount < rawLines.Length)
                    lineText += "...";

                if (lineText.Length > 200)
                    lineText = lineText.Substring(0, 197) + "...";

                var tb = new TextBlock
                {
                    Text = lineText,
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    FontStyle = FontStyles.Italic,
                    Opacity = 0.7,
                    FontFamily = fontFamily,
                    FontSize = fontSize,
                    TextTrimming = TextTrimming.None
                };

                container.Children.Add(tb);

                if (i == 0)
                {
                    Canvas.SetLeft(tb, bounds.Right - _textView.ViewportLeft);
                    Canvas.SetTop(tb, startLine.TextTop - _textView.ViewportTop);
                }
                else
                {
                    Canvas.SetLeft(tb, 0);
                    Canvas.SetTop(tb, startLine.TextTop - _textView.ViewportTop + i * lineHeight);
                }
            }

            // 操作提示
            var hintText = acceptedCount > 0
                ? "Tab: next word  |  Ctrl+Enter: accept all  |  Esc: cancel"
                : "Tab: accept  |  Ctrl+Enter: accept all  |  Esc: cancel";
            var hint = new TextBlock
            {
                Text = hintText,
                Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                FontStyle = FontStyles.Italic,
                Opacity = 0.35,
                FontFamily = fontFamily,
                FontSize = fontSize * 0.75
            };
            container.Children.Add(hint);
            Canvas.SetLeft(hint, 0);
            Canvas.SetTop(hint, startLine.TextTop - _textView.ViewportTop + displayLineCount * lineHeight + 2);

            _currentAdornment = container;
            _layer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, container, null);
        }

        private void RemoveAdornment()
        {
            if (_currentAdornment != null)
            {
                _layer.RemoveAdornment(_currentAdornment);
                _currentAdornment = null;
            }
        }
    }
}
