using GameFramework;
using GameFramework.UI;
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

            DrawMetricCards(
                new ProfiledMetric("UI Groups", _uiComponent.UIGroupCount.ToString(), "Registered groups"),
                new ProfiledMetric(
                    "Canvas Root",
                    _uiComponent.CanvasRoot != null ? _uiComponent.CanvasRoot.name : "None",
                    _uiComponent.CanvasRoot != null ? _uiComponent.CanvasRoot.GetType().Name : "Transform"));

            IUIGroup[] groups = _uiComponent.GetAllUIGroups();
            if (groups == null || groups.Length == 0)
            {
                DrawSection("UI Groups", "No UI groups are currently registered in the runtime component.", () =>
                {
                    DrawKeyValueRow("State", "Empty");
                });
                return;
            }

            foreach (IUIGroup group in groups)
            {
                string currentFormName = group.CurrentUIForm != null
                    ? group.CurrentUIForm.UIFormAssetName
                    : "<None>";

                DrawSection(
                    $"UI Group · {group.Name}",
                    "Current form focus, pause state, and stacked forms inside this UI group.",
                    () =>
                    {
                        DrawKeyValueRow("Summary", Utility.Text.Format("Depth {0}  Pause {1}  Count {2}", group.Depth, group.Pause, group.UIFormCount));
                        DrawKeyValueRow("Current UI Form", currentFormName);

                        IUIForm[] uiForms = group.GetAllUIForms();
                        if (uiForms == null || uiForms.Length == 0)
                        {
                            DrawKeyValueRow("Forms", "None");
                            return;
                        }

                        foreach (IUIForm uiForm in uiForms)
                        {
                            string summary = Utility.Text.Format(
                                "Depth {0}  PauseCovered {1}",
                                uiForm.DepthInUIGroup,
                                uiForm.PauseCoveredUIForm);

                            DrawKeyValueRow($"[{uiForm.SerialId}] {uiForm.UIFormAssetName}", summary);

                            if (uiForm.Handle is GameObject gameObject)
                            {
                                DrawKeyValueRow("Instance", gameObject.name);
                            }
                        }
                    });
            }
        }
    }
}
