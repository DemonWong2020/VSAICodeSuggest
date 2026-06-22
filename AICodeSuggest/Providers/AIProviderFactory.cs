using System;
using AICodeSuggest.Options;
using AICodeSuggest.Services;

namespace AICodeSuggest.Providers
{
    public static class AIProviderFactory
    {
        public const string ProviderTypeOpenAI = "OpenAI兼容";
        public const string ProviderTypeOllama = "Ollama 本地";
        public const string ProviderTypeCustom = "自定义";

        public static IAIProvider Create(AIModelOptions options, ILogService log)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            if (string.IsNullOrWhiteSpace(options.ApiEndpoint))
                throw new InvalidOperationException("API 端点未配置");

            var providerType = options.ProviderType ?? ProviderTypeOpenAI;

            if (providerType == ProviderTypeOllama)
                return new OllamaProvider(options.ApiEndpoint, log);

            // OpenAI兼容 和 自定义 都使用 OpenAIProvider
            return new OpenAIProvider(options.ApiEndpoint, options.ApiKey, log);
        }
    }
}
