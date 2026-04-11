using System;
using System.Collections.Generic;
using System.Text;

namespace Luban.Editor.PrimaryKey
{
    public static class LubanPrimaryKeyNameUtility
    {
        private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        public static string SanitizeIdentifier(string raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException(nameof(raw));
            }

            string trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                throw new InvalidOperationException("Primary key value cannot be empty after trimming.");
            }

            var builder = new StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            string result = builder.ToString();
            if (result.Length == 0)
            {
                throw new InvalidOperationException($"Primary key '{raw}' cannot be converted to a valid identifier.");
            }

            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            if (CSharpKeywords.Contains(result))
            {
                result = "_" + result;
            }

            return result;
        }
    }
}
