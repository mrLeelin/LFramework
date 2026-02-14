using GameFramework.DataNode;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class DataNodeProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private DataNodeComponent _dataNodeComponent;

        internal override void Draw()
        {
            GetComponent(ref _dataNodeComponent);
            DrawDataNode(_dataNodeComponent.Root);
        }


        private void DrawDataNode(IDataNode dataNode)
        {
            EditorGUILayout.LabelField(dataNode.FullName, dataNode.ToDataString());
            IDataNode[] child = dataNode.GetAllChild();
            foreach (IDataNode c in child)
            {
                DrawDataNode(c);
            }
        }
    }
}