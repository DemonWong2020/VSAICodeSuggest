using System.Threading;
using System.Threading.Tasks;
using AICodeSuggest.Models;

namespace AICodeSuggest.Services
{
    public interface ISuggestionEngine
    {
        Task<AISuggestion> GenerateSuggestionAsync(CancellationToken ct);
    }
}
