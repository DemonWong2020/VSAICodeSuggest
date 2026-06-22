using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AICodeSuggest.Services
{
    public class LogService : ILogService
    {
        private const string PaneName = "AI Code Suggest";
        private IVsOutputWindowPane _pane;
        private readonly AsyncPackage _package;

        public LogService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public void Info(string message)
        {
            WriteMessage("INFO", message);
        }

        public void Warn(string message)
        {
            WriteMessage("WARN", message);
        }

        public void Error(string message)
        {
            WriteMessage("ERROR", message);
        }

        public void Error(string message, Exception ex)
        {
            WriteMessage("ERROR", $"{message}\n  {ex.GetType().Name}: {ex.Message}\n  {ex.StackTrace}");
        }

        private void WriteMessage(string level, string message)
        {
            _package.JoinableTaskFactory.Run(async () =>
            {
                await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_pane == null)
                {
                    var outputWindow = await _package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    if (outputWindow == null) return;

                    var paneGuid = new Guid("AF3C84E1-9B2D-4A1F-8C7D-1E5A9F3B6D2C");
                    if (ErrorHandler.Failed(outputWindow.GetPane(ref paneGuid, out _pane)) || _pane == null)
                    {
                        ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(ref paneGuid, PaneName, 1, 1));
                        ErrorHandler.ThrowOnFailure(outputWindow.GetPane(ref paneGuid, out _pane));
                    }
                }

                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _pane?.OutputStringThreadSafe($"[{timestamp}] [{level}] {message}\n");
            });
        }

        public void Dispose()
        {
            _pane = null;
        }
    }
}
