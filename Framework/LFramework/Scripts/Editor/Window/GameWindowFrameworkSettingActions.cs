using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    /// <summary>
    /// GameWindow 框架设置页交互动作。
    /// </summary>
    public static class GameWindowFrameworkSettingActions
    {
        public static bool HandleAssetBadgeClick(Object asset, int badgeIndex)
        {
            if (badgeIndex != 0 || asset == null)
            {
                return false;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            return true;
        }
    }
}
