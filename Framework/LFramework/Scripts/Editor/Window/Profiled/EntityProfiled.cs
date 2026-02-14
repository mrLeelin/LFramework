using GameFramework;
using GameFramework.Entity;
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
            EditorGUILayout.LabelField("Entity Group Count", _entityComponent.EntityGroupCount.ToString());
            EditorGUILayout.LabelField("Entity Count (Total)", _entityComponent.EntityCount.ToString());
            IEntityGroup[] entityGroups = _entityComponent.GetAllEntityGroups();
            foreach (IEntityGroup entityGroup in entityGroups)
            {
                EditorGUILayout.LabelField(Utility.Text.Format("Entity Count ({0})", entityGroup.Name),
                    entityGroup.EntityCount.ToString());
            }
        }
    }
}