using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    internal sealed class ProviderProfiled : ProfiledBase
    {
        private const double RefreshInterval = 0.5d;

        private readonly WorldProfiledRefreshGate _refreshGate = new(RefreshInterval);
        private readonly HashSet<SystemProviderBase> _collapsedProviders =
            new(ReferenceComparer<SystemProviderBase>.Instance);
        private string _searchText = string.Empty;
        private bool _updateOnly;
        private bool _showAllInterfaces = true;
        private Exception _refreshError;
        private ProviderProfiledSnapshot _snapshot;

        internal override string Title => "Provider";
        internal override string SubTitle => "查看当前 Provider、服务接口、生命周期和 Update 状态。";
        internal override bool CanDraw => true;

        internal override void Draw()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可查看当前活动 Provider。", MessageType.Info);
                return;
            }

            RefreshSnapshotIfNeeded();
            if (_refreshError != null)
            {
                EditorGUILayout.HelpBox(
                    $"读取 Provider 运行时数据失败：{_refreshError.GetType().Name}: {_refreshError.Message}",
                    MessageType.Error);
                if (_snapshot == null)
                {
                    if (GUILayout.Button("重试", GUILayout.Width(72f)))
                    {
                        RefreshSnapshotImmediately(EditorApplication.timeSinceStartup);
                    }

                    return;
                }
            }

            ProviderProfiledSnapshot snapshot = _snapshot;
            int subscribedCount = snapshot.Providers.Count(provider => provider.Diagnostics.IsSubscribed);
            int updateCount = snapshot.Providers.Count(provider => provider.Diagnostics.OverridesUpdate);
            int foreverCount = snapshot.Providers.Count(
                provider => provider.Diagnostics.LifeCycle == ProviderLifeCycle.Forever);

            DrawMetricCards(
                new ProfiledMetric("当前 Provider", snapshot.Providers.Count.ToString(), $"扫描 {snapshot.ScopeCount} 个服务作用域"),
                new ProfiledMetric("事件订阅", subscribedCount.ToString(), "当前已执行 SubscribeEvent"),
                new ProfiledMetric("Update", updateCount.ToString(), "覆写 UpdateComponent"),
                new ProfiledMetric("Forever", foreverCount.ToString(), "跨流程保留的 Provider"));

            DrawToolbar();
            ProviderProfiledProvider[] providers = snapshot.Providers.Where(MatchesFilter).ToArray();
            if (snapshot.Providers.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前没有在 LServices 中注册的 SystemProviderBase。进入包含 HotfixProcedure 的流程后，这里会显示活动 Provider。",
                    MessageType.Info);
                return;
            }

            if (providers.Length == 0)
            {
                EditorGUILayout.HelpBox("没有符合当前筛选条件的 Provider。", MessageType.Info);
                return;
            }

            foreach (ProviderProfiledProvider provider in providers)
            {
                DrawProvider(provider);
                GUILayout.Space(8f);
            }
        }

        private void DrawToolbar()
        {
            DrawSection("筛选", "按 Provider 类型、接口、服务键、作用域或生命周期筛选。", () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("搜索", GUILayout.Width(42f));
                _searchText = EditorGUILayout.TextField(_searchText);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_searchText)))
                {
                    if (GUILayout.Button("清除", GUILayout.Width(54f)))
                    {
                        _searchText = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                if (GUILayout.Button("立即刷新", GUILayout.Width(72f)))
                {
                    RefreshSnapshotImmediately(EditorApplication.timeSinceStartup);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                _updateOnly = EditorGUILayout.ToggleLeft("仅显示覆写 Update 的 Provider", _updateOnly);
                _showAllInterfaces = EditorGUILayout.ToggleLeft("显示全部接口", _showAllInterfaces);
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawProvider(ProviderProfiledProvider profiledProvider)
        {
            ProviderDiagnosticsSnapshot provider = profiledProvider.Diagnostics;
            DrawSection("Provider", "活动实例、生命周期阶段与 LServices 实际绑定。", () =>
            {
                bool expanded = !_collapsedProviders.Contains(provider.Instance);
                EditorGUILayout.BeginHorizontal();
                bool nextExpanded = EditorGUILayout.Foldout(expanded, provider.ProviderType.Name, true);
                SetCollapsed(provider.Instance, !nextExpanded);
                GUILayout.FlexibleSpace();
                DrawBadge(provider.Stage.ToString(), provider.Stage == ProviderDiagnosticsStage.Ready);
                DrawBadge("Subscribed", provider.IsSubscribed);
                DrawBadge("Update", provider.OverridesUpdate);
                DrawBadge(provider.LifeCycle.ToString(), provider.LifeCycle == ProviderLifeCycle.Forever);
                EditorGUILayout.EndHorizontal();

                if (!nextExpanded)
                {
                    return;
                }

                EditorGUI.indentLevel++;
                DrawKeyValueRow("完整类型", provider.ProviderType.FullName);
                DrawKeyValueRow("程序集", provider.ProviderType.Assembly.GetName().Name);
                DrawKeyValueRow("实例", FormatInstance(provider.Instance));
                DrawKeyValueRow("ProcedureState", provider.ProcedureState.ToString());
                DrawKeyValueRow("生命周期", provider.LifeCycle.ToString());
                DrawKeyValueRow("注册排序", provider.Sort.ToString());
                DrawKeyValueRow("当前阶段", provider.Stage.ToString());
                DrawKeyValueRow("事件订阅", provider.IsSubscribed ? "已订阅" : "未订阅");
                DrawKeyValueRow("Update 回调", provider.OverridesUpdate ? "已覆写并由 LSystemApplication 分发" : "使用空实现");
                DrawKeyValueRow("LateUpdate 回调", "Provider 当前无 LateUpdate 契约");
                DrawKeyValueRow("AwakeComponent", FormatOverride(provider.OverridesAwake));
                DrawKeyValueRow("SetUp", FormatOverride(provider.OverridesSetUp));
                DrawKeyValueRow("OnStop", FormatOverride(provider.OverridesOnStop));
                DrawKeyValueRow("Subscribe", FormatOverride(provider.OverridesSubscribe));
                DrawKeyValueRow("UnSubscribe", FormatOverride(provider.OverridesUnSubscribe));

                GUILayout.Space(3f);
                EditorGUILayout.LabelField("当前 LServices 绑定", EditorStyles.boldLabel);
                foreach (ProviderServiceRegistration registration in profiledProvider.ServiceRegistrations)
                {
                    string identifier = registration.Identifier == null ? "<default>" : registration.Identifier.ToString();
                    string ownership = registration.IsOwned ? "Owned" : "External";
                    EditorGUILayout.LabelField(
                        $"{registration.ServiceType.FullName}  ·  Id {identifier}  ·  {registration.ScopeName}  ·  {ownership}",
                        EditorStyles.wordWrappedMiniLabel);
                }

                if (_showAllInterfaces)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.LabelField("实现的 Interfaces", EditorStyles.boldLabel);
                    if (provider.ImplementedInterfaces.Count == 0)
                    {
                        EditorGUILayout.LabelField("无业务接口", EditorStyles.wordWrappedMiniLabel);
                    }
                    else
                    {
                        foreach (Type interfaceType in provider.ImplementedInterfaces)
                        {
                            string category = typeof(ISystemProvider).IsAssignableFrom(interfaceType)
                                ? "Provider"
                                : "Interface";
                            EditorGUILayout.LabelField(
                                $"[{category}] {interfaceType.FullName}",
                                EditorStyles.wordWrappedMiniLabel);
                        }
                    }
                }

                EditorGUI.indentLevel--;
            });
        }

        private bool MatchesFilter(ProviderProfiledProvider profiledProvider)
        {
            ProviderDiagnosticsSnapshot provider = profiledProvider.Diagnostics;
            if (_updateOnly && !provider.OverridesUpdate)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return true;
            }

            string search = _searchText.Trim();
            return Contains(provider.ProviderType.FullName, search) ||
                   Contains(provider.LifeCycle.ToString(), search) ||
                   Contains(provider.Stage.ToString(), search) ||
                   provider.ImplementedInterfaces.Any(type => Contains(type.FullName, search)) ||
                   profiledProvider.ServiceRegistrations.Any(registration =>
                       Contains(registration.ServiceType.FullName, search) ||
                       Contains(registration.ScopeName, search) ||
                       Contains(registration.Identifier?.ToString(), search));
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatInstance(object instance)
        {
            return instance == null
                ? "null"
                : $"0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(instance):X8}";
        }

        private static string FormatOverride(bool overridesMethod)
        {
            return overridesMethod ? "已覆写" : "使用基类空实现";
        }

        private static void DrawBadge(string label, bool active)
        {
            Color previous = GUI.color;
            GUI.color = active ? new Color(0.50f, 0.95f, 0.62f) : new Color(0.72f, 0.75f, 0.79f);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUI.color = previous;
        }

        private void RefreshSnapshotIfNeeded()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (_refreshGate.TryBeginRefresh(currentTime))
            {
                RefreshSnapshot();
            }
        }

        private void RefreshSnapshotImmediately(double currentTime)
        {
            _refreshGate.BeginImmediateRefresh(currentTime);
            RefreshSnapshot();
        }

        private void RefreshSnapshot()
        {
            try
            {
                SetSnapshot(ProviderProfiledDataSource.Capture());
                _refreshError = null;
            }
            catch (Exception exception)
            {
                _refreshError = exception;
            }
        }

        private void SetSnapshot(ProviderProfiledSnapshot snapshot)
        {
            _snapshot = snapshot;
            var activeProviders = new HashSet<SystemProviderBase>(ReferenceComparer<SystemProviderBase>.Instance);
            foreach (ProviderProfiledProvider provider in snapshot.Providers)
            {
                activeProviders.Add(provider.Diagnostics.Instance);
            }

            _collapsedProviders.RemoveWhere(provider => !activeProviders.Contains(provider));
        }

        private void SetCollapsed(SystemProviderBase provider, bool collapsed)
        {
            if (collapsed)
            {
                _collapsedProviders.Add(provider);
            }
            else
            {
                _collapsedProviders.Remove(provider);
            }
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
