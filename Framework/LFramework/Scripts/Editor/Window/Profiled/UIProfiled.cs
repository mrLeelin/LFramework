using GameFramework;
using GameFramework.UI;
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
            EditorGUILayout.LabelField("UI Group Count", _uiComponent.UIGroupCount.ToString());
            EditorGUILayout.ObjectField("Canvas Root", _uiComponent.CanvasRoot, typeof(Transform), true);

            IUIGroup[] groups = _uiComponent.GetAllUIGroups();
            foreach (IUIGroup group in groups)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    string currentFormName = group.CurrentUIForm != null
                        ? group.CurrentUIForm.UIFormAssetName
                        : "<None>";

                    EditorGUILayout.LabelField(
                        Utility.Text.Format("Group: {0}", group.Name),
                        Utility.Text.Format("Depth: {0}  Pause: {1}  Count: {2}", group.Depth, group.Pause, group.UIFormCount));

                    EditorGUILayout.LabelField("Current UI Form", currentFormName);

                    IUIForm[] uiForms = group.GetAllUIForms();
                    if (uiForms.Length > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (IUIForm uiForm in uiForms)
                        {
                            DrawUIFormInfo(uiForm);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawUIFormInfo(IUIForm uiForm)
        {
            EditorGUILayout.LabelField(
                Utility.Text.Format("[{0}] {1}", uiForm.SerialId, uiForm.UIFormAssetName),
                Utility.Text.Format("Depth: {0}  PauseCovered: {1}", uiForm.DepthInUIGroup, uiForm.PauseCoveredUIForm));

            if (uiForm.Handle is GameObject go)
            {
                EditorGUILayout.ObjectField("Instance", go, typeof(GameObject), true);
            }
        }
    }
}
