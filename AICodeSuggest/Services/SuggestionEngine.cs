using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using AICodeSuggest.Models;
using AICodeSuggest.Options;

namespace AICodeSuggest.Services
{
    public class SuggestionEngine : ISuggestionEngine
    {
        private readonly IAIClientService _aiClient;
        private readonly ICodeContextService _codeContext;
        private readonly ILogService _log;
        private readonly AsyncPackage _package;

        public SuggestionEngine(AsyncPackage package, IAIClientService aiClient,
            ICodeContextService codeContext, ILogService log)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _aiClient = aiClient ?? throw new ArgumentNullException(nameof(aiClient));
            _codeContext = codeContext ?? throw new ArgumentNullException(nameof(codeContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<AISuggestion> GenerateSuggestionAsync(IWpfTextView textView, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // 1. 检查是否启用
                var generalOptions = _package.GetDialogPage(typeof(GeneralOptions)) as GeneralOptions;
                if (generalOptions != null && !generalOptions.EnableSuggestions)
                {
                    _log.Info("SuggestionEngine: 建议功能已关闭");
                    return AISuggestion.Empty("功能已关闭");
                }

                // 2. 采集代码上下文（使用 tagger 传入的 textView，避免查询 active view 时脱节）
                var context = await _codeContext.GetCodeContextAsync(textView, ct);
                if (ct.IsCancellationRequested || context.IsEmpty)
                {
                    _log.Info("SuggestionEngine: 代码上下文为空");
                    return AISuggestion.Empty();
                }

                // 3. 调用 AI
                var suggestion = await _aiClient.RequestSuggestionAsync(context, ct);

                // 4. 应用最大长度限制
                int maxLen = generalOptions?.SuggestionMaxLength ?? 500;
                if (!string.IsNullOrEmpty(suggestion.Text) && suggestion.Text.Length > maxLen)
                {
                    // 在最大长度处找最后一个换行符截断，避免截断在单词中间
                    int cutPos = suggestion.Text.LastIndexOf('\n', maxLen);
                    if (cutPos < maxLen / 2) cutPos = maxLen;
                    suggestion = new AISuggestion
                    {
                        Text = suggestion.Text.Substring(0, cutPos),
                        ModelUsed = suggestion.ModelUsed
                    };
                }

                sw.Stop();
                _log.Info($"SuggestionEngine: 生成建议完成, 耗时={sw.ElapsedMilliseconds}ms, " +
                         $"长度={suggestion.Text.Length}, 模型={suggestion.ModelUsed}");

                return suggestion;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _log.Error($"SuggestionEngine: 生成建议失败 ({sw.ElapsedMilliseconds}ms)", ex);
                return AISuggestion.Empty($"生成失败: {ex.Message}");
            }
        }
    }
}
