using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// Injection 系统调试窗口
    /// 显示当前注册的服务、作用域层级、注入器信息
    /// </summary>
    public partial class InjectionDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "注册的服务", "注入器信息", "注入点信息", "作用域层级", "统计分析", "问题诊断", "性能监控" };
        private readonly string[] _embeddedTabNames = { "服务", "注入器", "注入点", "作用域", "统计", "诊断", "性能" };

        private bool _initialized;
        private bool _embeddedHost;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.5; // 秒


        // 搜索框
        private string _searchText = "";
        // 高亮显示
        private Type _highlightServiceType = null;
        private double _highlightStartTime = 0;
        private const double HighlightDuration = 2.0; // 高亮持续时间（秒）

        // 通过反射获取的数据
        private Dictionary<string, ServiceInfo> _serviceCache = new Dictionary<string, ServiceInfo>();
        private List<InjectorInfo> _injectorCache = new List<InjectorInfo>();
        private List<InjectPointInfo> _injectPointCache = new List<InjectPointInfo>();
        private ScopeHierarchy _scopeHierarchy;

        // 验证结果
        private List<ValidationIssue> _validationIssues = new List<ValidationIssue>();

        [MenuItem("LFramework/Injection Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<InjectionDebugWindow>("Injection 调试");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        internal static void CloseStandaloneWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<InjectionDebugWindow>();
            foreach (var window in windows)
            {
                if (window == null || window._embeddedHost)
                {
                    continue;
                }

                window.Close();
            }
        }

        internal void MarkEmbeddedHost()
        {
            _embeddedHost = true;
        }

        internal int ServiceCount => _serviceCache.Count;

        internal int InjectorCount => _injectorCache.Count;

        internal int InjectPointCount => _injectPointCache.Count;

        internal int ScopeCount => _scopeHierarchy?.TotalScopes ?? 0;

        internal int ValidationIssueCount => _validationIssues.Count;

        internal int ValidationErrorCount => _validationIssues.Count(issue => issue.Severity == IssueSeverity.Error);

        private void OnEnable()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Draws the full injection debug UI inside another editor surface.
        /// This does not open or focus an extra EditorWindow.
        /// </summary>
        internal void DrawEmbedded()
        {
            MarkEmbeddedHost();
            UpdateEmbeddedSnapshot();
            DrawDebugPanel(false);
        }

        internal void UpdateEmbeddedSnapshot()
        {
            EnsureInitialized();
            TickAutoRefresh(false);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            // Keep creation cheap. Full injection-point scanning is explicit via the Rescan button.
            EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                RefreshData();
                ValidateConfiguration();
            };
        }

        private void Update()
        {
            // 检查窗口是否有焦点，避免后台刷新导致抢焦点
            if (!focusedWindow || focusedWindow != this)
            {
                // 窗口没有焦点时不刷新，避免抢焦点
                return;
            }

            TickAutoRefresh(true);

            // 性能监控更新
            UpdatePerformanceMonitoring();
        }

        private void OnGUI()
        {
            EnsureInitialized();
            DrawDebugPanel(true);
        }

        private void DrawDebugPanel(bool useInternalScroll)
        {
            if (_embeddedHost)
            {
                DrawEmbeddedPanel();
                return;
            }

            DrawToolbar();
            DrawSearchBar();
            DrawFilterPanel();

            if (useInternalScroll)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            }

            DrawSelectedTab();

            if (useInternalScroll)
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawEmbeddedPanel()
        {
            DrawEmbeddedTabStrip();
            DrawEmbeddedActionBar();
            DrawEmbeddedSearchAndFilter();
            GUILayout.Space(8f);
            DrawSelectedTab();
        }

        private void DrawEmbeddedTabStrip()
        {
            int firstRowCount = EditorGUIUtility.currentViewWidth < 760f ? 4 : _embeddedTabNames.Length;
            DrawEmbeddedTabRow(0, firstRowCount);

            if (firstRowCount < _embeddedTabNames.Length)
            {
                DrawEmbeddedTabRow(firstRowCount, _embeddedTabNames.Length);
            }

            GUILayout.Space(6f);
        }

        private void DrawEmbeddedTabRow(int startIndex, int endIndex)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = startIndex; i < endIndex; i++)
            {
                DrawEmbeddedTabButton(i);
                GUILayout.Space(4f);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmbeddedTabButton(int index)
        {
            var previousColor = GUI.backgroundColor;
            if (index == _selectedTab)
            {
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.20f, 0.55f, 0.82f)
                    : new Color(0.58f, 0.80f, 0.96f);
            }

            if (GUILayout.Button(_embeddedTabNames[index], EditorStyles.miniButton, GUILayout.Width(76f), GUILayout.Height(24f)))
            {
                if (_selectedTab != index)
                {
                    _selectedTab = index;
                    OnTabChanged(_selectedTab);
                }
            }

            GUI.backgroundColor = previousColor;
        }

        private void DrawEmbeddedActionBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawHistoryButtons();
            GUILayout.FlexibleSpace();

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(48f));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                RefreshData();
                ValidateConfiguration();
            }

            if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(58f)))
            {
                RefreshInjectPoints();
                ValidateConfiguration();
                Repaint();
            }

            DrawExportButton();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmbeddedSearchAndFilter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Search", EditorStyles.miniLabel, GUILayout.Width(42f));
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarTextField);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(44f)))
            {
                _searchText = string.Empty;
                GUI.FocusControl(null);
            }

            var filterActive = _filterSettings.IsActive();
            var previousColor = GUI.backgroundColor;
            if (filterActive)
            {
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.42f, 0.56f)
                    : new Color(0.62f, 0.82f, 0.96f);
            }

            _showFilterPanel = GUILayout.Toggle(_showFilterPanel, filterActive ? "Filter *" : "Filter", EditorStyles.toolbarButton, GUILayout.Width(58f));
            GUI.backgroundColor = previousColor;

            if (filterActive && GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(48f)))
            {
                _filterSettings.Reset();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            if (!_showFilterPanel)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (_selectedTab)
            {
                case 0:
                    DrawServiceFilters();
                    break;
                case 2:
                    DrawInjectPointFilters();
                    break;
                default:
                    EditorGUILayout.LabelField("当前标签页不支持筛选", EditorStyles.miniLabel);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedTab()
        {
            switch (_selectedTab)
            {
                case 0:
                    DrawServicesTab();
                    break;
                case 1:
                    DrawInjectorsTab();
                    break;
                case 2:
                    DrawInjectPointsTab();
                    break;
                case 3:
                    DrawScopesTab();
                    break;
                case 4:
                    DrawStatisticsTab();
                    break;
                case 5:
                    DrawValidationTab();
                    break;
                case 6:
                    DrawPerformanceTab();
                    break;
            }
        }

        private void TickAutoRefresh(bool repaint)
        {
            if (!_autoRefresh || EditorApplication.timeSinceStartup - _lastRefreshTime <= RefreshInterval)
            {
                return;
            }

            RefreshData();
            if (repaint)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            DrawStandaloneToolbar();
        }

        private void DrawStandaloneToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 保存旧的标签索引
            var oldTab = _selectedTab;

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, EditorStyles.toolbarButton);

            // 检测标签切换
            if (oldTab != _selectedTab)
            {
                OnTabChanged(_selectedTab);
            }

            GUILayout.FlexibleSpace();

            // 历史导航按钮
            DrawHistoryButtons();

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                RefreshData();
                ValidateConfiguration();
            }

            if (GUILayout.Button("重新扫描", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                RefreshInjectPoints();
                ValidateConfiguration();
                Repaint();
            }

            // 导出按钮
            DrawExportButton();

            EditorGUILayout.EndHorizontal();

            // 统计信息栏
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"服务: {_serviceCache.Count}", EditorStyles.miniLabel);
            GUILayout.Label($"注入器: {_injectorCache.Count}", EditorStyles.miniLabel);
            GUILayout.Label($"注入点: {_injectPointCache.Count}", EditorStyles.miniLabel);
            GUILayout.Label($"作用域: {(_scopeHierarchy?.TotalScopes ?? 0)}", EditorStyles.miniLabel);

            // 问题提示
            if (_validationIssues.Count > 0)
            {
                var errorCount = _validationIssues.Count(i => i.Severity == IssueSeverity.Error);
                var warningCount = _validationIssues.Count(i => i.Severity == IssueSeverity.Warning);

                if (errorCount > 0)
                {
                    var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.red } };
                    GUILayout.Label($"⚠️ 错误: {errorCount}", style);
                }
                if (warningCount > 0)
                {
                    var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.yellow } };
                    GUILayout.Label($"⚠ 警告: {warningCount}", style);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("搜索:", GUILayout.Width(40));

            GUI.SetNextControlName("SearchField");
            var newSearchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarTextField);

            if (newSearchText != _searchText)
            {
                _searchText = newSearchText;
            }

            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private bool MatchesSearch(string text)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            return text.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsServiceRegistered(Type serviceType)
        {
            if (serviceType == null) return false;

            return _serviceCache.Values.Any(s => s.ServiceType == serviceType);
        }

        private void JumpToService(Type serviceType)
        {
            // 切换到"注册的服务"标签页
            _selectedTab = 0;

            // 设置搜索文本为服务类型名
            _searchText = serviceType.Name;

            // 设置高亮
            _highlightServiceType = serviceType;
            _highlightStartTime = EditorApplication.timeSinceStartup;

            // 刷新显示
            Repaint();

            Debug.Log($"[InjectionDebugWindow] 跳转到服务: {serviceType.Name}");
        }

        #region Services Tab

        private void DrawServicesTab()
        {
            if (_serviceCache.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有注册的服务。\n\n请确保：\n1. 游戏已经运行或初始化了 LServices\n2. 通过 LServices.Register() 注册了服务\n3. 点击「刷新」按钮重新获取数据", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            var filteredServices = _serviceCache.Values.Where(s =>
                MatchesSearch(s.ServiceType.Name) ||
                MatchesSearch(s.ServiceType.FullName) ||
                (s.Identifier != null && MatchesSearch(s.Identifier.ToString()))
            ).Where(PassesServiceFilter).OrderBy(x => x.ServiceType.Name).ToList();

            EditorGUILayout.LabelField($"已注册的服务 ({filteredServices.Count}/{_serviceCache.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            foreach (var service in filteredServices)
            {
                DrawServiceItem(service);
            }
        }

        private void DrawServiceItem(ServiceInfo info)
        {
            // 检查是否需要高亮
            bool shouldHighlight = _highlightServiceType == info.ServiceType &&
                                   (EditorApplication.timeSinceStartup - _highlightStartTime) < HighlightDuration;

            if (shouldHighlight)
            {
                // 高亮背景色
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f, 0.3f); // 浅蓝色高亮
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = originalColor;
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            }

            // 标题行
            EditorGUILayout.BeginHorizontal();

            // 类型图标
            GUILayout.Label(GetServiceIcon(info), GUILayout.Width(20), GUILayout.Height(20));

            // 服务类型
            EditorGUILayout.LabelField(info.ServiceType.Name, EditorStyles.boldLabel);

            // Owned 标记
            if (info.IsOwned)
            {
                GUILayout.Label("OWNED", GetOwnedStyle(), GUILayout.Width(60));
            }

            GUILayout.FlexibleSpace();

            // 使用情况按钮（双向跳转）
            DrawServiceUsageButton(info);

            EditorGUILayout.EndHorizontal();

            // 详细信息
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("完整类型", info.ServiceType.FullName, EditorStyles.wordWrappedMiniLabel);

            if (info.Identifier != null)
            {
                EditorGUILayout.LabelField("标识符", info.Identifier.ToString(), EditorStyles.wordWrappedMiniLabel);
            }

            if (info.Instance != null)
            {
                EditorGUILayout.LabelField("实例类型", info.Instance.GetType().FullName, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("实例 HashCode", info.Instance.GetHashCode().ToString(), EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("实例", "null", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.LabelField("作用域", info.ScopeName, EditorStyles.wordWrappedMiniLabel);

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Injectors Tab

        private void DrawInjectorsTab()
        {
            if (_injectorCache.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有注册的动态注入器。\n\n" +
                    "💡 什么是动态注入器？\n" +
                    "• 动态注入器用于热更新程序集（HybridCLR）\n" +
                    "• 通过 Injection.Register<T>() 注册\n" +
                    "• 主项目代码使用源码生成器（不需要动态注入器）\n\n" +
                    "📝 如何注册？\n" +
                    "Injection.Register<MyClass>((target, resolver) => {\n" +
                    "    target.Service = resolver.Get<IService>();\n" +
                    "});", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            var filteredInjectors = _injectorCache.Where(i =>
                MatchesSearch(i.TargetType.Name) ||
                MatchesSearch(i.TargetType.FullName) ||
                MatchesSearch(i.TargetType.Assembly.GetName().Name)
            ).OrderBy(x => x.TargetType.Name).ToList();

            EditorGUILayout.LabelField($"已注册的动态注入器 ({filteredInjectors.Count}/{_injectorCache.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            foreach (var injector in filteredInjectors)
            {
                DrawInjectorItem(injector);
            }
        }

        private void DrawInjectorItem(InjectorInfo info)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("cs Script Icon"), GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(info.TargetType.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("目标类型", info.TargetType.FullName, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("程序集", info.TargetType.Assembly.GetName().Name, EditorStyles.wordWrappedMiniLabel);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Inject Points Tab

        private void DrawInjectPointsTab()
        {
            if (_injectPointCache.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有找到使用 [Inject] 标记的字段或属性。\n\n" +
                    "💡 什么是注入点？\n" +
                    "• 使用 [Inject] 特性标记的字段或属性\n" +
                    "• 表示这些成员需要依赖注入\n\n" +
                    "📝 示例：\n" +
                    "public class MyClass {\n" +
                    "    [Inject] private IMyService _service;\n" +
                    "}\n\n" +
                    "💡 跳转功能：\n" +
                    "• 如果服务已注册，会显示 [→] 按钮\n" +
                    "• 点击可跳转到「注册的服务」标签页", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            var filteredPoints = _injectPointCache.Where(p =>
                MatchesSearch(p.DeclaringType.Name) ||
                MatchesSearch(p.DeclaringType.FullName) ||
                MatchesSearch(p.MemberName) ||
                MatchesSearch(p.ServiceType.Name)
            ).Where(PassesInjectPointFilter).ToList();

            var groupedByType = filteredPoints.GroupBy(x => x.DeclaringType).OrderBy(g => g.Key.Name);

            EditorGUILayout.LabelField($"发现的注入点 ({filteredPoints.Count}/{_injectPointCache.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            foreach (var group in groupedByType)
            {
                DrawInjectPointGroup(group.Key, group.ToList());
            }
        }

        private void DrawInjectPointGroup(Type declaringType, List<InjectPointInfo> points)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 类型头部
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("cs Script Icon"), GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(declaringType.Name, EditorStyles.boldLabel);
            GUILayout.Label($"({points.Count} 个注入点)", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("命名空间", declaringType.Namespace ?? "<global>", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("程序集", declaringType.Assembly.GetName().Name, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("注入点列表：", EditorStyles.miniLabel);

            foreach (var point in points)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);

                // 成员类型图标
                string icon = point.MemberType == "Field" ? "ScriptableObject Icon" : "Property Icon";
                GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(16), GUILayout.Height(16));

                // 成员信息
                EditorGUILayout.LabelField(
                    $"{point.MemberName} : {point.ServiceType.Name}",
                    EditorStyles.miniLabel
                );

                GUILayout.FlexibleSpace();

                // 检查服务是否已注册
                bool isServiceRegistered = IsServiceRegistered(point.ServiceType);

                if (isServiceRegistered)
                {
                    if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        JumpToService(point.ServiceType);
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(24));
                    GUI.enabled = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Scopes Tab

        private void DrawScopesTab()
        {
            if (_scopeHierarchy == null)
            {
                EditorGUILayout.HelpBox("无法获取作用域信息。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("作用域层级结构", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawScopeNode(_scopeHierarchy.Root, 0);
        }

        private void DrawScopeNode(ScopeNode node, int depth)
        {
            if (node == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(depth * 20);

            if (node.Children.Count > 0)
            {
                node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, "", true);
            }
            else
            {
                GUILayout.Space(12);
            }

            GUILayout.Label(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(20), GUILayout.Height(20));

            EditorGUILayout.LabelField(node.Name, EditorStyles.boldLabel);

            GUILayout.Label($"({node.ServiceCount} 服务)", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (node.IsDisposed)
            {
                GUILayout.Label("DISPOSED", GetDisposedStyle(), GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);

            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    DrawScopeNode(child, depth + 1);
                }
            }
        }

        #endregion

        #region Data Refresh

        private void RefreshData()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;

            try
            {
                RefreshServices();
                RefreshInjectors();
                // 注入点不需要自动刷新，只在首次打开或手动刷新时扫描
                RefreshScopes();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"刷新 Injection 数据时出错: {ex.Message}");
            }
        }

        private void RefreshServices()
        {
            _serviceCache.Clear();

            try
            {
                // 通过反射获取 LServices 的 root scope
                var rootField = typeof(LServices).GetField("_root", BindingFlags.NonPublic | BindingFlags.Static);
                if (rootField == null)
                {
                    Debug.LogWarning("[InjectionDebugWindow] 无法找到 LServices._root 字段");
                    return;
                }

                var rootScope = rootField.GetValue(null) as LServiceScope;
                if (rootScope == null)
                {
                    Debug.LogWarning("[InjectionDebugWindow] LServices._root 为 null");
                    return;
                }

                // 获取 scope 的私有字段 _services
                var servicesField = typeof(LServiceScope).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance);
                if (servicesField == null)
                {
                    Debug.LogWarning("[InjectionDebugWindow] 无法找到 LServiceScope._services 字段");
                    return;
                }

                var services = servicesField.GetValue(rootScope) as System.Collections.IDictionary;
                if (services == null)
                {
                    Debug.LogWarning("[InjectionDebugWindow] _services 字段为 null");
                    return;
                }

                Debug.Log($"[InjectionDebugWindow] 找到 {services.Count} 个已注册的服务");

                int index = 0;
                foreach (System.Collections.DictionaryEntry entry in services)
                {
                    var key = entry.Key;
                    var value = entry.Value;

                    // 解析 ServiceKey - 使用私有字段 _type 和 _identifier
                    var keyType = key.GetType();
                    var serviceTypeField = keyType.GetField("_type", BindingFlags.NonPublic | BindingFlags.Instance);
                    var identifierField = keyType.GetField("_identifier", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (serviceTypeField == null) continue;

                    var serviceType = serviceTypeField.GetValue(key) as Type;
                    var identifier = identifierField?.GetValue(key);

                    // 解析 ServiceEntry - 使用属性 Instance 和私有字段 _disposeWithScope
                    var entryType = value.GetType();
                    var instanceProperty = entryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Instance);
                    var ownedField = entryType.GetField("_disposeWithScope", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (instanceProperty == null) continue;

                    var instance = instanceProperty.GetValue(value);
                    var isOwned = ownedField != null && (bool)ownedField.GetValue(value);

                    var info = new ServiceInfo
                    {
                        ServiceType = serviceType,
                        Identifier = identifier,
                        Instance = instance,
                        IsOwned = isOwned,
                        ScopeName = "Root"
                    };

                    string cacheKey = $"{serviceType.FullName}_{identifier ?? "null"}_{index++}";
                    _serviceCache[cacheKey] = info;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"刷新服务列表时出错: {ex.Message}");
            }
        }

        private void RefreshInjectors()
        {
            _injectorCache.Clear();

            try
            {
                // 通过反射获取 Injection 的私有字典 GeneratedInjectors
                var injectorsField = typeof(Injection).GetField("GeneratedInjectors", BindingFlags.NonPublic | BindingFlags.Static);
                if (injectorsField == null) return;

                var injectors = injectorsField.GetValue(null) as System.Collections.IDictionary;
                if (injectors == null) return;

                foreach (System.Collections.DictionaryEntry entry in injectors)
                {
                    var targetType = entry.Key as Type;
                    if (targetType == null) continue;

                    _injectorCache.Add(new InjectorInfo
                    {
                        TargetType = targetType
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"刷新注入器列表时出错: {ex.Message}");
            }
        }

        private void RefreshInjectPoints()
        {
            _injectPointCache.Clear();

            var startTime = EditorApplication.timeSinceStartup;

            try
            {
                // 获取所有已加载的程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                // 只扫描项目相关的程序集
                var targetAssemblies = assemblies.Where(a =>
                {
                    var name = a.GetName().Name;
                    return !name.StartsWith("System") &&
                           !name.StartsWith("Unity") &&
                           !name.StartsWith("mscorlib") &&
                           !name.StartsWith("netstandard") &&
                           !name.StartsWith("Microsoft") &&
                           !name.StartsWith("Mono.") &&
                           !name.StartsWith("ExCSS") &&
                           !name.StartsWith("Newtonsoft") &&
                           !name.StartsWith("nunit") &&
                           !name.StartsWith("JetBrains") &&
                           !name.StartsWith("NSubstitute");
                }).ToList();

                Debug.Log($"[InjectionDebugWindow] 开始扫描 {targetAssemblies.Count} 个程序集...");

                int scannedTypes = 0;

                foreach (var assembly in targetAssemblies)
                {
                    var assemblyName = assembly.GetName().Name;

                    try
                    {
                        var types = assembly.GetTypes();

                        foreach (var type in types)
                        {
                            if (type.IsAbstract || type.IsInterface)
                                continue;

                            scannedTypes++;

                            // 查找带有 [Inject] 特性的字段（只查找实例字段）
                            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                            foreach (var field in fields)
                            {
                                if (Attribute.IsDefined(field, typeof(InjectAttribute)))
                                {
                                    _injectPointCache.Add(new InjectPointInfo
                                    {
                                        DeclaringType = type,
                                        MemberName = field.Name,
                                        MemberType = "Field",
                                        ServiceType = field.FieldType
                                    });
                                }
                            }

                            // 查找带有 [Inject] 特性的属性（只查找实例属性）
                            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                            foreach (var property in properties)
                            {
                                if (Attribute.IsDefined(property, typeof(InjectAttribute)))
                                {
                                    _injectPointCache.Add(new InjectPointInfo
                                    {
                                        DeclaringType = type,
                                        MemberName = property.Name,
                                        MemberType = "Property",
                                        ServiceType = property.PropertyType
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 某些程序集可能无法获取类型，跳过
                        Debug.LogWarning($"[InjectionDebugWindow] 跳过程序集 {assemblyName}: {ex.Message}");
                        continue;
                    }
                }

                var elapsed = EditorApplication.timeSinceStartup - startTime;
                Debug.Log($"[InjectionDebugWindow] 扫描完成：{scannedTypes} 个类型，找到 {_injectPointCache.Count} 个注入点，耗时 {elapsed:F2} 秒");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"刷新注入点列表时出错: {ex.Message}");
            }
        }

        private void RefreshScopes()
        {
            try
            {
                var rootField = typeof(LServices).GetField("_root", BindingFlags.NonPublic | BindingFlags.Static);
                if (rootField == null) return;

                var rootScope = rootField.GetValue(null) as LServiceScope;
                if (rootScope == null) return;

                _scopeHierarchy = new ScopeHierarchy
                {
                    Root = BuildScopeNode(rootScope, "Root Scope")
                };

                CountScopes(_scopeHierarchy.Root, ref _scopeHierarchy.TotalScopes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"刷新作用域层级时出错: {ex.Message}");
            }
        }

        private ScopeNode BuildScopeNode(LServiceScope scope, string name)
        {
            var node = new ScopeNode
            {
                Name = name,
                IsDisposed = scope.IsDisposed,
                IsExpanded = true
            };

            // 获取服务数量
            var servicesField = typeof(LServiceScope).GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance);
            if (servicesField != null)
            {
                var services = servicesField.GetValue(scope) as System.Collections.IDictionary;
                node.ServiceCount = services?.Count ?? 0;
            }

            // 获取子作用域
            var childrenField = typeof(LServiceScope).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance);
            if (childrenField != null)
            {
                var children = childrenField.GetValue(scope) as List<LServiceScope>;
                if (children != null)
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        node.Children.Add(BuildScopeNode(children[i], $"Child Scope {i + 1}"));
                    }
                }
            }

            return node;
        }

        private void CountScopes(ScopeNode node, ref int count)
        {
            if (node == null) return;
            count++;
            foreach (var child in node.Children)
            {
                CountScopes(child, ref count);
            }
        }

        #endregion

        #region Helper Methods & Styles

        private GUIContent GetServiceIcon(ServiceInfo info)
        {
            if (info.ServiceType.IsInterface)
            {
                return EditorGUIUtility.IconContent("Interface Icon");
            }
            if (typeof(MonoBehaviour).IsAssignableFrom(info.ServiceType))
            {
                return EditorGUIUtility.IconContent("cs Script Icon");
            }
            if (typeof(ScriptableObject).IsAssignableFrom(info.ServiceType))
            {
                return EditorGUIUtility.IconContent("ScriptableObject Icon");
            }
            return EditorGUIUtility.IconContent("GameObject Icon");
        }

        private GUIStyle GetOwnedStyle()
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };
            return style;
        }

        private GUIStyle GetDisposedStyle()
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.2f, 0.2f) }
            };
            return style;
        }

        #endregion

        #region Test Data

        #endregion

        #region Data Classes

        private class ServiceInfo
        {
            public Type ServiceType;
            public object Identifier;
            public object Instance;
            public bool IsOwned;
            public string ScopeName;
        }

        private class InjectorInfo
        {
            public Type TargetType;
        }

        private class InjectPointInfo
        {
            public Type DeclaringType;
            public string MemberName;
            public string MemberType; // "Field" or "Property"
            public Type ServiceType;
        }

        private class ScopeHierarchy
        {
            public ScopeNode Root;
            public int TotalScopes;
        }

        private class ScopeNode
        {
            public string Name;
            public int ServiceCount;
            public bool IsDisposed;
            public bool IsExpanded = true;
            public List<ScopeNode> Children = new List<ScopeNode>();
        }

        #endregion
    }
}
