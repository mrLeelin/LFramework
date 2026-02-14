
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
            
            EditorGUILayout.LabelField("Current Procedure", _procedureComponent.CurrentProcedure == null ? "None" : _procedureComponent.CurrentProcedure.GetType().ToString());

            
        }
    }
}