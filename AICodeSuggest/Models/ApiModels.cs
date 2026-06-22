using Newtonsoft.Json;

namespace AICodeSuggest.Models
{
    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class ChatCompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public ChatMessage[] Messages { get; set; }

        [JsonProperty("temperature", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double Temperature { get; set; }

        [JsonProperty("top_p", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double TopP { get; set; }

        [JsonProperty("max_tokens", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int MaxTokens { get; set; }

        [JsonProperty("stop", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Stop { get; set; }
    }

    public class ChatCompletionResponse
    {
        [JsonProperty("choices")]
        public Choice[] Choices { get; set; }

        [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public ChatMessage Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class Usage
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
