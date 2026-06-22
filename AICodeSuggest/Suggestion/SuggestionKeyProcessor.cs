using System.Windows.Input;

namespace AICodeSuggest.Suggestion
{
    internal class SuggestionKeyHandler
    {
        private readonly SuggestionTagger _tagger;

        public SuggestionKeyHandler(SuggestionTagger tagger)
        {
            _tagger = tagger;
        }

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (string.IsNullOrEmpty(_tagger.CurrentSuggestion))
                return;

            // Tab（无修饰键）→ 逐词接受
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                _tagger.AcceptNextWord();
                e.Handled = true;
                return;
            }

            // Shift+Tab 或 Ctrl+Enter → 全量接受
            if ((e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Shift) ||
                (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control))
            {
                _tagger.AcceptAllRemaining();
                e.Handled = true;
                return;
            }

            // Esc → 取消
            if (e.Key == Key.Escape)
            {
                _tagger.ClearSuggestion();
                e.Handled = true;
                return;
            }

            // 导航键：放行，保留建议
            if (IsNavigationKey(e.Key))
                return;

            // 其他按键：清除建议
            _tagger.ClearSuggestion();
        }

        private static bool IsNavigationKey(Key key)
        {
            return (key >= Key.Left && key <= Key.Down)
                || key == Key.Home || key == Key.End
                || key == Key.PageUp || key == Key.PageDown
                || key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin;
        }
    }
}
