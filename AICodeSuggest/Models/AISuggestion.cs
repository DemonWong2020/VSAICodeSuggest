using System;

namespace AICodeSuggest.Models
{
    public class AISuggestion
    {
        public string Text { get; set; } = string.Empty;
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ModelUsed { get; set; } = string.Empty;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public static AISuggestion Empty(string reason = null) => new AISuggestion
        {
            Text = string.Empty,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };
    }
}
