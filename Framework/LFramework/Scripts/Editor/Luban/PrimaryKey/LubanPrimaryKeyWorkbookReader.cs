using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;

namespace Luban.Editor.PrimaryKey
{
    public static class LubanPrimaryKeyWorkbookReader
    {
        static LubanPrimaryKeyWorkbookReader()
        {
            TryRegisterCodePagesProvider();
        }

        public static string ResolveDataRoot(LubanExportConfig exportConfig)
        {
            if (exportConfig == null)
            {
                throw new ArgumentNullException(nameof(exportConfig));
            }

            string confAbsolutePath = Path.GetFullPath(exportConfig.luban_conf_path);
            string confDirectory = Path.GetDirectoryName(confAbsolutePath)
                                   ?? throw new InvalidOperationException("Cannot resolve Luban conf directory.");
            return Path.GetFullPath(Path.Combine(confDirectory, exportConfig.config.data_dir));
        }

        public static string ResolveWorkbookPath(LubanExportConfig exportConfig, string tableName)
        {
            string dataRoot = ResolveDataRoot(exportConfig);
            if (!Directory.Exists(dataRoot))
            {
                throw new DirectoryNotFoundException($"Luban data directory does not exist: {dataRoot}");
            }

            string[] matches = Directory.GetFiles(dataRoot, $"{tableName}.xlsx", SearchOption.AllDirectories);
            if (matches.Length == 0)
            {
                throw new FileNotFoundException($"Cannot find workbook for table '{tableName}' under '{dataRoot}'.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Found multiple workbooks for table '{tableName}': {string.Join(", ", matches)}");
            }

            return matches[0];
        }

        public static string GetTableNameFromWorkbookPath(string workbookPath)
        {
            if (string.IsNullOrWhiteSpace(workbookPath))
            {
                throw new ArgumentException("Workbook path cannot be null or empty.", nameof(workbookPath));
            }

            return Path.GetFileNameWithoutExtension(workbookPath);
        }

        public static List<Dictionary<string, string>> ReadRows(string workbookPath)
        {
            using var stream = File.Open(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var rows = new List<Dictionary<string, string>>();
            string[] headers = null;

            while (reader.Read())
            {
                string firstCellValue = reader.FieldCount > 0 ? reader.GetValue(0)?.ToString() ?? string.Empty : string.Empty;
                if (headers == null)
                {
                    if (IsHeaderRow(firstCellValue))
                    {
                        headers = ReadHeaders(reader);
                    }

                    continue;
                }

                if (ShouldSkipRow(firstCellValue))
                {
                    continue;
                }

                var row = ReadDataRow(reader, headers);
                if (row.Count == 0 || row.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                rows.Add(row);
            }

            return rows;
        }

        public static IReadOnlyList<string> ReadHeaderNames(string workbookPath)
        {
            using var stream = File.Open(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            while (reader.Read())
            {
                string firstCellValue = reader.FieldCount > 0 ? reader.GetValue(0)?.ToString() ?? string.Empty : string.Empty;
                if (IsHeaderRow(firstCellValue))
                {
                    return ReadHeaders(reader);
                }
            }

            return Array.Empty<string>();
        }

        private static bool IsHeaderRow(string firstCellValue)
        {
            return string.Equals(firstCellValue?.Trim(), "##var", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldSkipRow(string firstCellValue)
        {
            return !string.IsNullOrWhiteSpace(firstCellValue);
        }

        private static string[] ReadHeaders(IExcelDataReader reader)
        {
            var headers = new List<string>();
            for (int index = 1; index < reader.FieldCount; index++)
            {
                string value = reader.GetValue(index)?.ToString() ?? string.Empty;
                int markerIndex = value.IndexOf('#');
                string header = markerIndex >= 0 ? value.Substring(0, markerIndex) : value;
                if (!string.IsNullOrWhiteSpace(header))
                {
                    headers.Add(header);
                }
            }

            return headers.ToArray();
        }

        private static Dictionary<string, string> ReadDataRow(IExcelDataReader reader, string[] headers)
        {
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int index = 0; index < headers.Length; index++)
            {
                string header = headers[index];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                int columnIndex = index + 1;
                string value = columnIndex < reader.FieldCount
                    ? reader.GetValue(columnIndex)?.ToString() ?? string.Empty
                    : string.Empty;
                row[header] = value;
            }

            return row;
        }

        private static void TryRegisterCodePagesProvider()
        {
            const string typeName =
                "System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages";
            Type providerType = Type.GetType(typeName, throwOnError: false);
            if (providerType == null)
            {
                return;
            }

            var instanceProperty = providerType.GetProperty("Instance");
            if (instanceProperty?.GetValue(null) is EncodingProvider provider)
            {
                Encoding.RegisterProvider(provider);
            }
        }
    }
}
