namespace AICodeSuggest.Models
{
    public class AIResponse
    {
        public string Content { get; set; } = string.Empty;
        public string FinishReason { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Content);
    }
}
