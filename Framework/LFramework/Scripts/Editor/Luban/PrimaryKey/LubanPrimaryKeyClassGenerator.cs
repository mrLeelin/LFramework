using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Luban.Editor.PrimaryKey
{
    public static class LubanPrimaryKeyClassGenerator
    {
        public static void GenerateAll(LubanExportConfig exportConfig, LubanPrimaryKeyGenerateConfig generateConfig)
        {
            if (exportConfig == null || generateConfig == null || generateConfig.Rules == null)
            {
                return;
            }

            foreach (LubanPrimaryKeyGenerateRule rule in generateConfig.Rules.Where(static r => r != null && r.Enable))
            {
                try
                {
                    string workbookPath = LubanPrimaryKeyWorkbookReader.ResolveWorkbookPath(exportConfig, rule.TableName);
                    IReadOnlyList<string> headerNames = LubanPrimaryKeyWorkbookReader.ReadHeaderNames(workbookPath);
                    List<Dictionary<string, string>> rows = LubanPrimaryKeyWorkbookReader.ReadRows(workbookPath);
                    string code = GenerateCode(generateConfig, rule, headerNames, rows);
                    WriteCodeFile(generateConfig, rule, code);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to generate primary key class for table '{rule.TableName}': {exception.Message}");
                }
            }

            AssetDatabase.Refresh();
        }

        public static string GenerateCode(
            LubanPrimaryKeyGenerateConfig generateConfig,
            LubanPrimaryKeyGenerateRule rule,
            IReadOnlyList<string> headerNames,
            IReadOnlyList<Dictionary<string, string>> rows)
        {
            if (generateConfig == null)
            {
                throw new ArgumentNullException(nameof(generateConfig));
            }

            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (string.IsNullOrWhiteSpace(rule.PrimaryKeyField))
            {
                throw new InvalidOperationException($"PrimaryKeyField is required for table '{rule.TableName}'.");
            }

            string outputClassName = ResolveOutputClassName(rule);
            IReadOnlyList<string> orderedCommentFields = ResolveOrderedCommentFields(rule, headerNames);
            var seenPrimaryKeys = new HashSet<string>(StringComparer.Ordinal);
            var seenIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            var members = new List<(string Identifier, string Value, List<string> Comments)>();

            foreach (Dictionary<string, string> row in rows)
            {
                if (!row.TryGetValue(rule.PrimaryKeyField, out string primaryKeyValue))
                {
                    throw new InvalidOperationException(
                        $"Table '{rule.TableName}' does not contain primary key field '{rule.PrimaryKeyField}'.");
                }

                if (string.IsNullOrWhiteSpace(primaryKeyValue))
                {
                    continue;
                }

                if (!seenPrimaryKeys.Add(primaryKeyValue))
                {
                    throw new InvalidOperationException(
                        $"Table '{rule.TableName}' contains duplicate primary key value '{primaryKeyValue}'.");
                }

                string identifier = LubanPrimaryKeyNameUtility.SanitizeIdentifier(primaryKeyValue);
                if (!seenIdentifiers.Add(identifier))
                {
                    throw new InvalidOperationException(
                        $"Table '{rule.TableName}' generates duplicate identifier '{identifier}' after sanitizing.");
                }

                List<string> commentLines = new();
                foreach (string commentField in orderedCommentFields)
                {
                    if (!row.TryGetValue(commentField, out string commentValue))
                    {
                        throw new InvalidOperationException(
                            $"Table '{rule.TableName}' does not contain comment field '{commentField}'.");
                    }

                    if (!string.IsNullOrWhiteSpace(commentValue))
                    {
                        commentLines.Add(commentValue);
                    }
                }

                members.Add((identifier, primaryKeyValue, commentLines));
            }

            var builder = new StringBuilder();
            builder.AppendLine("// Auto-generated. Do not modify.");
            if (!string.IsNullOrWhiteSpace(generateConfig.Namespace))
            {
                builder.AppendLine($"namespace {generateConfig.Namespace}");
                builder.AppendLine("{");
            }

            string indent = string.IsNullOrWhiteSpace(generateConfig.Namespace) ? string.Empty : "    ";
            builder.AppendLine($"{indent}public static class {outputClassName}");
            builder.AppendLine($"{indent}{{");
            foreach ((string identifier, string value, List<string> comments) in members)
            {
                AppendSummary(builder, indent + "    ", comments);
                builder.AppendLine($"{indent}    public const string {identifier} = {QuoteString(value)};");
                builder.AppendLine();
            }

            if (members.Count > 0)
            {
                builder.Length -= Environment.NewLine.Length;
            }

            builder.AppendLine($"{indent}}}");
            if (!string.IsNullOrWhiteSpace(generateConfig.Namespace))
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        internal static string ResolveOutputClassName(LubanPrimaryKeyGenerateRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (string.IsNullOrWhiteSpace(rule.TableName))
            {
                throw new InvalidOperationException("TableName is required to infer OutputClassName.");
            }

            return $"{rule.TableName.Trim()}SerialID";
        }

        internal static IReadOnlyList<string> ResolveOrderedCommentFields(
            LubanPrimaryKeyGenerateRule rule,
            IReadOnlyList<string> headerNames)
        {
            if (rule?.CommentFields == null || rule.CommentFields.Count == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> selectedFields = new(rule.CommentFields.Where(static f => !string.IsNullOrWhiteSpace(f)), StringComparer.Ordinal);
            if (selectedFields.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (headerNames == null || headerNames.Count == 0)
            {
                return rule.CommentFields.Where(static f => !string.IsNullOrWhiteSpace(f)).ToArray();
            }

            List<string> orderedFields = new();
            foreach (string headerName in headerNames)
            {
                if (selectedFields.Contains(headerName))
                {
                    orderedFields.Add(headerName);
                }
            }

            foreach (string selectedField in selectedFields)
            {
                if (!orderedFields.Contains(selectedField))
                {
                    orderedFields.Add(selectedField);
                }
            }

            return orderedFields;
        }

        private static void AppendSummary(StringBuilder builder, string indent, IReadOnlyList<string> comments)
        {
            if (comments == null || comments.Count == 0)
            {
                return;
            }

            builder.AppendLine($"{indent}/// <summary>");
            foreach (string comment in comments)
            {
                builder.AppendLine($"{indent}/// {comment}");
            }
            builder.AppendLine($"{indent}/// </summary>");
        }

        private static string QuoteString(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static void WriteCodeFile(
            LubanPrimaryKeyGenerateConfig generateConfig,
            LubanPrimaryKeyGenerateRule rule,
            string code)
        {
            string outputDir = Path.GetFullPath(generateConfig.OutputDir);
            Directory.CreateDirectory(outputDir);

            string outputPath = Path.Combine(outputDir, $"{ResolveOutputClassName(rule)}.cs");
            File.WriteAllText(outputPath, code, Encoding.UTF8);
            Debug.Log($"Generated primary key class: {outputPath}");
        }
    }
}
