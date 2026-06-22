using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using AICodeSuggest.Models;
using AICodeSuggest.Options;

namespace AICodeSuggest.Services
{
    public class CodeContextService : ICodeContextService
    {
        private readonly AsyncPackage _package;
        private readonly ILogService _log;
        private readonly ICodeAnalyzerService _analyzer;

        public CodeContextService(AsyncPackage package, ILogService log, ICodeAnalyzerService analyzer)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        }

        public async Task<CodeContext> GetCodeContextAsync(IWpfTextView textView, CancellationToken ct)
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            try
            {
                IWpfTextView wpfView = textView;

                // 如果调用方传入了 textView 则直接使用（来自 SuggestionTagger），
                // 否则回退到查询 IVsTextManager 的活动视图（兼容旧调用路径）
                if (wpfView == null)
                {
                    var textManager = await _package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
                    if (textManager == null)
                    {
                        _log.Info("CodeContext: 无法获取 IVsTextManager");
                        return CodeContext.Empty;
                    }

                    if (ErrorHandler.Failed(textManager.GetActiveView(1, null, out IVsTextView vsTextView)) || vsTextView == null)
                    {
                        _log.Info("CodeContext: 无活动编辑器");
                        return CodeContext.Empty;
                    }

                    var componentModel = await _package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                    var adapterFactory = componentModel?.GetService<IVsEditorAdaptersFactoryService>();
                    if (adapterFactory == null)
                    {
                        _log.Info("CodeContext: 无法获取 IVsEditorAdaptersFactoryService");
                        return CodeContext.Empty;
                    }

                    wpfView = adapterFactory.GetWpfTextView(vsTextView);
                    if (wpfView == null)
                    {
                        _log.Info("CodeContext: 无法获取 IWpfTextView");
                        return CodeContext.Empty;
                    }
                }

                return ExtractContext(wpfView);
            }
            catch (Exception ex)
            {
                _log.Error("CodeContext: 采集上下文异常", ex);
                return CodeContext.Empty;
            }
        }

        private CodeContext ExtractContext(IWpfTextView wpfView)
        {
            var buffer = wpfView.TextBuffer;
            var caret = wpfView.Caret;
            var snapshot = buffer.CurrentSnapshot;

            // ── 文档路径 ──
            string docPath = "Untitled";
            if (buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument))
                docPath = textDocument.FilePath;

            // ── 语言 ──
            string language = buffer.ContentType?.DisplayName ?? "plaintext";

            // ── 光标位置与行范围 ──
            var caretPosition = caret.Position.BufferPosition;
            int totalLines = snapshot.LineCount;
            int currentLineNum = snapshot.GetLineNumberFromPosition(caretPosition);

            var options = _package.GetDialogPage(typeof(GeneralOptions)) as GeneralOptions;
            int linesBefore = options?.ContextLinesBefore ?? 50;
            int linesAfter = options?.MaxLinesAfter ?? 10;

            int startLine = Math.Max(0, currentLineNum - linesBefore);
            int endLine = Math.Min(totalLines - 1, currentLineNum + linesAfter);

            // ── 采集光标前后代码文本 ──
            var beforeBuilder = new StringBuilder();
            for (int i = startLine; i < currentLineNum; i++)
                beforeBuilder.AppendLine(snapshot.GetLineFromLineNumber(i).GetText());

            var afterBuilder = new StringBuilder();
            for (int i = currentLineNum + 1; i <= endLine; i++)
                afterBuilder.AppendLine(snapshot.GetLineFromLineNumber(i).GetText());

            string currentLine = snapshot.GetLineFromLineNumber(currentLineNum).GetText();
            int cursorCol = caretPosition - snapshot.GetLineFromLineNumber(currentLineNum).Start;
            if (cursorCol >= 0 && cursorCol < currentLine.Length)
                currentLine = currentLine.Insert(cursorCol, "<CURSOR>");

            // ── 智能代码结构分析 ──
            string allCodeBefore = snapshot.GetText(0, caretPosition);
            var structure = _analyzer.Analyze(allCodeBefore, language);

            // ── 项目信息（DTE） ──
            string projectName = null, projectKind = null, solutionName = null;
            GetProjectInfo(docPath, out projectName, out projectKind, out solutionName);

            // ── 用户选中文本 ──
            string selectedCode = null;
            if (!wpfView.Selection.IsEmpty && !wpfView.Selection.Start.Position.Equals(caretPosition))
            {
                var selSpan = wpfView.Selection.StreamSelectionSpan;
                int selStart = selSpan.Start.Position;
                int selEnd = selSpan.End.Position;
                if (selEnd - selStart > 0)
                    selectedCode = snapshot.GetText(selStart, selEnd - selStart);
            }

            // ── 构建上下文 ──
            var ctx = new CodeContext
            {
                DocumentPath = docPath,
                Language = language,
                BeforeCursor = beforeBuilder.ToString(),
                AfterCursor = afterBuilder.ToString(),
                CurrentLine = currentLine,
                UsingStatements = structure.UsingStatements,
                EnclosingNamespace = structure.Namespace,
                EnclosingClass = structure.ClassName,
                EnclosingMethod = structure.MethodSignature,
                LocalVariables = structure.LocalVariables,
                ProjectName = projectName,
                ProjectKind = projectKind,
                SolutionName = solutionName,
                SelectedCode = selectedCode
            };

            _log.Info($"CodeContext: 文件={System.IO.Path.GetFileName(docPath)}, 语言={language}, " +
                     $"行={currentLineNum + 1}/{totalLines}, 范围={startLine}-{endLine}, " +
                     $"类={structure.ClassName ?? "-"}, 方法={structure.MethodSignature ?? "-"}, " +
                     $"using={structure.UsingStatements?.Count ?? 0}, 项目={projectName ?? "-"}");

            return ctx;
        }

        private void GetProjectInfo(string filePath, out string projectName, out string projectKind, out string solutionName)
        {
            string pn = null, pk = null, sn = null;

            try
            {
                _package.JoinableTaskFactory.Run(async () =>
                {
                    await _package.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
                    if (dte == null) return;

                    var fullName = dte.Solution?.FullName;
                    if (!string.IsNullOrEmpty(fullName))
                        sn = System.IO.Path.GetFileNameWithoutExtension(fullName);

                    if (string.IsNullOrEmpty(filePath) || filePath == "Untitled") return;

                    var projects = dte.Solution?.Projects;
                    if (projects == null) return;

                    foreach (Project project in projects)
                    {
                        var found = FindProjectItem(project, filePath);
                        if (found != null)
                        {
                            pn = project.Name;
                            pk = GetProjectKind(project);
                            break;
                        }
                    }
                });
            }
            catch
            {
                // DTE 不可用时静默跳过
            }

            projectName = pn;
            projectKind = pk;
            solutionName = sn;
        }

        private static ProjectItem FindProjectItem(Project project, string filePath)
        {
            try
            {
                if (project.ProjectItems == null) return null;
                foreach (ProjectItem item in project.ProjectItems)
                {
                    var result = FindInProjectItem(item, filePath);
                    if (result != null) return result;
                }
            }
            catch { }
            return null;
        }

        private static ProjectItem FindInProjectItem(ProjectItem item, string filePath)
        {
            try
            {
                for (short i = 1; i <= item.FileCount; i++)
                {
                    if (string.Equals(item.get_FileNames(i), filePath, StringComparison.OrdinalIgnoreCase))
                        return item;
                }

                if (item.ProjectItems != null)
                {
                    foreach (ProjectItem child in item.ProjectItems)
                    {
                        var result = FindInProjectItem(child, filePath);
                        if (result != null) return result;
                    }
                }

                if (item.SubProject != null && item.SubProject.ProjectItems != null)
                {
                    foreach (ProjectItem subItem in item.SubProject.ProjectItems)
                    {
                        var result = FindInProjectItem(subItem, filePath);
                        if (result != null) return result;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string GetProjectKind(Project project)
        {
            try
            {
                // 通过项目类型 GUID 判断
                var kind = project.Kind ?? "";
                switch (kind)
                {
                    case "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}": return "C#";
                    case "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}": return "VB.NET";
                    case "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}": return "C++";
                    case "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}": return "C# (SDK)";
                    case "{349C5851-65DF-11DA-9384-00065B846F21}": return "ASP.NET";
                    case "{E24C65DC-7377-472B-9ABA-BC803B73C61A}": return "Web Site";
                    default: return kind;
                }
            }
            catch { return null; }
        }
    }
}
