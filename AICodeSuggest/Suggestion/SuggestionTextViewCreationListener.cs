using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace AICodeSuggest.Suggestion
{
    [Export(typeof(ITextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class SuggestionTextViewCreationListener : ITextViewCreationListener
    {
        public void TextViewCreated(ITextView textView)
        {
            if (!(textView is IWpfTextView wpfView))
                return;

            // 始终创建 tagger，不依赖 AICodeSuggestPackage.Instance 是否就绪。
            // SuggestionTagger 在触发时延迟检查 Instance，从而兼容异步后台加载。
            var tagger = new SuggestionTagger(wpfView);
            var ghostText = new SuggestionGhostText(wpfView, tagger);
            var keyHandler = new SuggestionKeyHandler(tagger);

            wpfView.VisualElement.PreviewKeyDown += keyHandler.OnPreviewKeyDown;

            wpfView.Closed += (s, e) =>
            {
                wpfView.VisualElement.PreviewKeyDown -= keyHandler.OnPreviewKeyDown;
                tagger.Dispose();
            };
        }
    }
}
