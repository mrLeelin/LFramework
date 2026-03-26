using GameFramework;
using GameFramework.UI;
using LFramework.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class UIProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private UIComponent _uiComponent;

        internal override void Draw()
        {
            GetComponent(ref _uiComponent);
            if (_uiComponent == null)
            {
                EditorGUILayout.HelpBox("UIComponent is unavailable in the current runtime state.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Overview", "Current UI groups, active forms, and instantiated handles.");
            EditorGUILayout.LabelField("UI Group Count", _uiComponent.UIGroupCount.ToString());
            EditorGUILayout.ObjectField("Canvas Root", _uiComponent.CanvasRoot, typeof(Transform), true);

            IUIGroup[] groups = _uiComponent.GetAllUIGroups();
            if (groups == null || groups.Length == 0)
            {
                EditorGUILayout.HelpBox("No UI groups are currently registered.", MessageType.Info);
                return;
            }

            GUILayout.Space(6f);
            GameWindowChrome.DrawCompactHeader("UI Groups", "Each group shows its current form, depth, pause state, and spawned instances.");
            foreach (IUIGroup group in groups)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string currentFormName = group.CurrentUIForm != null
                    ? group.CurrentUIForm.UIFormAssetName
                    : "<None>";

                EditorGUILayout.LabelField(group.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "Summary",
                    Utility.Text.Format("Depth {0} | Pause {1} | Forms {2}", group.Depth, group.Pause, group.UIFormCount));
                EditorGUILayout.LabelField("Current UI Form", currentFormName);

                IUIForm[] uiForms = group.GetAllUIForms();
                if (uiForms != null && uiForms.Length > 0)
                {
                    foreach (IUIForm uiForm in uiForms)
                    {
                        DrawUIFormInfo(uiForm);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("This group does not currently contain any UI forms.", MessageType.None);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(4f);
            }
        }

        private void DrawUIFormInfo(IUIForm uiForm)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                Utility.Text.Format("[{0}] {1}", uiForm.SerialId, uiForm.UIFormAssetName),
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "State",
                Utility.Text.Format("Depth {0} | Pause Covered {1}", uiForm.DepthInUIGroup, uiForm.PauseCoveredUIForm),
                EditorStyles.wordWrappedMiniLabel);

            if (uiForm.Handle is GameObject go)
            {
                EditorGUILayout.ObjectField("Instance", go, typeof(GameObject), true);
            }
            else
            {
                EditorGUILayout.LabelField("Instance", "<None>");
            }

            EditorGUILayout.EndVertical();
        }
    }
}
