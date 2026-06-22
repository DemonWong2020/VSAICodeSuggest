using System;
using System.Threading;
using System.Threading.Tasks;
using AICodeSuggest.Models;

namespace AICodeSuggest.Providers
{
    public interface IAIProvider : IDisposable
    {
        string ProviderName { get; }
        Task<AIResponse> SendChatAsync(AIRequest request, CancellationToken ct);
        Task<bool> ValidateConnectionAsync(CancellationToken ct);
    }
}
