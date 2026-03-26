using GameFramework;
using GameFramework.Entity;
using LFramework.Editor;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class EntityProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private EntityComponent _entityComponent;

        internal override void Draw()
        {
            GetComponent(ref _entityComponent);
            if (_entityComponent == null)
            {
                EditorGUILayout.HelpBox("EntityComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Entity Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Entity Group Count", _entityComponent.EntityGroupCount.ToString());
            EditorGUILayout.LabelField("Entity Count (Total)", _entityComponent.EntityCount.ToString());
            EditorGUILayout.EndVertical();

            IEntityGroup[] entityGroups = _entityComponent.GetAllEntityGroups();
            GameWindowChrome.DrawCompactHeader("Entity Groups");
            EditorGUILayout.BeginVertical("box");
            if (entityGroups == null || entityGroups.Length == 0)
            {
                EditorGUILayout.LabelField("No entity groups found.");
            }
            else
            {
                foreach (IEntityGroup entityGroup in entityGroups)
                {
                    EditorGUILayout.LabelField(
                        Utility.Text.Format("Entity Count ({0})", entityGroup.Name),
                        entityGroup.EntityCount.ToString());
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
