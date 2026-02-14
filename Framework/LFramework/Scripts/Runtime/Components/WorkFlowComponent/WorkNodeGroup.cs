using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public class WorkNodeGroup : WorkNode
    {
        private readonly List<WorkNode> _nodes = new List<WorkNode>();

        public void AddNode(WorkNode node)
        {
            _nodes.Add(node);
        }

        public void JoinNode(int index, WorkNode node)
        {
            _nodes.Insert(index, node);
        }

        public void RemoveNode(WorkNode node)
        {
            _nodes.Remove(node);
        }

        public bool HasNode(WorkNode node)
        {
            return _nodes.Contains(node);
        }

        public List<WorkNode> Nodes => _nodes;

        public int Count => _nodes.Count;

        public void Clear()
        {
            _nodes.Clear();
        }
    }
}