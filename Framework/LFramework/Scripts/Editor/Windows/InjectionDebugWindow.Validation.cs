using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 实时验证功能
    /// 自动检测配置问题
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region Validation Data

        private enum IssueSeverity
        {
            Warning,
            Error
        }

        private class ValidationIssue
        {
            public IssueSeverity Severity;
            public string Title;
            public string Description;
            public string Location;
            public Action OnClick;
        }

        #endregion

        #region Validation Tab

        private void DrawValidationTab()
        {
            if (_validationIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ 未发现配置问题！\n\n系统运行正常。", MessageType.Info);

                if (GUILayout.Button("重新检查", GUILayout.Height(30)))
                {
                    ValidateConfiguration();
                    Repaint();
                }
                return;
            }

            EditorGUILayout.Space();

            var errors = _validationIssues.Where(i => i.Severity == IssueSeverity.Error).ToList();
            var warnings = _validationIssues.Where(i => i.Severity == IssueSeverity.Warning).ToList();

            // 错误
            if (errors.Count > 0)
            {
                EditorGUILayout.LabelField($"⚠️ 错误 ({errors.Count})", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                foreach (var issue in errors)
                {
                    DrawValidationIssue(issue, MessageType.Error);
                }
            }

            // 警告
            if (warnings.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"⚠ 警告 ({warnings.Count})", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                foreach (var issue in warnings)
                {
                    DrawValidationIssue(issue, MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("重新检查", GUILayout.Height(30)))
            {
                ValidateConfiguration();
                Repaint();
            }
        }

        private void DrawValidationIssue(ValidationIssue issue, MessageType type)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // 图标
            string icon = type == MessageType.Error ? "console.erroricon" : "console.warnicon";
            GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(20), GUILayout.Height(20));

            // 标题
            EditorGUILayout.LabelField(issue.Title, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // 跳转按钮
            if (issue.OnClick != null)
            {
                if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    issue.OnClick.Invoke();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 描述
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedMiniLabel);

            if (!string.IsNullOrEmpty(issue.Location))
            {
                EditorGUILayout.LabelField("位置: " + issue.Location, EditorStyles.miniLabel);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Validation Logic

        private void ValidateConfiguration()
        {
            _validationIssues.Clear();

            var startTime = EditorApplication.timeSinceStartup;

            // 1. 检查未注册的服务
            ValidateUnregisteredServices();

            // 2. 检查重复注册
            ValidateDuplicateRegistrations();

            // 3. 检查类型匹配
            ValidateTypeMismatch();

            var elapsed = EditorApplication.timeSinceStartup - startTime;
            Debug.Log($"[InjectionDebugWindow] 验证完成：发现 {_validationIssues.Count} 个问题，耗时 {elapsed:F3} 秒");
        }

        private void ValidateUnregisteredServices()
        {
            // 查找所有注入点需要的服务类型
            var requiredServices = new HashSet<Type>();
            foreach (var point in _injectPointCache)
            {
                requiredServices.Add(point.ServiceType);
            }

            // 检查哪些服务未注册
            foreach (var serviceType in requiredServices)
            {
                bool isRegistered = _serviceCache.Values.Any(s => s.ServiceType == serviceType);

                if (!isRegistered)
                {
                    // 统计有多少个注入点需要这个服务
                    var usageCount = _injectPointCache.Count(p => p.ServiceType == serviceType);
                    var users = _injectPointCache.Where(p => p.ServiceType == serviceType)
                        .Select(p => p.DeclaringType.Name)
                        .Distinct()
                        .Take(3)
                        .ToList();

                    _validationIssues.Add(new ValidationIssue
                    {
                        Severity = IssueSeverity.Error,
                        Title = $"服务未注册: {serviceType.Name}",
                        Description = $"有 {usageCount} 个注入点需要此服务，但服务未注册。\n使用者: {string.Join(", ", users)}{(users.Count < usageCount ? "..." : "")}",
                        Location = serviceType.FullName,
                        OnClick = () =>
                        {
                            _selectedTab = 2; // 跳转到注入点信息
                            _searchText = serviceType.Name;
                            Repaint();
                        }
                    });
                }
            }
        }

        private void ValidateDuplicateRegistrations()
        {
            // 按服务类型分组
            var groups = _serviceCache.Values.GroupBy(s => s.ServiceType);

            foreach (var group in groups)
            {
                if (group.Count() > 1)
                {
                    // 检查是否有不同的标识符
                    var withoutId = group.Where(s => s.Identifier == null).ToList();

                    if (withoutId.Count > 1)
                    {
                        _validationIssues.Add(new ValidationIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Title = $"重复注册: {group.Key.Name}",
                            Description = $"同一服务类型注册了 {withoutId.Count} 次（无标识符），最后注册的会覆盖之前的。",
                            Location = group.Key.FullName,
                            OnClick = () =>
                            {
                                _selectedTab = 0; // 跳转到服务列表
                                _searchText = group.Key.Name;
                                Repaint();
                            }
                        });
                    }
                }
            }
        }

        private void ValidateTypeMismatch()
        {
            // 检查注入点类型和注册服务类型是否匹配
            foreach (var point in _injectPointCache)
            {
                var matchingServices = _serviceCache.Values.Where(s =>
                    point.ServiceType.IsAssignableFrom(s.ServiceType)).ToList();

                if (matchingServices.Count == 0)
                {
                    // 服务未注册（已在 ValidateUnregisteredServices 中处理）
                    continue;
                }

                // 检查是否有多个匹配的服务（可能导致歧义）
                if (matchingServices.Count > 1)
                {
                    var withoutId = matchingServices.Where(s => s.Identifier == null).ToList();

                    if (withoutId.Count > 1)
                    {
                        _validationIssues.Add(new ValidationIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Title = $"类型歧义: {point.DeclaringType.Name}.{point.MemberName}",
                            Description = $"注入点类型 {point.ServiceType.Name} 有 {withoutId.Count} 个匹配的服务，可能导致不确定的行为。",
                            Location = $"{point.DeclaringType.FullName}.{point.MemberName}",
                            OnClick = () =>
                            {
                                _selectedTab = 2;
                                _searchText = point.DeclaringType.Name;
                                Repaint();
                            }
                        });
                    }
                }
            }
        }

        #endregion
    }
}
