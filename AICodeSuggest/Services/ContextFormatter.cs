using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AICodeSuggest.Options;

namespace AICodeSuggest.Services
{
    public class ContextFormatter : IContextFormatter
    {
        public string FormatContext(Models.CodeContext ctx, GeneralOptions options)
        {
            var sb = new System.Text.StringBuilder();
            var smartEnabled = options?.EnableSmartContext ?? true;

            // ── 项目信息 ──
            bool hasProjectInfo = !string.IsNullOrWhiteSpace(ctx.ProjectName)
                               || !string.IsNullOrWhiteSpace(ctx.SolutionName)
                               || !string.IsNullOrWhiteSpace(ctx.ProjectKind);
            if (hasProjectInfo)
            {
                sb.AppendLine("## 项目上下文");
                if (!string.IsNullOrWhiteSpace(ctx.SolutionName))
                    sb.AppendLine($"- 解决方案: {ctx.SolutionName}");
                if (!string.IsNullOrWhiteSpace(ctx.ProjectName))
                    sb.AppendLine($"- 项目: {ctx.ProjectName}");
                if (!string.IsNullOrWhiteSpace(ctx.ProjectKind))
                    sb.AppendLine($"- 项目类型: {ctx.ProjectKind}");
                sb.AppendLine();
            }

            // ── 文件级结构 ──
            bool hasStructure = smartEnabled && (
                ctx.UsingStatements?.Count > 0 ||
                !string.IsNullOrWhiteSpace(ctx.EnclosingNamespace) ||
                !string.IsNullOrWhiteSpace(ctx.EnclosingClass) ||
                !string.IsNullOrWhiteSpace(ctx.EnclosingMethod));

            if (hasStructure)
            {
                sb.AppendLine("## 代码结构");

                if (!string.IsNullOrWhiteSpace(ctx.EnclosingNamespace))
                    sb.AppendLine($"- 命名空间: {ctx.EnclosingNamespace}");

                if (!string.IsNullOrWhiteSpace(ctx.EnclosingClass))
                    sb.AppendLine($"- 所在类: {ctx.EnclosingClass}");

                if (!string.IsNullOrWhiteSpace(ctx.EnclosingMethod))
                    sb.AppendLine($"- 所在方法: {ctx.EnclosingMethod}");

                if (ctx.UsingStatements?.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("### 引用 (using/import)");
                    foreach (var u in ctx.UsingStatements.Take(30))
                        sb.AppendLine(u);
                }

                if (ctx.LocalVariables?.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("### 局部变量/参数");
                    foreach (var v in ctx.LocalVariables.Take(20))
                        sb.AppendLine($"- {v}");
                }

                sb.AppendLine();
            }

            // ── 选中代码 ──
            if (ctx.HasSelection)
            {
                sb.AppendLine("## 用户选中的代码 (参考上下文)");
                sb.AppendLine("```" + (string.IsNullOrWhiteSpace(ctx.Language) ? "" : ctx.Language));
                sb.AppendLine(Truncate(ctx.SelectedCode, 3000));
                sb.AppendLine("```");
                sb.AppendLine();
            }

            // ── 补全指令 ──
            sb.AppendLine("## 代码补全");
            sb.AppendLine($"在 `<CURSOR_HERE>` 位置补全代码。文件: `{ctx.DocumentPath ?? "Untitled"}`。");

            // ── 代码正文 ──
            if (!string.IsNullOrWhiteSpace(ctx.BeforeCursor) || !string.IsNullOrWhiteSpace(ctx.CurrentLine))
            {
                sb.AppendLine();
                var lang = string.IsNullOrWhiteSpace(ctx.Language) ? "" : ctx.Language;
                if (!string.IsNullOrWhiteSpace(ctx.BeforeCursor))
                {
                    sb.AppendLine("```" + lang);
                    sb.Append(ctx.BeforeCursor.TrimEnd('\n'));
                    sb.AppendLine();
                    sb.AppendLine("<CURSOR_HERE>");
                    if (!string.IsNullOrWhiteSpace(ctx.AfterCursor))
                    {
                        sb.Append(ctx.AfterCursor.TrimEnd('\n'));
                        sb.AppendLine();
                    }
                    sb.AppendLine("```");
                }
                else if (!string.IsNullOrWhiteSpace(ctx.CurrentLine))
                {
                    sb.AppendLine("```" + lang);
                    sb.Append("<CURSOR_HERE>");
                    var currLine = StripCursor(ctx.CurrentLine);
                    if (!string.IsNullOrWhiteSpace(currLine))
                        sb.Append(currLine);
                    sb.AppendLine();
                    if (!string.IsNullOrWhiteSpace(ctx.AfterCursor))
                    {
                        sb.Append(ctx.AfterCursor.TrimEnd('\n'));
                        sb.AppendLine();
                    }
                    sb.AppendLine("```");
                }
            }

            return sb.ToString();
        }

        private static string StripCursor(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            return line.Replace("<CURSOR>", "").Replace("<CURSOR_HERE>", "");
        }

        private static string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLen) return text;
            int cut = text.LastIndexOf('\n', maxLen);
            if (cut < maxLen / 2) cut = maxLen;
            return text.Substring(0, cut) + "\n...";
        }
    }
}
