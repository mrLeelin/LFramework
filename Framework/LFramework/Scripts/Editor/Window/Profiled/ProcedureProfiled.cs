using LFramework.Editor;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ProcedureProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private ProcedureComponent _procedureComponent;

        internal override void Draw()
        {
            GetComponent(ref _procedureComponent);
            if (_procedureComponent == null)
            {
                EditorGUILayout.HelpBox("ProcedureComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Procedure Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(
                "Current Procedure",
                _procedureComponent.CurrentProcedure == null
                    ? "None"
                    : _procedureComponent.CurrentProcedure.GetType().ToString());
            EditorGUILayout.LabelField(
                "Current Procedure Time",
                _procedureComponent.CurrentProcedure == null
                    ? "N/A"
                    : $"{_procedureComponent.CurrentProcedureTime:F1}s");
            EditorGUILayout.EndVertical();
        }
    }
}
