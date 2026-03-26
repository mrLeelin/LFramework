using System.Collections.Generic;
using GameFramework.DataNode;
using LFramework.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class DataNodeProfiled : ProfiledBase
    {
        private readonly HashSet<string> m_ExpandedNodes = new HashSet<string>();

        internal override bool CanDraw { get; } = true;

        private DataNodeComponent _dataNodeComponent;

        internal override void Draw()
        {
            GetComponent(ref _dataNodeComponent);
            if (_dataNodeComponent == null || _dataNodeComponent.Root == null)
            {
                EditorGUILayout.HelpBox("DataNodeComponent or its root node is unavailable in the current runtime state.", MessageType.Info);
                return;
            }

            IDataNode root = _dataNodeComponent.Root;
            IDataNode[] rootChildren = root.GetAllChild();
            m_ExpandedNodes.Add(root.FullName);

            GameWindowChrome.DrawCompactHeader("Overview", "Fold large branches to keep the runtime data tree readable.");
            EditorGUILayout.LabelField("Root", root.FullName);
            EditorGUILayout.LabelField("Top Level Children", rootChildren.Length.ToString());

            GUILayout.Space(6f);
            GameWindowChrome.DrawCompactHeader("Nodes");
            DrawDataNode(root);
        }

        private void DrawDataNode(IDataNode dataNode)
        {
            IDataNode[] children = dataNode.GetAllChild();
            bool hasChildren = children != null && children.Length > 0;
            string value = dataNode.ToDataString();

            if (!hasChildren)
            {
                EditorGUILayout.LabelField(dataNode.FullName, string.IsNullOrEmpty(value) ? "<Empty>" : value, EditorStyles.wordWrappedMiniLabel);
                return;
            }

            bool lastState = m_ExpandedNodes.Contains(dataNode.FullName);
            bool currentState = EditorGUILayout.Foldout(lastState, string.Format("{0}  ({1})", dataNode.FullName, children.Length), true);
            if (currentState != lastState)
            {
                if (currentState)
                {
                    m_ExpandedNodes.Add(dataNode.FullName);
                }
                else
                {
                    m_ExpandedNodes.Remove(dataNode.FullName);
                }
            }

            if (!string.IsNullOrEmpty(value))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Value", value, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }

            if (!currentState)
            {
                return;
            }

            EditorGUI.indentLevel++;
            foreach (IDataNode child in children)
            {
                DrawDataNode(child);
            }

            EditorGUI.indentLevel--;
        }
    }
}
