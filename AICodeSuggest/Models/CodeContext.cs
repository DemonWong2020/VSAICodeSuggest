using System.Collections.Generic;

namespace AICodeSuggest.Models
{
    public class CodeContext
    {
        // ── 原始代码上下文 ──
        public string DocumentPath { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string BeforeCursor { get; set; } = string.Empty;
        public string AfterCursor { get; set; } = string.Empty;
        public string CurrentLine { get; set; } = string.Empty;

        // ── 代码结构分析结果 ──
        public List<string> UsingStatements { get; set; } = new List<string>();
        public string EnclosingNamespace { get; set; }
        public string EnclosingClass { get; set; }
        public string EnclosingMethod { get; set; }
        public List<string> LocalVariables { get; set; } = new List<string>();

        // ── 项目级上下文 ──
        public string ProjectName { get; set; }
        public string ProjectKind { get; set; }
        public string SolutionName { get; set; }

        // ── 用户选中文本（用于 FIM 或参考上下文） ──
        public string SelectedCode { get; set; }
        public bool HasSelection => !string.IsNullOrWhiteSpace(SelectedCode);

        // ── 辅助属性 ──
        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(BeforeCursor) &&
            string.IsNullOrWhiteSpace(CurrentLine) &&
            string.IsNullOrWhiteSpace(AfterCursor);

        public static CodeContext Empty => new CodeContext();
    }
}
