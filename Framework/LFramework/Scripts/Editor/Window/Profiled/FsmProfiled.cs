using GameFramework;
using GameFramework.Fsm;
using LFramework.Editor;
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
            if (_fsmComponent == null)
            {
                EditorGUILayout.HelpBox("FsmComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("FSM Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("FSM Count", _fsmComponent.Count.ToString());
            EditorGUILayout.EndVertical();

            FsmBase[] fsms = _fsmComponent.GetAllFsms();
            GameWindowChrome.DrawCompactHeader("FSM Instances");
            EditorGUILayout.BeginVertical("box");
            if (fsms == null || fsms.Length == 0)
            {
                EditorGUILayout.LabelField("No FSM instances found.");
            }
            else
            {
                foreach (FsmBase fsm in fsms)
                {
                    DrawFsm(fsm);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFsm(FsmBase fsm)
        {
            EditorGUILayout.LabelField(
                fsm.FullName,
                fsm.IsRunning
                    ? Utility.Text.Format("{0}, {1:F1} s", fsm.CurrentStateName, fsm.CurrentStateTime)
                    : (fsm.IsDestroyed ? "Destroyed" : "Not Running"));
        }
    }
}
