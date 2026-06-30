using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Luban.Editor
{
    /// <summary>
    /// Luban 导表快捷入口。
    /// </summary>
    internal static class LubanQuickAccess
    {
        internal const string SceneToolbarElementId = "LFramework/Luban/QuickExportButton";

        private static void ExecuteExport()
        {
            if(!CanExecuteQuickExport())
            {
                EditorUtility.DisplayDialog("Luban", "Unity 正在编译或刷新资源，请稍后再试。", "确定");
                return;
            }

            LubanExportConfig config = LubanExportConfig.GetOrCreate();
            if(config == null)
            {
                EditorUtility.DisplayDialog("Luban", "未找到 LubanExportConfig。", "确定");
                return;
            }

            config.RunCommand();
        }

        internal static bool CanExecuteQuickExport()
        {
            return CanExecuteQuickExport(EditorApplication.isCompiling, EditorApplication.isUpdating);
        }

        internal static bool CanExecuteQuickExport(bool isCompiling, bool isUpdating)
        {
            return !isCompiling && !isUpdating;
        }

        internal static void ExecuteFromToolbar()
        {
            ExecuteExport();
        }
    }

    [EditorToolbarElement(LubanQuickAccess.SceneToolbarElementId, typeof(SceneView))]
    internal sealed class LubanQuickExportToolbarButton : EditorToolbarButton
    {
        public LubanQuickExportToolbarButton()
        {
            text = "Luban";
            tooltip = "执行 Luban 导表";
            icon = EditorGUIUtility.IconContent("d_BuildSettings.Editor.Small").image as Texture2D;
            clicked += LubanQuickAccess.ExecuteFromToolbar;
            RegisterCallback<AttachToPanelEvent>(_ => EditorApplication.update += RefreshEnabledState);
            RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= RefreshEnabledState);
            RefreshEnabledState();
        }

        private void RefreshEnabledState()
        {
            SetEnabled(LubanQuickAccess.CanExecuteQuickExport());
        }
    }

    [Overlay(typeof(SceneView), "Luban Quick Export", true, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top)]
    internal sealed class LubanQuickExportOverlay : ToolbarOverlay
    {
        public LubanQuickExportOverlay()
            : base(LubanQuickAccess.SceneToolbarElementId)
        {
        }
    }
}
