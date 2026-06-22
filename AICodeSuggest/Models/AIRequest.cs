namespace AICodeSuggest.Models
{
    public class AIRequest
    {
        public string SystemPrompt { get; set; }
        public string UserPrompt { get; set; }
        public string Model { get; set; }
        public double Temperature { get; set; }
        public double TopP { get; set; }
        public int MaxTokens { get; set; }
        public string[] Stop { get; set; }
    }
}
