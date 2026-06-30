using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 性能监控功能
    /// 监控注入性能、内存占用、解析耗时
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region Performance Data

        private class PerformanceMetrics
        {
            // 注入性能
            public Dictionary<Type, InjectionMetrics> InjectionStats = new Dictionary<Type, InjectionMetrics>();

            // 服务解析性能
            public Dictionary<Type, ResolveMetrics> ResolveStats = new Dictionary<Type, ResolveMetrics>();

            // 内存占用
            public long TotalServiceMemory = 0;
            public Dictionary<Type, long> ServiceMemoryUsage = new Dictionary<Type, long>();

            // 总体统计
            public int TotalInjectionCalls = 0;
            public double TotalInjectionTime = 0;
            public int TotalResolveCalls = 0;
            public double TotalResolveTime = 0;
        }

        private class InjectionMetrics
        {
            public Type TargetType;
            public int CallCount = 0;
            public double TotalTime = 0;
            public double MinTime = double.MaxValue;
            public double MaxTime = 0;
            public double AvgTime => CallCount > 0 ? TotalTime / CallCount : 0;
        }

        private class ResolveMetrics
        {
            public Type ServiceType;
            public int CallCount = 0;
            public double TotalTime = 0;
            public double MinTime = double.MaxValue;
            public double MaxTime = 0;
            public double AvgTime => CallCount > 0 ? TotalTime / CallCount : 0;
        }

        private PerformanceMetrics _performanceMetrics = new PerformanceMetrics();
        private bool _isMonitoring = false;
        private double _lastMonitoringUpdate = 0;

        #endregion

        #region Performance Tab

        private Vector2 _perfScrollPosition;

        private void DrawPerformanceTab()
        {
            EditorGUILayout.Space();

            // 监控控制
            DrawMonitoringControls();

            EditorGUILayout.Space();

            if (_performanceMetrics.TotalInjectionCalls == 0 && _performanceMetrics.TotalResolveCalls == 0)
            {
                EditorGUILayout.HelpBox("⏱️ 性能监控\n\n" +
                    "性能监控需要在 DI 系统中添加埋点代码。\n\n" +
                    "【如何启用真实数据追踪】\n\n" +
                    "1. 在 Injection.Inject() 方法中添加计时：\n" +
                    "   var sw = Stopwatch.StartNew();\n" +
                    "   // ...注入逻辑\n" +
                    "   sw.Stop();\n" +
                    "   RecordInjection(targetType, sw.Elapsed.TotalMilliseconds);\n\n" +
                    "2. 在 LServices.Get<T>() 方法中添加计时：\n" +
                    "   var sw = Stopwatch.StartNew();\n" +
                    "   // ...解析逻辑\n" +
                    "   sw.Stop();\n" +
                    "   RecordResolve(typeof(T), sw.Elapsed.TotalMilliseconds);\n\n" +
                    "3. 创建公共方法供 DI 系统调用：\n" +
                    "   public static void RecordInjection(Type type, double time)\n" +
                    "   public static void RecordResolve(Type type, double time)\n\n" +
                    "当前显示的是空数据，等待真实调用。", MessageType.Info);
                return;
            }

            _perfScrollPosition = EditorGUILayout.BeginScrollView(_perfScrollPosition);

            // 总体性能
            DrawOverallPerformance();

            EditorGUILayout.Space();

            // 注入性能
            DrawInjectionPerformance();

            EditorGUILayout.Space();

            // 服务解析性能
            DrawResolvePerformance();

            EditorGUILayout.Space();

            // 内存占用
            DrawMemoryUsage();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMonitoringControls()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (_isMonitoring)
            {
                var style = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
                EditorGUILayout.LabelField("🟢 监控中...", style);

                if (GUILayout.Button("停止监控", GUILayout.Width(100)))
                {
                    StopMonitoring();
                }
            }
            else
            {
                EditorGUILayout.LabelField("⚪ 未监控", EditorStyles.label);

                if (GUILayout.Button("开始监控", GUILayout.Width(100)))
                {
                    StartMonitoring();
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("重置数据", GUILayout.Width(100)))
            {
                ResetPerformanceMetrics();
            }

            if (GUILayout.Button("刷新内存", GUILayout.Width(100)))
            {
                CalculateMemoryUsage();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverallPerformance()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 总体性能", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总注入调用", GUILayout.Width(150));
            EditorGUILayout.LabelField(_performanceMetrics.TotalInjectionCalls.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总注入耗时", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{_performanceMetrics.TotalInjectionTime:F2} ms", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总解析调用", GUILayout.Width(150));
            EditorGUILayout.LabelField(_performanceMetrics.TotalResolveCalls.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("总解析耗时", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{_performanceMetrics.TotalResolveTime:F2} ms", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("服务总内存", GUILayout.Width(150));
            EditorGUILayout.LabelField(FormatBytes(_performanceMetrics.TotalServiceMemory), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private void DrawInjectionPerformance()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💉 注入性能排行 (耗时最高 TOP 10)", EditorStyles.boldLabel);

            if (_performanceMetrics.InjectionStats.Count == 0)
            {
                EditorGUILayout.LabelField("暂无数据", EditorStyles.miniLabel);
            }
            else
            {
                var topInjections = _performanceMetrics.InjectionStats.Values
                    .OrderByDescending(m => m.AvgTime)
                    .Take(10)
                    .ToList();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("类型", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("调用次数", EditorStyles.boldLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField("平均耗时", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("最小", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("最大", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                foreach (var metrics in topInjections)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(metrics.TargetType.Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField(metrics.CallCount.ToString(), GUILayout.Width(70));
                    EditorGUILayout.LabelField($"{metrics.AvgTime:F3} ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{metrics.MinTime:F3} ms", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"{metrics.MaxTime:F3} ms", GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResolvePerformance()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔍 服务解析性能排行 (调用最频繁 TOP 10)", EditorStyles.boldLabel);

            if (_performanceMetrics.ResolveStats.Count == 0)
            {
                EditorGUILayout.LabelField("暂无数据", EditorStyles.miniLabel);
            }
            else
            {
                var topResolves = _performanceMetrics.ResolveStats.Values
                    .OrderByDescending(m => m.CallCount)
                    .Take(10)
                    .ToList();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("服务类型", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField("调用次数", EditorStyles.boldLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField("平均耗时", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("总耗时", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                foreach (var metrics in topResolves)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(metrics.ServiceType.Name, GUILayout.Width(200));
                    EditorGUILayout.LabelField(metrics.CallCount.ToString(), GUILayout.Width(70));
                    EditorGUILayout.LabelField($"{metrics.AvgTime:F3} ms", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"{metrics.TotalTime:F2} ms", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMemoryUsage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("💾 服务内存占用 TOP 10", EditorStyles.boldLabel);

            if (_performanceMetrics.ServiceMemoryUsage.Count == 0)
            {
                EditorGUILayout.LabelField("点击「刷新内存」来计算内存占用", EditorStyles.miniLabel);
            }
            else
            {
                var topMemory = _performanceMetrics.ServiceMemoryUsage
                    .OrderByDescending(kv => kv.Value)
                    .Take(10)
                    .ToList();

                foreach (var item in topMemory)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(item.Key.Name, GUILayout.Width(250));

                    var percentage = (float)item.Value / Math.Max(1, _performanceMetrics.TotalServiceMemory);
                    var rect = GUILayoutUtility.GetRect(150, 18);
                    EditorGUI.ProgressBar(rect, percentage, FormatBytes(item.Value));

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Performance Monitoring

        private void StartMonitoring()
        {
            _isMonitoring = true;
            _lastMonitoringUpdate = EditorApplication.timeSinceStartup;
            Debug.Log("[InjectionDebugWindow] 性能监控已启动");
        }

        private void StopMonitoring()
        {
            _isMonitoring = false;
            Debug.Log("[InjectionDebugWindow] 性能监控已停止");
        }

        private void UpdatePerformanceMonitoring()
        {
            if (!_isMonitoring)
                return;

            var now = EditorApplication.timeSinceStartup;
            if (now - _lastMonitoringUpdate < 1.0) // 每秒更新一次
                return;

            _lastMonitoringUpdate = now;

            // 只在性能监控标签页时刷新显示
            // 不再自动收集模拟数据
            if (_selectedTab == 6) // 性能监控标签页
            {
                Repaint();
            }
        }

        private void CollectPerformanceData()
        {
            // 这个方法现在保留为空，等待真实的埋点数据
            // 实际项目中，这里的数据应该来自 Injection.Inject() 和 LServices.Get() 的埋点
        }

        private void ResetPerformanceMetrics()
        {
            _performanceMetrics = new PerformanceMetrics();
            Debug.Log("[InjectionDebugWindow] 性能数据已重置");
            Repaint();
        }

        private void CalculateMemoryUsage()
        {
            _performanceMetrics.ServiceMemoryUsage.Clear();
            _performanceMetrics.TotalServiceMemory = 0;

            foreach (var service in _serviceCache.Values)
            {
                if (service.Instance == null)
                    continue;

                try
                {
                    // 估算内存占用（这是一个粗略估算）
                    long estimatedSize = EstimateObjectSize(service.Instance);

                    _performanceMetrics.ServiceMemoryUsage[service.ServiceType] = estimatedSize;
                    _performanceMetrics.TotalServiceMemory += estimatedSize;
                }
                catch
                {
                    // 某些对象可能无法访问
                }
            }

            Debug.Log($"[InjectionDebugWindow] 内存占用已更新：{FormatBytes(_performanceMetrics.TotalServiceMemory)}");
            Repaint();
        }

        private long EstimateObjectSize(object obj)
        {
            if (obj == null)
                return 0;

            long size = 0;
            var type = obj.GetType();

            // 基础类型大小
            if (type.IsValueType)
            {
                size = System.Runtime.InteropServices.Marshal.SizeOf(type);
            }
            else
            {
                size = IntPtr.Size; // 引用大小

                // 字符串特殊处理
                if (obj is string str)
                {
                    size += str.Length * 2;
                }
                else
                {
                    // 遍历所有字段
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType.IsValueType)
                        {
                            size += System.Runtime.InteropServices.Marshal.SizeOf(field.FieldType);
                        }
                        else
                        {
                            size += IntPtr.Size;
                        }
                    }
                }
            }

            return size;
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        #endregion

        #region Public API for DI System

        /// <summary>
        /// 记录注入操作性能
        /// 在 Injection.Inject() 中调用
        /// </summary>
        public static void RecordInjection(Type targetType, double elapsedMilliseconds)
        {
            // 使用 Resources.FindObjectsOfTypeAll 避免创建或激活窗口
            var windows = Resources.FindObjectsOfTypeAll<InjectionDebugWindow>();
            if (windows == null || windows.Length == 0)
                return;

            var window = windows[0];
            if (!window._isMonitoring)
                return;

            var metrics = window._performanceMetrics;

            if (!metrics.InjectionStats.ContainsKey(targetType))
            {
                metrics.InjectionStats[targetType] = new InjectionMetrics
                {
                    TargetType = targetType
                };
            }

            var stat = metrics.InjectionStats[targetType];
            stat.CallCount++;
            stat.TotalTime += elapsedMilliseconds;
            stat.MinTime = Math.Min(stat.MinTime, elapsedMilliseconds);
            stat.MaxTime = Math.Max(stat.MaxTime, elapsedMilliseconds);

            metrics.TotalInjectionCalls++;
            metrics.TotalInjectionTime += elapsedMilliseconds;
        }

        /// <summary>
        /// 记录服务解析性能
        /// 在 LServices.Get<T>() 中调用
        /// </summary>
        public static void RecordResolve(Type serviceType, double elapsedMilliseconds)
        {
            // 使用 Resources.FindObjectsOfTypeAll 避免创建或激活窗口
            var windows = Resources.FindObjectsOfTypeAll<InjectionDebugWindow>();
            if (windows == null || windows.Length == 0)
                return;

            var window = windows[0];
            if (!window._isMonitoring)
                return;

            var metrics = window._performanceMetrics;

            if (!metrics.ResolveStats.ContainsKey(serviceType))
            {
                metrics.ResolveStats[serviceType] = new ResolveMetrics
                {
                    ServiceType = serviceType
                };
            }

            var stat = metrics.ResolveStats[serviceType];
            stat.CallCount++;
            stat.TotalTime += elapsedMilliseconds;
            stat.MinTime = Math.Min(stat.MinTime, elapsedMilliseconds);
            stat.MaxTime = Math.Max(stat.MaxTime, elapsedMilliseconds);

            metrics.TotalResolveCalls++;
            metrics.TotalResolveTime += elapsedMilliseconds;
        }

        #endregion
    }
}
