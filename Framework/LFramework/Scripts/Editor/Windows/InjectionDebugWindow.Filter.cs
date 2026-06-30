using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 高级筛选功能
    /// 多维度过滤数据
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region Filter Data

        private class FilterSettings
        {
            // 服务筛选
            public bool ShowOnlyOwned = false;
            public bool ShowOnlyInterfaces = false;
            public HashSet<string> SelectedAssemblies = new HashSet<string>();
            public HashSet<string> SelectedNamespaces = new HashSet<string>();

            // 注入点筛选
            public bool ShowOnlyFields = false;
            public bool ShowOnlyProperties = false;
            public bool ShowOnlyUnregistered = false;

            public bool IsActive()
            {
                return ShowOnlyOwned || ShowOnlyInterfaces ||
                       SelectedAssemblies.Count > 0 || SelectedNamespaces.Count > 0 ||
                       ShowOnlyFields || ShowOnlyProperties || ShowOnlyUnregistered;
            }

            public void Reset()
            {
                ShowOnlyOwned = false;
                ShowOnlyInterfaces = false;
                ShowOnlyFields = false;
                ShowOnlyProperties = false;
                ShowOnlyUnregistered = false;
                SelectedAssemblies.Clear();
                SelectedNamespaces.Clear();
            }
        }

        private FilterSettings _filterSettings = new FilterSettings();
        private bool _showFilterPanel = false;

        #endregion

        #region Filter UI

        private void DrawFilterPanel()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var filterActive = _filterSettings.IsActive();
            var filterLabel = filterActive ? "筛选 ✓" : "筛选";
            var filterColor = GUI.backgroundColor;

            if (filterActive)
            {
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            }

            _showFilterPanel = GUILayout.Toggle(_showFilterPanel, filterLabel, EditorStyles.toolbarButton, GUILayout.Width(60));

            GUI.backgroundColor = filterColor;

            if (filterActive && GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _filterSettings.Reset();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            if (_showFilterPanel)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // 根据当前标签页显示不同的筛选选项
                switch (_selectedTab)
                {
                    case 0: // 注册的服务
                        DrawServiceFilters();
                        break;
                    case 2: // 注入点信息
                        DrawInjectPointFilters();
                        break;
                    default:
                        EditorGUILayout.LabelField("当前标签页不支持筛选", EditorStyles.miniLabel);
                        break;
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawServiceFilters()
        {
            EditorGUILayout.LabelField("服务筛选", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // 类型筛选
            EditorGUILayout.BeginHorizontal();
            var newShowOnlyOwned = EditorGUILayout.ToggleLeft("只显示 Owned 服务", _filterSettings.ShowOnlyOwned);
            if (newShowOnlyOwned != _filterSettings.ShowOnlyOwned)
            {
                _filterSettings.ShowOnlyOwned = newShowOnlyOwned;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var newShowOnlyInterfaces = EditorGUILayout.ToggleLeft("只显示接口类型", _filterSettings.ShowOnlyInterfaces);
            if (newShowOnlyInterfaces != _filterSettings.ShowOnlyInterfaces)
            {
                _filterSettings.ShowOnlyInterfaces = newShowOnlyInterfaces;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            // 程序集筛选
            DrawAssemblyFilter();

            EditorGUI.indentLevel--;
        }

        private void DrawInjectPointFilters()
        {
            EditorGUILayout.LabelField("注入点筛选", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // 成员类型筛选
            EditorGUILayout.BeginHorizontal();
            var newShowOnlyFields = EditorGUILayout.ToggleLeft("只显示字段", _filterSettings.ShowOnlyFields);
            if (newShowOnlyFields != _filterSettings.ShowOnlyFields)
            {
                _filterSettings.ShowOnlyFields = newShowOnlyFields;
                if (newShowOnlyFields) _filterSettings.ShowOnlyProperties = false;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var newShowOnlyProperties = EditorGUILayout.ToggleLeft("只显示属性", _filterSettings.ShowOnlyProperties);
            if (newShowOnlyProperties != _filterSettings.ShowOnlyProperties)
            {
                _filterSettings.ShowOnlyProperties = newShowOnlyProperties;
                if (newShowOnlyProperties) _filterSettings.ShowOnlyFields = false;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            // 注册状态筛选
            EditorGUILayout.BeginHorizontal();
            var newShowOnlyUnregistered = EditorGUILayout.ToggleLeft("只显示未注册的服务", _filterSettings.ShowOnlyUnregistered);
            if (newShowOnlyUnregistered != _filterSettings.ShowOnlyUnregistered)
            {
                _filterSettings.ShowOnlyUnregistered = newShowOnlyUnregistered;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            // 程序集筛选
            DrawAssemblyFilter();

            EditorGUI.indentLevel--;
        }

        private void DrawAssemblyFilter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("程序集:", EditorStyles.miniLabel);

            // 获取所有程序集
            HashSet<string> allAssemblies = new HashSet<string>();

            if (_selectedTab == 0) // 服务
            {
                foreach (var service in _serviceCache.Values)
                {
                    allAssemblies.Add(service.ServiceType.Assembly.GetName().Name);
                }
            }
            else if (_selectedTab == 2) // 注入点
            {
                foreach (var point in _injectPointCache)
                {
                    allAssemblies.Add(point.DeclaringType.Assembly.GetName().Name);
                }
            }

            if (allAssemblies.Count == 0)
            {
                EditorGUILayout.LabelField("  无数据", EditorStyles.miniLabel);
                return;
            }

            foreach (var assembly in allAssemblies.OrderBy(a => a))
            {
                EditorGUILayout.BeginHorizontal();
                var isSelected = _filterSettings.SelectedAssemblies.Contains(assembly);
                var newSelected = EditorGUILayout.ToggleLeft("  " + assembly, isSelected);

                if (newSelected != isSelected)
                {
                    if (newSelected)
                        _filterSettings.SelectedAssemblies.Add(assembly);
                    else
                        _filterSettings.SelectedAssemblies.Remove(assembly);
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Filter Logic

        private bool PassesServiceFilter(ServiceInfo service)
        {
            // Owned 筛选
            if (_filterSettings.ShowOnlyOwned && !service.IsOwned)
                return false;

            // 接口筛选
            if (_filterSettings.ShowOnlyInterfaces && !service.ServiceType.IsInterface)
                return false;

            // 程序集筛选
            if (_filterSettings.SelectedAssemblies.Count > 0)
            {
                var assemblyName = service.ServiceType.Assembly.GetName().Name;
                if (!_filterSettings.SelectedAssemblies.Contains(assemblyName))
                    return false;
            }

            return true;
        }

        private bool PassesInjectPointFilter(InjectPointInfo point)
        {
            // 成员类型筛选
            if (_filterSettings.ShowOnlyFields && point.MemberType != "Field")
                return false;

            if (_filterSettings.ShowOnlyProperties && point.MemberType != "Property")
                return false;

            // 注册状态筛选
            if (_filterSettings.ShowOnlyUnregistered)
            {
                if (IsServiceRegistered(point.ServiceType))
                    return false;
            }

            // 程序集筛选
            if (_filterSettings.SelectedAssemblies.Count > 0)
            {
                var assemblyName = point.DeclaringType.Assembly.GetName().Name;
                if (!_filterSettings.SelectedAssemblies.Contains(assemblyName))
                    return false;
            }

            return true;
        }

        #endregion
    }
}
