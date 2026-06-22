using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AICodeSuggest.Services
{
    public class CodeAnalyzerService : ICodeAnalyzerService
    {
        // ── C# 正则 ──
        private static readonly Regex CsUsing = new Regex(
            @"^\s*using\s+(\S[\S]*?)\s*;", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CsNamespace = new Regex(
            @"^\s*namespace\s+(\S+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CsClass = new Regex(
            @"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|unsafe|readonly|ref\s+)*\s*(?:class|struct|record|interface|enum)\s+(\w+(?:<[\w\s,]+>)?)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CsMethod = new Regex(
            @"^\s*(?:public|private|protected|internal|static|virtual|override|abstract|async|unsafe|sealed|new|extern|partial|readonly|volatile\s+)*" +
            @"((?:\w+(?:<[\w\s,]+>)?)\s+)?(\w+)\s*\(([^)]*)\)",
            RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CsVariable = new Regex(
            @"(?:var|int|long|float|double|decimal|bool|byte|char|string|object|dynamic|DateTime|Guid|List<[\w]+>|Dictionary<[\w\s,]+>|IEnumerable<[\w]+>|IList<[\w]+>|\w+)\s+(\w+)\s*=",
            RegexOptions.Multiline | RegexOptions.Compiled);

        // ── JavaScript / TypeScript 正则 ──
        private static readonly Regex JsImport = new Regex(
            @"^\s*(?:import\s+.*?from\s+['""][^'""]+['""]|import\s+['""][^'""]+['""]|const\s+.*?=\s*require\s*\(|import\s*\([^)]*\))",
            RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex JsFunction = new Regex(
            @"^\s*(?:export\s+)?(?:async\s+)?(?:static\s+)?(?:function\s+(\w+)|(\w+)\s*=\s*(?:async\s+)?\([^)]*\)\s*=>|(\w+)\s*\([^)]*\)\s*\{)",
            RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex JsClass = new Regex(
            @"^\s*(?:export\s+)?(?:abstract\s+)?class\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex JsVariable = new Regex(
            @"^\s*(?:const|let|var)\s+(\w+)\s*=", RegexOptions.Multiline | RegexOptions.Compiled);

        // ── Python 正则 ──
        private static readonly Regex PyImport = new Regex(
            @"^\s*(?:import\s+(\S+)|from\s+(\S+)\s+import\s+.+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PyClass = new Regex(
            @"^\s*class\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PyFunction = new Regex(
            @"^\s*def\s+(\w+)\s*\(([^)]*)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PyVariable = new Regex(
            @"^\s*(\w+)\s*=\s*\S", RegexOptions.Multiline | RegexOptions.Compiled);

        // ── C / C++ 正则 ──
        private static readonly Regex CppInclude = new Regex(
            @"^\s*#include\s+[<""]([^>""]+)[>""]", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CppNamespace = new Regex(
            @"^\s*namespace\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CppClass = new Regex(
            @"^\s*(?:class|struct)\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex CppFunction = new Regex(
            @"^\s*(?:virtual\s+)?(?:static\s+)?(?:inline\s+)?(?:const\s+)?((?:\w+(?:<[\w\s,]+>)?(?:\s*\*|\s*&)?)\s+)?(\w+)\s*\(([^)]*)\)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        public CodeStructureAnalysis Analyze(string sourceCode, string language)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return new CodeStructureAnalysis();

            var normLang = (language ?? "").ToLowerInvariant();

            switch (normLang)
            {
                case "csharp":
                case "c#":
                case "cs":
                    return AnalyzeCSharp(sourceCode);
                case "typescript":
                case "javascript":
                case "js":
                case "ts":
                case "jsx":
                case "tsx":
                    return AnalyzeJavaScript(sourceCode);
                case "python":
                case "py":
                    return AnalyzePython(sourceCode);
                case "c++":
                case "cpp":
                case "c":
                case "cxx":
                case "h":
                case "hpp":
                    return AnalyzeCpp(sourceCode);
                default:
                    return AnalyzeGeneric(sourceCode);
            }
        }

        private CodeStructureAnalysis AnalyzeCSharp(string code)
        {
            var result = new CodeStructureAnalysis();

            // using 语句
            foreach (Match m in CsUsing.Matches(code))
                result.UsingStatements.Add(m.Value.Trim());

            // namespace（取最后一个，即最内层）
            var nsMatches = CsNamespace.Matches(code);
            if (nsMatches.Count > 0)
                result.Namespace = nsMatches[nsMatches.Count - 1].Groups[1].Value;

            // class（取最后一个，离光标最近）
            var classMatches = CsClass.Matches(code);
            if (classMatches.Count > 0)
            {
                var last = classMatches[classMatches.Count - 1];
                result.ClassName = last.Groups[1].Value;
                result.ClassDeclaration = last.Value.Trim();
            }

            // method（取最后一个）
            var methodMatches = CsMethod.Matches(code);
            if (methodMatches.Count > 0)
            {
                var last = methodMatches[methodMatches.Count - 1];
                result.ReturnType = last.Groups[1].Value?.Trim();
                result.MethodSignature = $"{last.Groups[1].Value?.Trim() ?? ""} {last.Groups[2].Value}({last.Groups[3].Value})".Trim();
                var paramStr = last.Groups[3].Value;
                if (!string.IsNullOrWhiteSpace(paramStr))
                {
                    foreach (var p in paramStr.Split(','))
                    {
                        var trimmed = p.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            result.MethodParameters.Add(trimmed);
                    }
                }
            }

            // 局部变量（取最后 10 个）
            var varMatches = CsVariable.Matches(code);
            int varStart = Math.Max(0, varMatches.Count - 10);
            for (int i = varStart; i < varMatches.Count; i++)
                result.LocalVariables.Add(varMatches[i].Value.Trim());

            return result;
        }

        private CodeStructureAnalysis AnalyzeJavaScript(string code)
        {
            var result = new CodeStructureAnalysis();

            foreach (Match m in JsImport.Matches(code))
                result.UsingStatements.Add(m.Value.Trim());

            var classMatches = JsClass.Matches(code);
            if (classMatches.Count > 0)
                result.ClassName = classMatches[classMatches.Count - 1].Groups[1].Value;

            var funcMatches = JsFunction.Matches(code);
            if (funcMatches.Count > 0)
            {
                var last = funcMatches[funcMatches.Count - 1];
                for (int g = 1; g < last.Groups.Count; g++)
                {
                    if (last.Groups[g].Success)
                    {
                        result.MethodSignature = $"function {last.Groups[g].Value}()";
                        break;
                    }
                }
            }

            var varMatches = JsVariable.Matches(code);
            int varStart = Math.Max(0, varMatches.Count - 10);
            for (int i = varStart; i < varMatches.Count; i++)
                result.LocalVariables.Add(varMatches[i].Groups[1].Value);

            return result;
        }

        private CodeStructureAnalysis AnalyzePython(string code)
        {
            var result = new CodeStructureAnalysis();

            foreach (Match m in PyImport.Matches(code))
                result.UsingStatements.Add(m.Value.Trim());

            var classMatches = PyClass.Matches(code);
            if (classMatches.Count > 0)
                result.ClassName = classMatches[classMatches.Count - 1].Groups[1].Value;

            var funcMatches = PyFunction.Matches(code);
            if (funcMatches.Count > 0)
            {
                var last = funcMatches[funcMatches.Count - 1];
                result.MethodSignature = $"def {last.Groups[1].Value}({last.Groups[2].Value})";
                var paramStr = last.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(paramStr))
                {
                    foreach (var p in paramStr.Split(','))
                    {
                        var trimmed = p.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            result.MethodParameters.Add(trimmed);
                    }
                }
            }

            var varMatches = PyVariable.Matches(code);
            int varStart = Math.Max(0, varMatches.Count - 10);
            for (int i = varStart; i < varMatches.Count; i++)
            {
                var name = varMatches[i].Groups[1].Value;
                if (name != "class" && name != "def" && name != "import" && name != "from" && name != "return" && name != "if" && name != "elif" && name != "else" && name != "for" && name != "while" && name != "try" && name != "except" && name != "with" && name != "pass" && name != "raise" && name != "yield")
                    result.LocalVariables.Add(name);
            }

            return result;
        }

        private CodeStructureAnalysis AnalyzeCpp(string code)
        {
            var result = new CodeStructureAnalysis();

            foreach (Match m in CppInclude.Matches(code))
                result.UsingStatements.Add(m.Value.Trim());

            var nsMatches = CppNamespace.Matches(code);
            if (nsMatches.Count > 0)
                result.Namespace = nsMatches[nsMatches.Count - 1].Groups[1].Value;

            var classMatches = CppClass.Matches(code);
            if (classMatches.Count > 0)
                result.ClassName = classMatches[classMatches.Count - 1].Groups[1].Value;

            var funcMatches = CppFunction.Matches(code);
            if (funcMatches.Count > 0)
            {
                var last = funcMatches[funcMatches.Count - 1];
                result.ReturnType = last.Groups[1].Value?.Trim();
                result.MethodSignature = $"{last.Groups[1].Value?.Trim() ?? ""} {last.Groups[2].Value}({last.Groups[3].Value})".Trim();
            }

            return result;
        }

        private CodeStructureAnalysis AnalyzeGeneric(string code)
        {
            var result = new CodeStructureAnalysis();

            // 通用检测：找 import / using / include 行
            var lines = code.Split(new[] { '\n' }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("import ") || trimmed.StartsWith("from ") || trimmed.StartsWith("using ") || trimmed.StartsWith("#include ") || trimmed.StartsWith("require("))
                    result.UsingStatements.Add(line.Trim());
            }

            return result;
        }
    }
}
