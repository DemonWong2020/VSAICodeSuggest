using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeSuggest.Models;
using AICodeSuggest.Services;
using Newtonsoft.Json;

namespace AICodeSuggest.Providers
{
    public class OpenAIProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogService _log;
        private readonly string _endpoint;
        private readonly string _apiKey;

        public string ProviderName => "OpenAI 兼容";

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public OpenAIProvider(string endpoint, string apiKey, ILogService log)
        {
            _endpoint = endpoint?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(endpoint));
            _apiKey = apiKey;
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<AIResponse> SendChatAsync(AIRequest request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var payload = new ChatCompletionRequest
            {
                Model = request.Model ?? "gpt-4o",
                Messages = new[]
                {
                    new ChatMessage { Role = "system", Content = request.SystemPrompt },
                    new ChatMessage { Role = "user", Content = request.UserPrompt }
                },
                Temperature = request.Temperature,
                TopP = request.TopP,
                MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : 256,
                Stop = request.Stop
            };

            var json = JsonConvert.SerializeObject(payload);
            var url = $"{_endpoint}/chat/completions";
            _log.Info($"OpenAI 请求: url={url}, model={request.Model}, len={json.Length}");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                httpRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            var httpResponse = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                _log.Warn($"OpenAI API 返回错误 {httpResponse.StatusCode}: {Truncate(responseBody, 200)}");
                throw new HttpRequestException($"API 错误 {(int)httpResponse.StatusCode}: {Truncate(responseBody, 200)}");
            }

            // 验证响应是否为 JSON（防止端点返回 HTML 页面时静默解析失败）
            if (!IsJsonResponse(responseBody))
            {
                var contentType = httpResponse.Content.Headers.ContentType?.MediaType ?? "unknown";
                _log.Error($"OpenAI 端点 {url} 返回了非 JSON 响应 (Content-Type: {contentType}): {Truncate(responseBody, 500)}");
                throw new HttpRequestException(
                    $"端点返回了非 JSON 响应 (Content-Type: {contentType})。" +
                    $"请检查 API 地址是否正确。当前请求地址: {url}。" +
                    $"提示: 大多数 OpenAI 兼容 API 的地址格式为 http://host:port/v1");
            }

            return ParseResponse(responseBody);
        }

        public async Task<bool> ValidateConnectionAsync(CancellationToken ct)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}/models");
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", _apiKey);
                }
                var response = await _httpClient.SendAsync(request, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsJsonResponse(string body)
        {
            if (string.IsNullOrEmpty(body)) return false;
            // JSON 响应以 '{' 或 '[' 开头（忽略前导空白）
            for (int i = 0; i < body.Length; i++)
            {
                if (!char.IsWhiteSpace(body[i]))
                    return body[i] == '{' || body[i] == '[';
            }
            return false;
        }

        private AIResponse ParseResponse(string responseJson)
        {
            var resp = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseJson);
            if (resp?.Choices == null || resp.Choices.Length == 0)
                return new AIResponse();

            var content = resp.Choices[0].Message?.Content;
            _log.Info($"OpenAI 返回: {content?.Length ?? 0} 字符, 完成原因={resp.Choices[0].FinishReason}" +
                     (content != null ? $"\n  ── 内容 ──\n{Truncate(content, 800)}" : ""));

            return new AIResponse
            {
                Content = content ?? string.Empty,
                FinishReason = resp.Choices[0].FinishReason,
                PromptTokens = resp.Usage?.PromptTokens,
                CompletionTokens = resp.Usage?.CompletionTokens
            };
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
