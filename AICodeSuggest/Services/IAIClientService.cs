using System.Threading;
using System.Threading.Tasks;
using AICodeSuggest.Models;

namespace AICodeSuggest.Services
{
    public interface IAIClientService
    {
        Task<AISuggestion> RequestSuggestionAsync(CodeContext context, CancellationToken ct);
        Task<bool> ValidateConnectionAsync(CancellationToken ct);
    }
}
