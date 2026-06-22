using System.Collections.Generic;

namespace AICodeSuggest.Services
{
    public class CodeStructureAnalysis
    {
        public List<string> UsingStatements { get; set; } = new List<string>();
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string ClassDeclaration { get; set; }
        public string MethodSignature { get; set; }
        public List<string> MethodParameters { get; set; } = new List<string>();
        public List<string> LocalVariables { get; set; } = new List<string>();
        public string ReturnType { get; set; }
    }

    public interface ICodeAnalyzerService
    {
        CodeStructureAnalysis Analyze(string sourceCode, string language);
    }
}
