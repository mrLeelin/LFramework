using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 导出功能
    /// 支持导出为 JSON/CSV/Markdown
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region Export UI

        private void DrawExportButton()
        {
            GUIStyle style = _embeddedHost ? EditorStyles.miniButton : EditorStyles.toolbarButton;
            GUILayoutOption[] options = _embeddedHost
                ? new[] { GUILayout.Width(50f), GUILayout.Height(22f) }
                : new[] { GUILayout.Width(50f) };

            if (GUILayout.Button("导出", style, options))
            {
                ShowExportMenu();
            }
        }

        private void ShowExportMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("导出为 JSON"), false, () => ExportToJson());
            menu.AddItem(new GUIContent("导出为 CSV"), false, () => ExportToCsv());
            menu.AddItem(new GUIContent("导出为 Markdown"), false, () => ExportToMarkdown());
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("导出完整报告"), false, () => ExportFullReport());

            menu.ShowAsContext();
        }

        #endregion

        #region Export Logic

        private void ExportToJson()
        {
            var data = new
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                services = _serviceCache.Values.Select(s => new
                {
                    type = s.ServiceType.FullName,
                    identifier = s.Identifier?.ToString(),
                    isOwned = s.IsOwned,
                    scope = s.ScopeName,
                    instanceHashCode = s.Instance?.GetHashCode()
                }).ToList(),
                injectors = _injectorCache.Select(i => new
                {
                    targetType = i.TargetType.FullName,
                    assembly = i.TargetType.Assembly.GetName().Name
                }).ToList(),
                injectPoints = _injectPointCache.Select(p => new
                {
                    declaringType = p.DeclaringType.FullName,
                    memberName = p.MemberName,
                    memberType = p.MemberType,
                    serviceType = p.ServiceType.FullName,
                    isRegistered = IsServiceRegistered(p.ServiceType)
                }).ToList(),
                issues = _validationIssues.Select(i => new
                {
                    severity = i.Severity.ToString(),
                    title = i.Title,
                    description = i.Description,
                    location = i.Location
                }).ToList()
            };

            var json = JsonUtility.ToJson(data, true);
            SaveToFile("injection_report.json", json);
        }

        private void ExportToCsv()
        {
            StringBuilder csv = new StringBuilder();

            // 服务列表
            csv.AppendLine("=== Services ===");
            csv.AppendLine("Type,Identifier,IsOwned,Scope,InstanceHashCode");
            foreach (var service in _serviceCache.Values)
            {
                csv.AppendLine($"\"{service.ServiceType.FullName}\",\"{service.Identifier}\",{service.IsOwned},{service.ScopeName},{service.Instance?.GetHashCode()}");
            }

            csv.AppendLine();
            csv.AppendLine("=== Inject Points ===");
            csv.AppendLine("DeclaringType,MemberName,MemberType,ServiceType,IsRegistered");
            foreach (var point in _injectPointCache)
            {
                csv.AppendLine($"\"{point.DeclaringType.FullName}\",{point.MemberName},{point.MemberType},\"{point.ServiceType.FullName}\",{IsServiceRegistered(point.ServiceType)}");
            }

            SaveToFile("injection_report.csv", csv.ToString());
        }

        private void ExportToMarkdown()
        {
            StringBuilder md = new StringBuilder();

            md.AppendLine("# Injection System Report");
            md.AppendLine();
            md.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine();

            // 统计信息
            md.AppendLine("## 📊 Statistics");
            md.AppendLine();
            md.AppendLine($"- **Services:** {_serviceCache.Count}");
            md.AppendLine($"- **Injectors:** {_injectorCache.Count}");
            md.AppendLine($"- **Inject Points:** {_injectPointCache.Count}");
            md.AppendLine($"- **Issues:** {_validationIssues.Count} ({_validationIssues.Count(i => i.Severity == IssueSeverity.Error)} errors, {_validationIssues.Count(i => i.Severity == IssueSeverity.Warning)} warnings)");
            md.AppendLine();

            // 问题列表
            if (_validationIssues.Count > 0)
            {
                md.AppendLine("## ⚠️ Issues");
                md.AppendLine();
                foreach (var issue in _validationIssues)
                {
                    var icon = issue.Severity == IssueSeverity.Error ? "🔴" : "⚠️";
                    md.AppendLine($"### {icon} {issue.Title}");
                    md.AppendLine();
                    md.AppendLine(issue.Description);
                    md.AppendLine();
                    if (!string.IsNullOrEmpty(issue.Location))
                    {
                        md.AppendLine($"**Location:** `{issue.Location}`");
                        md.AppendLine();
                    }
                }
            }

            // 服务列表
            md.AppendLine("## 📦 Registered Services");
            md.AppendLine();
            md.AppendLine("| Type | Identifier | Owned | Scope |");
            md.AppendLine("|------|------------|-------|-------|");
            foreach (var service in _serviceCache.Values.OrderBy(s => s.ServiceType.Name))
            {
                var owned = service.IsOwned ? "✓" : "";
                md.AppendLine($"| `{service.ServiceType.Name}` | {service.Identifier ?? "-"} | {owned} | {service.ScopeName} |");
            }
            md.AppendLine();

            // TOP 排行
            md.AppendLine("## 🏆 Top Rankings");
            md.AppendLine();

            var serviceUsage = new Dictionary<Type, int>();
            foreach (var point in _injectPointCache)
            {
                if (!serviceUsage.ContainsKey(point.ServiceType))
                    serviceUsage[point.ServiceType] = 0;
                serviceUsage[point.ServiceType]++;
            }

            md.AppendLine("### Most Injected Services");
            md.AppendLine();
            var topServices = serviceUsage.OrderByDescending(kv => kv.Value).Take(10).ToList();
            for (int i = 0; i < topServices.Count; i++)
            {
                md.AppendLine($"{i + 1}. **{topServices[i].Key.Name}** - {topServices[i].Value} usages");
            }
            md.AppendLine();

            SaveToFile("injection_report.md", md.ToString());
        }

        private void ExportFullReport()
        {
            ExportToJson();
            ExportToMarkdown();
            Debug.Log("[InjectionDebugWindow] 完整报告已导出（JSON + Markdown）");
        }

        private void SaveToFile(string fileName, string content)
        {
            var path = EditorUtility.SaveFilePanel(
                "导出 Injection 报告",
                Application.dataPath,
                fileName,
                Path.GetExtension(fileName).TrimStart('.')
            );

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                File.WriteAllText(path, content, Encoding.UTF8);
                Debug.Log($"[InjectionDebugWindow] 报告已导出到: {path}");
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InjectionDebugWindow] 导出失败: {ex.Message}");
            }
        }

        #endregion
    }
}
