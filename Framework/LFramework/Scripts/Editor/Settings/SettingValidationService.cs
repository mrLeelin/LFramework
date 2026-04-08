using System.Collections.Generic;
using System.Linq;
using GameFramework;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Settings
{
    public enum SettingValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public enum SettingValidationCode
    {
        MissingProjectSelector = 0,
        MissingSyncState = 1,
        DuplicateSettingId = 2,
        InvalidBindType = 3,
        MissingRequiredSetting = 4,
        DirectTemplateReference = 5
    }

    public sealed class SettingValidationIssue
    {
        public SettingValidationSeverity severity;
        public SettingValidationCode code;
        public string message;
        public Object target;
    }

    public sealed class SettingValidationReport
    {
        public List<SettingValidationIssue> Issues { get; } = new();
        public bool HasErrors => Issues.Any(issue => issue.severity == SettingValidationSeverity.Error);
    }

    /// <summary>
    /// 工程侧 Setting 校验服务。
    /// </summary>
    public static class SettingValidationService
    {
        public static SettingValidationReport Validate(ProjectSettingSelector selector, SettingSyncState syncState)
        {
            return Validate(selector, syncState, null);
        }

        public static SettingValidationReport Validate(
            ProjectSettingSelector selector,
            SettingSyncState syncState,
            IEnumerable<ScriptableObject> templates)
        {
            var report = new SettingValidationReport();

            if (selector == null)
            {
                report.Issues.Add(new SettingValidationIssue
                {
                    severity = SettingValidationSeverity.Error,
                    code = SettingValidationCode.MissingProjectSelector,
                    message = "ProjectSettingSelector is missing."
                });
            }

            if (syncState == null)
            {
                report.Issues.Add(new SettingValidationIssue
                {
                    severity = SettingValidationSeverity.Error,
                    code = SettingValidationCode.MissingSyncState,
                    message = "SettingSyncState is missing."
                });
            }

            if (selector == null)
            {
                return report;
            }

            ValidateDuplicateSettingIds(selector, report);
            ValidateComponentBindTypes(selector, report);
            ValidateRequiredSettings(selector, report, templates);
            ValidateDirectTemplateReferences(selector, report, templates);
            return report;
        }

        private static void ValidateDuplicateSettingIds(ProjectSettingSelector selector, SettingValidationReport report)
        {
            var allSettings = selector.GetAllSettings().Cast<Object>()
                .Concat(selector.GetAllComponentSettings())
                .OfType<Object>()
                .Select(setting => new
                {
                    target = setting,
                    settingId = setting switch
                    {
                        BaseSetting baseSetting => baseSetting.SettingId,
                        ComponentSetting componentSetting => componentSetting.SettingId,
                        _ => null
                    }
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.settingId))
                .GroupBy(item => item.settingId)
                .Where(group => group.Count() > 1);

            foreach (var duplicate in allSettings)
            {
                report.Issues.Add(new SettingValidationIssue
                {
                    severity = SettingValidationSeverity.Error,
                    code = SettingValidationCode.DuplicateSettingId,
                    message = $"Duplicate settingId detected: {duplicate.Key}",
                    target = duplicate.First().target
                });
            }
        }

        private static void ValidateComponentBindTypes(ProjectSettingSelector selector, SettingValidationReport report)
        {
            foreach (ComponentSetting setting in selector.GetAllComponentSettings())
            {
                if (setting == null || string.IsNullOrWhiteSpace(setting.bindTypeName))
                {
                    continue;
                }

                var componentType = Utility.Assembly.GetType(setting.bindTypeName);
                bool valid = componentType != null &&
                             !componentType.IsAbstract &&
                             !componentType.IsInterface &&
                             typeof(GameFrameworkComponent).IsAssignableFrom(componentType);

                if (!valid)
                {
                    report.Issues.Add(new SettingValidationIssue
                    {
                        severity = SettingValidationSeverity.Error,
                        code = SettingValidationCode.InvalidBindType,
                        message = $"Invalid bindTypeName: {setting.bindTypeName}",
                        target = setting
                    });
                }
            }
        }

        private static void ValidateRequiredSettings(
            ProjectSettingSelector selector,
            SettingValidationReport report,
            IEnumerable<ScriptableObject> templates)
        {
            if (templates == null)
            {
                return;
            }

            HashSet<string> localSettingIds = selector.GetAllSettings().Select(setting => setting.SettingId)
                .Concat(selector.GetAllComponentSettings().Select(setting => setting.SettingId))
                .ToHashSet();

            foreach (ScriptableObject template in templates.Where(template => template != null))
            {
                string settingId = GetSettingId(template);
                if (!localSettingIds.Contains(settingId))
                {
                    report.Issues.Add(new SettingValidationIssue
                    {
                        severity = SettingValidationSeverity.Error,
                        code = SettingValidationCode.MissingRequiredSetting,
                        message = $"Required setting is missing: {settingId}",
                        target = template
                    });
                }
            }
        }

        private static void ValidateDirectTemplateReferences(
            ProjectSettingSelector selector,
            SettingValidationReport report,
            IEnumerable<ScriptableObject> templates)
        {
            if (templates == null)
            {
                return;
            }

            var templateSet = templates.Where(template => template != null).ToHashSet();
            foreach (Object local in selector.GetAllSettings().Cast<Object>().Concat(selector.GetAllComponentSettings()))
            {
                if (templateSet.Contains(local))
                {
                    report.Issues.Add(new SettingValidationIssue
                    {
                        severity = SettingValidationSeverity.Error,
                        code = SettingValidationCode.DirectTemplateReference,
                        message = $"Project selector references template asset directly: {local.name}",
                        target = local
                    });
                }
            }
        }

        private static string GetSettingId(Object setting)
        {
            return setting switch
            {
                BaseSetting baseSetting => baseSetting.SettingId,
                ComponentSetting componentSetting => componentSetting.SettingId,
                _ => setting.GetType().FullName
            };
        }
    }
}
