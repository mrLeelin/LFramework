using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    internal sealed class WorldProfiled : ProfiledBase
    {
        private const double RefreshInterval = 0.5d;

        private readonly WorldProfiledRefreshGate _refreshGate = new WorldProfiledRefreshGate(RefreshInterval);
        private readonly HashSet<WorldBase> _collapsedWorlds =
            new HashSet<WorldBase>(ReferenceComparer<WorldBase>.Instance);
        private readonly HashSet<IWorldHelper> _collapsedHelpers =
            new HashSet<IWorldHelper>(ReferenceComparer<IWorldHelper>.Instance);
        private string _searchText = string.Empty;
        private bool _frameCallbacksOnly;
        private bool _showAllInterfaces = true;
        private Exception _refreshError;
        private WorldProfiledSnapshot _snapshot;

        internal override string Title => "World";
        internal override string SubTitle => "查看当前 World、服务注册、WorldHelper 接口和逐帧回调状态。";
        internal override bool CanDraw => true;

        internal override void Draw()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入 Play Mode 后可查看当前 World、WorldHelper 和逐帧回调状态。",
                    MessageType.Info);
                return;
            }

            RefreshSnapshotIfNeeded();
            if (_refreshError != null)
            {
                EditorGUILayout.HelpBox(
                    $"读取 World 运行时数据失败：{_refreshError.GetType().Name}: {_refreshError.Message}",
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

            WorldProfiledSnapshot snapshot = _snapshot;
            int helperCount = snapshot.Worlds.Sum(world => world.Helpers.Count);
            int updateCount = snapshot.Worlds.Sum(
                world => world.Helpers.Count(helper => helper.Diagnostics.ReceivesUpdate));
            int lateUpdateCount = snapshot.Worlds.Sum(
                world => world.Helpers.Count(helper => helper.Diagnostics.ReceivesLateUpdate));

            DrawMetricCards(
                new ProfiledMetric("当前 World", snapshot.Worlds.Count.ToString(), $"扫描 {snapshot.ScopeCount} 个服务作用域"),
                new ProfiledMetric("WorldHelper", helperCount.ToString(), "按实例去重后的注册数量"),
                new ProfiledMetric("Update", updateCount.ToString(), "进入 IWorldUpdate 回调列表"),
                new ProfiledMetric("LateUpdate", lateUpdateCount.ToString(), "进入 IWorldLateUpdate 回调列表"));

            DrawToolbar();

            if (snapshot.Worlds.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前没有在 LServices 中注册的 World。进入包含 HotfixProcedure 的流程后，这里会显示活动 World 和它的 WorldHelper。",
                    MessageType.Info);
                return;
            }

            foreach (WorldProfiledWorld world in snapshot.Worlds)
            {
                DrawWorld(world);
                GUILayout.Space(8f);
            }
        }

        private void DrawToolbar()
        {
            DrawSection("筛选", "按 Helper 类型、命名空间、业务接口或生命周期接口筛选。", () =>
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
                _frameCallbacksOnly = EditorGUILayout.ToggleLeft("仅显示 Update / LateUpdate Helper", _frameCallbacksOnly);
                _showAllInterfaces = EditorGUILayout.ToggleLeft("显示全部接口", _showAllInterfaces);
                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawWorld(WorldProfiledWorld world)
        {
            WorldDiagnosticsSnapshot diagnostics = world.Diagnostics;
            bool expanded = !_collapsedWorlds.Contains(diagnostics.Instance);

            DrawSection("当前 World", "活动 World 实例、关联流程和 LServices 注册别名。", () =>
            {
                bool nextExpanded = EditorGUILayout.Foldout(
                    expanded,
                    $"{diagnostics.WorldType.Name}  ({world.Helpers.Count} Helpers)",
                    true);
                SetCollapsed(_collapsedWorlds, diagnostics.Instance, !nextExpanded);
                if (!nextExpanded)
                {
                    return;
                }

                EditorGUI.indentLevel++;
                DrawKeyValueRow("完整类型", diagnostics.WorldType.FullName);
                DrawKeyValueRow("程序集", diagnostics.WorldType.Assembly.GetName().Name);
                DrawKeyValueRow("实例", FormatInstance(diagnostics.Instance));
                DrawKeyValueRow("关联 Procedure", diagnostics.LinkedProcedureType?.FullName ?? "未关联");
                DrawKeyValueRow(
                    "World 接口",
                    JoinTypes(diagnostics.WorldType.GetInterfaces().Where(type => type != typeof(IWorld))));

                GUILayout.Space(4f);
                EditorGUILayout.LabelField("服务注册", EditorStyles.boldLabel);
                foreach (WorldServiceRegistration registration in world.ServiceRegistrations)
                {
                    string identifier = registration.Identifier == null ? "<default>" : registration.Identifier.ToString();
                    string ownership = registration.IsOwned ? "Owned" : "External";
                    EditorGUILayout.LabelField(
                        $"{registration.ServiceType.FullName}  ·  Id {identifier}  ·  {registration.ScopeName}  ·  {ownership}",
                        EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUI.indentLevel--;
            });

            WorldProfiledHelper[] helpers = world.Helpers
                .Where(MatchesFilter)
                .ToArray();

            DrawSection(
                "World Helpers",
                $"显示 {helpers.Length}/{world.Helpers.Count} 个 Helper。Interface 是 World 内部业务绑定；DI 表示当前 LServices 中存在该实例的注册。",
                () =>
                {
                    if (helpers.Length == 0)
                    {
                        EditorGUILayout.HelpBox("没有符合当前筛选条件的 WorldHelper。", MessageType.Info);
                        return;
                    }

                    foreach (WorldProfiledHelper helper in helpers)
                    {
                        DrawHelper(helper);
                        GUILayout.Space(4f);
                    }
                });
        }

        private void DrawHelper(WorldProfiledHelper profiledHelper)
        {
            WorldHelperDiagnostics helper = profiledHelper.Diagnostics;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool expanded = !_collapsedHelpers.Contains(helper.Instance);
                EditorGUILayout.BeginHorizontal();
                bool nextExpanded = EditorGUILayout.Foldout(expanded, helper.HelperType.Name, true);
                SetCollapsed(_collapsedHelpers, helper.Instance, !nextExpanded);
                GUILayout.FlexibleSpace();
                DrawStatusBadge("Update", helper.ReceivesUpdate);
                DrawStatusBadge("LateUpdate", helper.ReceivesLateUpdate);
                DrawStatusBadge("Auto", helper.IsAutoRegistered);
                DrawStatusBadge("DI", profiledHelper.IsRegisteredAsService);
                EditorGUILayout.EndHorizontal();

                if (!nextExpanded)
                {
                    return;
                }

                EditorGUI.indentLevel++;
                DrawKeyValueRow("完整类型", helper.HelperType.FullName);
                DrawKeyValueRow("程序集", helper.HelperType.Assembly.GetName().Name);
                DrawKeyValueRow("实例", FormatInstance(helper.Instance));
                DrawKeyValueRow("注册来源", helper.IsAutoRegistered ? "[AutoWorldHelper] 自动注册" : "World.BuildWorldHelpers 手动注册");
                DrawKeyValueRow("业务 Interface", helper.ServiceInterface?.FullName ?? "无唯一业务接口");
                DrawKeyValueRow("World 注册键", JoinTypes(helper.RegistrationTypes));
                DrawKeyValueRow("Update 回调", helper.ReceivesUpdate ? "已加入 World.Update" : "未加入");
                DrawKeyValueRow("LateUpdate 回调", helper.ReceivesLateUpdate ? "已加入 World.LateUpdate" : "未加入");
                DrawKeyValueRow("生命周期 API", "Initialize · StartGame · InstantiateWorld · StopGame · Clear");

                GUILayout.Space(3f);
                EditorGUILayout.LabelField("当前 LServices 绑定", EditorStyles.boldLabel);
                if (profiledHelper.ServiceRegistrations.Count == 0)
                {
                    EditorGUILayout.LabelField("未绑定", EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    foreach (WorldServiceRegistration registration in profiledHelper.ServiceRegistrations)
                    {
                        string identifier = registration.Identifier == null
                            ? "<default>"
                            : registration.Identifier.ToString();
                        string ownership = registration.IsOwned ? "Owned" : "External";
                        EditorGUILayout.LabelField(
                            $"{registration.ServiceType.FullName}  ·  Id {identifier}  ·  {registration.ScopeName}  ·  {ownership}",
                            EditorStyles.wordWrappedMiniLabel);
                    }
                }

                if (_showAllInterfaces)
                {
                    GUILayout.Space(3f);
                    EditorGUILayout.LabelField("实现的 Interfaces", EditorStyles.boldLabel);
                    foreach (Type interfaceType in helper.ImplementedInterfaces)
                    {
                        string category = GetInterfaceCategory(interfaceType);
                        EditorGUILayout.LabelField($"[{category}] {interfaceType.FullName}", EditorStyles.wordWrappedMiniLabel);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private bool MatchesFilter(WorldProfiledHelper profiledHelper)
        {
            WorldHelperDiagnostics helper = profiledHelper.Diagnostics;
            if (_frameCallbacksOnly && !helper.ReceivesUpdate && !helper.ReceivesLateUpdate)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return true;
            }

            string search = _searchText.Trim();
            return Contains(helper.HelperType.FullName, search) ||
                   Contains(helper.ServiceInterface?.FullName, search) ||
                   helper.RegistrationTypes.Any(type => Contains(type.FullName, search)) ||
                   helper.ImplementedInterfaces.Any(type => Contains(type.FullName, search)) ||
                   profiledHelper.ServiceRegistrations.Any(
                       registration => Contains(registration.ServiceType.FullName, search));
        }

        private static bool Contains(string value, string search)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawStatusBadge(string label, bool active)
        {
            Color previous = GUI.color;
            GUI.color = active ? new Color(0.50f, 0.95f, 0.62f) : new Color(0.62f, 0.65f, 0.69f);
            GUILayout.Label(active ? label : $"No {label}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            GUI.color = previous;
        }

        private static string GetInterfaceCategory(Type interfaceType)
        {
            if (interfaceType == typeof(IWorldUpdate))
            {
                return "Update";
            }

            if (interfaceType == typeof(IWorldLateUpdate))
            {
                return "LateUpdate";
            }

            if (interfaceType == typeof(IWorldHelper))
            {
                return "Lifecycle";
            }

            return typeof(IWorldHelper).IsAssignableFrom(interfaceType) ? "Service" : "Interface";
        }

        private static string JoinTypes(IEnumerable<Type> types)
        {
            string[] names = types?
                .Where(type => type != null)
                .Select(type => type.FullName)
                .Distinct()
                .ToArray();
            return names == null || names.Length == 0 ? "无" : string.Join("\n", names);
        }

        private static string FormatInstance(object instance)
        {
            return instance == null
                ? "null"
                : $"0x{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(instance):X8}";
        }

        private void RefreshSnapshotIfNeeded()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (!_refreshGate.TryBeginRefresh(currentTime))
            {
                return;
            }

            RefreshSnapshot();
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
                SetSnapshot(WorldProfiledDataSource.Capture());
                _refreshError = null;
            }
            catch (Exception exception)
            {
                _refreshError = exception;
            }
        }

        private void SetSnapshot(WorldProfiledSnapshot snapshot)
        {
            _snapshot = snapshot;

            var activeWorlds = new HashSet<WorldBase>(ReferenceComparer<WorldBase>.Instance);
            var activeHelpers = new HashSet<IWorldHelper>(ReferenceComparer<IWorldHelper>.Instance);
            foreach (WorldProfiledWorld world in snapshot.Worlds)
            {
                activeWorlds.Add(world.Diagnostics.Instance);
                foreach (WorldProfiledHelper helper in world.Helpers)
                {
                    activeHelpers.Add(helper.Diagnostics.Instance);
                }
            }

            _collapsedWorlds.RemoveWhere(world => !activeWorlds.Contains(world));
            _collapsedHelpers.RemoveWhere(helper => !activeHelpers.Contains(helper));
        }

        private static void SetCollapsed<T>(ISet<T> collapsedItems, T item, bool collapsed) where T : class
        {
            if (collapsed)
            {
                collapsedItems.Add(item);
            }
            else
            {
                collapsedItems.Remove(item);
            }
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

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
