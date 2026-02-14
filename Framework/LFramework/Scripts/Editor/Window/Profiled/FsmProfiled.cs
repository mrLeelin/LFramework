using GameFramework;
using GameFramework.Fsm;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class FsmProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private FsmComponent _fsmComponent;

        internal override void Draw()
        {
            GetComponent(ref _fsmComponent);

            EditorGUILayout.LabelField("FSM Count", _fsmComponent.Count.ToString());

            FsmBase[] fsms = _fsmComponent.GetAllFsms();
            foreach (FsmBase fsm in fsms)
            {
                DrawFsm(fsm);
            }
        }


        private void DrawFsm(FsmBase fsm)
        {
            EditorGUILayout.LabelField(fsm.FullName,
                fsm.IsRunning
                    ? Utility.Text.Format("{0}, {1:F1} s", fsm.CurrentStateName, fsm.CurrentStateTime)
                    : (fsm.IsDestroyed ? "Destroyed" : "Not Running"));
        }
    }
}