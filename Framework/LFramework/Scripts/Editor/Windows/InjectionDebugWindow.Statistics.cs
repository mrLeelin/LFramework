using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 统计分析功能
    /// 数据洞察和 TOP 排行榜
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region Statistics Tab

        private void DrawStatisticsTab()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("📊 统计分析", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 基础统计
            DrawBasicStatistics();

            EditorGUILayout.Space();

            // TOP 排行榜
            DrawTopRankings();

            EditorGUILayout.Space();

            // 程序集分布
            DrawAssemblyDistribution();
        }

        private void DrawBasicStatistics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("基础统计", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("注册的服务", GUILayout.Width(150));
            EditorGUILayout.LabelField(_serviceCache.Count.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("动态注入器", GUILayout.Width(150));
            EditorGUILayout.LabelField(_injectorCache.Count.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("注入点", GUILayout.Width(150));
            EditorGUILayout.LabelField(_injectPointCache.Count.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            var ownedCount = _serviceCache.Values.Count(s => s.IsOwned);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Owned 服务", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{ownedCount} ({(float)ownedCount / Math.Max(1, _serviceCache.Count) * 100:F1}%)", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawTopRankings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🏆 TOP 排行榜", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 最常被注入的服务
            EditorGUILayout.LabelField("最常被注入的服务 TOP 10:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var serviceUsage = new Dictionary<Type, int>();
            foreach (var point in _injectPointCache)
            {
                if (!serviceUsage.ContainsKey(point.ServiceType))
                    serviceUsage[point.ServiceType] = 0;
                serviceUsage[point.ServiceType]++;
            }

            var topServices = serviceUsage.OrderByDescending(kv => kv.Value).Take(10).ToList();

            if (topServices.Count == 0)
            {
                EditorGUILayout.LabelField("暂无数据", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < topServices.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                    EditorGUILayout.LabelField(topServices[i].Key.Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"({topServices[i].Value} 处)", EditorStyles.miniLabel);

                    if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        _selectedTab = 2;
                        _searchText = topServices[i].Key.Name;
                        Repaint();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // 依赖最多的类
            EditorGUILayout.LabelField("依赖最多的类 TOP 10:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var classDependencies = _injectPointCache.GroupBy(p => p.DeclaringType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            if (classDependencies.Count == 0)
            {
                EditorGUILayout.LabelField("暂无数据", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < classDependencies.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                    EditorGUILayout.LabelField(classDependencies[i].Type.Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"({classDependencies[i].Count} 个依赖)", EditorStyles.miniLabel);

                    if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        _selectedTab = 2;
                        _searchText = classDependencies[i].Type.Name;
                        Repaint();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawAssemblyDistribution()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 程序集分布", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 按程序集统计注入点
            var assemblyGroups = _injectPointCache.GroupBy(p => p.DeclaringType.Assembly.GetName().Name)
                .Select(g => new { Assembly = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            if (assemblyGroups.Count == 0)
            {
                EditorGUILayout.LabelField("暂无数据", EditorStyles.miniLabel);
            }
            else
            {
                var totalCount = _injectPointCache.Count;

                foreach (var group in assemblyGroups)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(group.Assembly, GUILayout.Width(250));

                    var percentage = (float)group.Count / totalCount;
                    var rect = GUILayoutUtility.GetRect(100, 18);
                    EditorGUI.ProgressBar(rect, percentage, $"{group.Count} ({percentage * 100:F1}%)");

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
