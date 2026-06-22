using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using AICodeSuggest.Services;
using AICodeSuggest.Options;
using Task = System.Threading.Tasks.Task;

namespace AICodeSuggest
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideBindingPath]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(GeneralOptions), "AI Code Suggest", "通用设置", 0, 0, true)]
    [ProvideOptionPage(typeof(AIModelOptions), "AI Code Suggest", "AI 模型", 0, 0, true)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class AICodeSuggestPackage : AsyncPackage
    {
        public const string PackageGuidString = "ae4f3c8d-9b1e-4a2f-8c7d-1e5a9f3b6d2c";

        internal static AICodeSuggestPackage Instance { get; private set; }

        private ILogService _logService;
        private IAIClientService _aiClientService;
        private ICodeContextService _codeContextService;
        private ICodeAnalyzerService _codeAnalyzerService;
        private IContextFormatter _contextFormatter;
        private ISuggestionEngine _suggestionEngine;

        public ISuggestionEngine SuggestionEngine => _suggestionEngine;
        public ILogService LogService => _logService;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;

            _logService = new LogService(this);
            _logService.Info("AI Code Suggest 插件已初始化");

            _codeAnalyzerService = new CodeAnalyzerService();
            _contextFormatter = new ContextFormatter();
            _aiClientService = new AIClientService(this, _logService, _contextFormatter);
            _codeContextService = new CodeContextService(this, _logService, _codeAnalyzerService);
            _suggestionEngine = new SuggestionEngine(this, _aiClientService, _codeContextService, _logService);

            _logService.Info("AI Code Suggest 核心引擎已就绪 (阶段4: 上下文引擎)");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (_aiClientService as IDisposable)?.Dispose();
                _logService?.Dispose();
                Instance = null;
            }
            base.Dispose(disposing);
        }
    }
}
