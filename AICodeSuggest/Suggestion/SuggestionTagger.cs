using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using AICodeSuggest.Options;

namespace AICodeSuggest.Suggestion
{
    internal class SuggestionTagger : ITagger<SuggestionTag>, IDisposable
    {
        private readonly IWpfTextView _textView;
        private readonly ITextBuffer _buffer;
        private CancellationTokenSource _debounceCts;
        private string _currentSuggestion;
        private SnapshotPoint? _suggestionPosition;
        private int _acceptedCharCount;
        private bool _inPartialAccept;
        private bool _disposed;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public string CurrentSuggestion => _currentSuggestion;
        public SnapshotPoint? SuggestionPosition => _suggestionPosition;
        public int AcceptedCharCount => _acceptedCharCount;

        public SuggestionTagger(IWpfTextView textView)
        {
            _textView = textView;
            _buffer = textView.TextBuffer;
            _buffer.Changed += OnTextBufferChanged;
            _textView.Properties.GetOrCreateSingletonProperty(typeof(SuggestionTagger), () => this);
        }

        public System.Collections.Generic.IEnumerable<ITagSpan<SuggestionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (string.IsNullOrEmpty(_currentSuggestion) || !_suggestionPosition.HasValue)
                yield break;

            var pos = _suggestionPosition.Value;
            if (pos.Snapshot != spans[0].Snapshot)
            {
                pos = pos.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);
            }

            yield return new TagSpan<SuggestionTag>(new SnapshotSpan(pos, 0), new SuggestionTag());
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if (_inPartialAccept) return;
            ClearSuggestion();
            ScheduleTrigger();
        }

        private void ScheduleTrigger()
        {
            var package = AICodeSuggestPackage.Instance;
            if (package == null) return;

            var options = package.GetDialogPage(typeof(GeneralOptions)) as GeneralOptions;
            if (options == null || !options.EnableSuggestions) return;

            _debounceCts?.Cancel();
            var cts = new CancellationTokenSource();
            _debounceCts = cts;
            var token = cts.Token;

            var delayMs = options.TriggerDelayMs;
            if (delayMs < 100) delayMs = 100;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    if (token.IsCancellationRequested) return;

                    var engine = AICodeSuggestPackage.Instance?.SuggestionEngine;
                    if (engine == null) return;

                    var suggestion = await engine.GenerateSuggestionAsync(token);
                    if (token.IsCancellationRequested) return;
                    if (suggestion.IsEmpty)
                    {
                        if (suggestion.HasReason)
                            AICodeSuggestPackage.Instance?.LogService?.Info($"建议为空，原因: {suggestion.Reason}");
                        return;
                    }

                    _currentSuggestion = suggestion.Text?.Replace("\r\n", "\n").Replace("\r", "\n") ?? string.Empty;
                    _acceptedCharCount = 0;
                    _suggestionPosition = _textView.Caret.Position.BufferPosition;

                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(_suggestionPosition.Value, 0)));
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    AICodeSuggestPackage.Instance?.LogService?.Error($"建议生成失败: {ex.Message}");
                }
            }, token);
        }

        public void ClearSuggestion()
        {
            _debounceCts?.Cancel();
            if (string.IsNullOrEmpty(_currentSuggestion) && _acceptedCharCount == 0) return;

            _currentSuggestion = null;
            _suggestionPosition = null;
            _acceptedCharCount = 0;
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(_textView.TextSnapshot, 0, 0)));
        }

        public void AcceptNextWord()
        {
            if (string.IsNullOrEmpty(_currentSuggestion)) return;

            if (_acceptedCharCount >= _currentSuggestion.Length)
            {
                ClearSuggestion();
                return;
            }

            var remaining = _currentSuggestion.Substring(_acceptedCharCount);
            var (word, consumed) = ExtractNextWord(remaining);

            if (string.IsNullOrEmpty(word) || consumed == 0)
            {
                ClearSuggestion();
                return;
            }

            var caretPos = _textView.Caret.Position.BufferPosition;

            _inPartialAccept = true;
            try
            {
                var edit = _buffer.CreateEdit();
                edit.Insert(caretPos.Position, word);
                edit.Apply();
            }
            finally
            {
                _inPartialAccept = false;
            }

            _acceptedCharCount += consumed;
            _suggestionPosition = _textView.Caret.Position.BufferPosition;

            if (_acceptedCharCount >= _currentSuggestion.Length)
            {
                ClearSuggestion();
                return;
            }

            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(_suggestionPosition.Value, 0)));
        }

        public void AcceptAllRemaining()
        {
            if (string.IsNullOrEmpty(_currentSuggestion)) return;

            if (_acceptedCharCount >= _currentSuggestion.Length)
            {
                ClearSuggestion();
                return;
            }

            var remaining = _currentSuggestion.Substring(_acceptedCharCount);
            var caretPos = _textView.Caret.Position.BufferPosition;

            _inPartialAccept = true;
            try
            {
                var edit = _buffer.CreateEdit();
                edit.Insert(caretPos.Position, remaining);
                edit.Apply();
            }
            finally
            {
                _inPartialAccept = false;
            }

            ClearSuggestion();
        }

        private static (string word, int consumed) ExtractNextWord(string remaining)
        {
            if (string.IsNullOrEmpty(remaining)) return (string.Empty, 0);

            int i = 0;

            // 前导空白（同行，不含换行符）
            while (i < remaining.Length && remaining[i] != '\n' && char.IsWhiteSpace(remaining[i]))
                i++;

            // 遇到换行符：消费换行符 + 下一行的缩进空白
            if (i < remaining.Length && remaining[i] == '\n')
            {
                i++; // 消费 \n
                while (i < remaining.Length && remaining[i] != '\n' && char.IsWhiteSpace(remaining[i]))
                    i++;
                return (remaining.Substring(0, i), i);
            }

            // 消费单词（非空白连续字符）
            while (i < remaining.Length && !char.IsWhiteSpace(remaining[i]))
                i++;

            // 消费同行尾随空白
            while (i < remaining.Length && remaining[i] != '\n' && char.IsWhiteSpace(remaining[i]))
                i++;

            return (remaining.Substring(0, i), i);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _debounceCts?.Cancel();
            _buffer.Changed -= OnTextBufferChanged;
        }
    }
}
