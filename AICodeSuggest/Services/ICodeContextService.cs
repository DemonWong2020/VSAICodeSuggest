using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using AICodeSuggest.Models;

namespace AICodeSuggest.Services
{
    public interface ICodeContextService
    {
        Task<CodeContext> GetCodeContextAsync(IWpfTextView textView, CancellationToken ct);
    }
}
