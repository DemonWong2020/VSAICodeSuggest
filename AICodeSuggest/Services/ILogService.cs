using System;

namespace AICodeSuggest.Services
{
    public interface ILogService : IDisposable
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(string message, Exception ex);
    }
}
