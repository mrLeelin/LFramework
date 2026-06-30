using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Windows
{
    /// <summary>
    /// InjectionDebugWindow - 双向导航功能
    /// 服务 ↔ 注入点的双向跳转
    /// </summary>
    public partial class InjectionDebugWindow
    {
        #region Navigation Helpers

        /// <summary>
        /// 从服务跳转到使用该服务的注入点
        /// </summary>
        private void JumpToInjectPointsUsingService(Type serviceType)
        {
            // 切换到注入点标签页
            _selectedTab = 2;

            // 设置搜索文本为服务类型名
            _searchText = serviceType.Name;

            // 清除高亮（因为跳转到另一个标签页）
            _highlightServiceType = null;

            // 刷新显示
            Repaint();

            Debug.Log($"[InjectionDebugWindow] 跳转到使用 {serviceType.Name} 的注入点");
        }

        /// <summary>
        /// 获取使用指定服务的注入点数量
        /// </summary>
        private int GetServiceUsageCount(Type serviceType)
        {
            return _injectPointCache.Count(p => p.ServiceType == serviceType);
        }

        /// <summary>
        /// 获取使用指定服务的类列表
        /// </summary>
        private List<Type> GetTypesUsingService(Type serviceType)
        {
            return _injectPointCache
                .Where(p => p.ServiceType == serviceType)
                .Select(p => p.DeclaringType)
                .Distinct()
                .ToList();
        }

        #endregion

        #region Enhanced Service Drawing

        /// <summary>
        /// 在服务项中添加使用情况按钮
        /// </summary>
        private void DrawServiceUsageButton(ServiceInfo info)
        {
            var usageCount = GetServiceUsageCount(info.ServiceType);

            if (usageCount > 0)
            {
                var buttonText = $"被 {usageCount} 处使用";
                var buttonColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);

                if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(90)))
                {
                    JumpToInjectPointsUsingService(info.ServiceType);
                }

                GUI.backgroundColor = buttonColor;
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("未被使用", EditorStyles.miniButton, GUILayout.Width(90));
                GUI.enabled = true;
            }
        }

        #endregion
    }
}
