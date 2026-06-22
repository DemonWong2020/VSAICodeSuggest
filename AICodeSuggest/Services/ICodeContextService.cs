using System.Threading;
using System.Threading.Tasks;
using AICodeSuggest.Models;

namespace AICodeSuggest.Services
{
    public interface ICodeContextService
    {
        Task<CodeContext> GetCodeContextAsync(CancellationToken ct);
    }
}
