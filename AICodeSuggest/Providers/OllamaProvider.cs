using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeSuggest.Models;
using AICodeSuggest.Services;
using Newtonsoft.Json;

namespace AICodeSuggest.Providers
{
    public class OllamaProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogService _log;
        private readonly string _endpoint;

        public string ProviderName => "Ollama 本地";

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        public OllamaProvider(string endpoint, ILogService log)
        {
            _endpoint = endpoint?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(endpoint));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        public async Task<AIResponse> SendChatAsync(AIRequest request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var payload = new
            {
                model = request.Model ?? "codellama",
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                stream = false,
                options = new
                {
                    temperature = request.Temperature,
                    num_predict = request.MaxTokens > 0 ? request.MaxTokens : 256
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var url = $"{_endpoint}/api/chat";
            _log.Info($"Ollama 请求: url={url}, model={request.Model}");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpResponse = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                _log.Warn($"Ollama API 返回错误 {httpResponse.StatusCode}: {Truncate(responseBody, 200)}");
                throw new HttpRequestException($"Ollama 错误: {Truncate(responseBody, 200)}");
            }

            if (!IsJsonResponse(responseBody))
            {
                var contentType = httpResponse.Content.Headers.ContentType?.MediaType ?? "unknown";
                _log.Error($"Ollama 端点 {url} 返回了非 JSON 响应 (Content-Type: {contentType}): {Truncate(responseBody, 500)}");
                throw new HttpRequestException($"端点返回了非 JSON 响应 (Content-Type: {contentType})。请检查 Ollama 服务是否正常运行。当前请求地址: {url}");
            }

            return ParseResponse(responseBody);
        }

        public async Task<bool> ValidateConnectionAsync(CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_endpoint}/api/tags", ct);
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
            for (int i = 0; i < body.Length; i++)
            {
                if (!char.IsWhiteSpace(body[i]))
                    return body[i] == '{' || body[i] == '[';
            }
            return false;
        }

        private AIResponse ParseResponse(string responseJson)
        {
            var resp = JsonConvert.DeserializeAnonymousType(responseJson,
                new { message = new { content = "" } });

            var content = resp?.message?.content ?? string.Empty;
            _log.Info($"Ollama 返回: {content.Length} 字符");

            return new AIResponse
            {
                Content = content
            };
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }
    }
}
