using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using AICodeSuggest.Models;

namespace AICodeSuggest.Services
{
    public interface ISuggestionEngine
    {
        Task<AISuggestion> GenerateSuggestionAsync(IWpfTextView textView, CancellationToken ct);
    }
}
