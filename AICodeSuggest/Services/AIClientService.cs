using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using AICodeSuggest.Models;
using AICodeSuggest.Options;
using AICodeSuggest.Providers;

namespace AICodeSuggest.Services
{
    public class AIClientService : IAIClientService
    {
        private readonly AsyncPackage _package;
        private readonly ILogService _log;
        private readonly IContextFormatter _formatter;

        private const string CodeCompletionSystemPrompt =
            "You are a code completion assistant. Given structured code context around a cursor position, " +
            "output ONLY the code that should appear at the cursor. Do NOT include explanations, " +
            "markdown code fences, or any text that is not valid code. Return the raw code continuation " +
            "that begins at the cursor position.";

        public AIClientService(AsyncPackage package, ILogService log, IContextFormatter formatter)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public async Task<AISuggestion> RequestSuggestionAsync(CodeContext context, CancellationToken ct)
        {
            if (context.IsEmpty)
                return AISuggestion.Empty("空代码上下文");

            try
            {
                var options = _package.GetDialogPage(typeof(AIModelOptions)) as AIModelOptions;
                if (options == null || string.IsNullOrWhiteSpace(options.ApiEndpoint))
                    return AISuggestion.Empty("AI 模型未配置");

                var generalOptions = _package.GetDialogPage(typeof(GeneralOptions)) as GeneralOptions;

                var provider = AIProviderFactory.Create(options, _log);

                // 使用 ContextFormatter 构建结构化提示词
                string userPrompt = _formatter.FormatContext(context, generalOptions);

                int maxTokens = generalOptions?.SuggestionMaxLength > 0
                    ? (int)Math.Ceiling(generalOptions.SuggestionMaxLength / 2.0)
                    : 256;

                // 上下文 Token 限制：如果设置了上限，估算并截断
                int tokenLimit = generalOptions?.MaxContextTokens ?? 0;
                if (tokenLimit > 0)
                {
                    userPrompt = TruncateToTokenEstimate(userPrompt, tokenLimit);
                }

                var request = new AIRequest
                {
                    SystemPrompt = CodeCompletionSystemPrompt,
                    UserPrompt = userPrompt,
                    Model = options.ModelName,
                    Temperature = options.Temperature,
                    TopP = options.TopP,
                    MaxTokens = maxTokens,
                    Stop = new[] { "\n\n\n" }
                };

                _log.Info($"AI 请求: model={options.ModelName}, promptLen={request.UserPrompt?.Length ?? 0}, maxTokens={maxTokens}");

                var response = await provider.SendChatAsync(request, ct);

                if (response.IsEmpty)
                    return AISuggestion.Empty("AI 未返回有效建议");

                return new AISuggestion
                {
                    Text = response.Content,
                    ModelUsed = options.ModelName ?? "unknown"
                };
            }
            catch (OperationCanceledException)
            {
                _log.Info("AI 建议请求已取消");
                return AISuggestion.Empty("请求已取消");
            }
            catch (Exception ex)
            {
                _log.Error("AI 建议请求失败", ex);
                return AISuggestion.Empty($"请求失败: {ex.Message}");
            }
        }

        public async Task<bool> ValidateConnectionAsync(CancellationToken ct)
        {
            try
            {
                var options = _package.GetDialogPage(typeof(AIModelOptions)) as AIModelOptions;
                if (options == null || string.IsNullOrWhiteSpace(options.ApiEndpoint))
                    return false;

                var provider = AIProviderFactory.Create(options, _log);
                return await provider.ValidateConnectionAsync(ct);
            }
            catch
            {
                return false;
            }
        }

        private static string TruncateToTokenEstimate(string text, int maxTokens)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // 粗略估算：1 token ≈ 4 字符（英文）或 2 字符（中文），取中间值 3
            int maxChars = maxTokens * 3;
            if (text.Length <= maxChars) return text;
            int cut = text.LastIndexOf('\n', maxChars);
            if (cut < maxChars * 0.6) cut = maxChars;
            return text.Substring(0, cut) + "\n... (上下文已截断)";
        }
    }
}
