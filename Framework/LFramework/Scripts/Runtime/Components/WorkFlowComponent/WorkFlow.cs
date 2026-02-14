using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    public class WorkFlow : WorkNodeSequence
    {
        [SerializeField] private bool mIsRunOnStart = true;
        [SerializeField] private bool mWhenFinishIsRestart = false;
      
        
        private WorkFlowStatus mStatus = WorkFlowStatus.None;

        
        public WorkNode Root => this;
        
        /// <summary>
        /// 是否正在运行工作流
        /// </summary>
        public bool IsRunning => mStatus == WorkFlowStatus.Running;


      

        public bool IsRunOnStart
        {
            get => mIsRunOnStart;
            set => mIsRunOnStart = value;
        }

        public bool WhenFinishIsRestart
        {
            get => mWhenFinishIsRestart;
            set => mWhenFinishIsRestart = value;
        }
        
        
        private void Update()
        {
            if (Time.timeScale == 0)
            {
                return;
            }
            if (mStatus == WorkFlowStatus.Running)
            {
                mStatus = Root.OnUpdate();
            }
        }

        private void OnDestroy()
        {
            Root.OnDispose();
            Root.OnResetWorkFlow();
        }

        private void OnApplicationQuit()
        {
            Root.OnDispose();
        }


        public override void OnStart()
        {
            if (mStatus == WorkFlowStatus.Running)
            {
                return;
            }
            base.OnStart();
            mStatus = WorkFlowStatus.Running;
        }

        private void RunOnStart()
        {
            if (IsRunOnStart)
            {
                OnStart();
            }
        }

        public void InitAndStart()
        {
            BindGroupAndChildren(this, this.transform);
            RunOnStart();
        }

        public void BindGroupAndChildren(WorkNode groupNode, Transform groupTrans)
        {
            if (groupNode != null && groupNode is WorkNodeGroup group)
            {
                group.WorkFlow = this;
                int childCount = groupTrans.childCount;
                if (childCount > 0)
                {
                    for (int i = 0; i < childCount; i++)
                    {
                        WorkNode childNode =
                            groupTrans.GetChild(i).GetComponent<WorkNode>();
                        group.AddNode(childNode);
                        childNode.WorkFlow = this;
                    }
                }
            }
        }
        

        public void Stop(bool isDestroy = true)
        {
            mStatus = WorkFlowStatus.None;
            StopReal(); // invoke one time
            if (isDestroy)
            {
                GameObject.Destroy(this.gameObject);
            }
            else
            {
                if (WhenFinishIsRestart)
                {
                    OnStart();
                }
            }
        }

        private void StopReal()
        {
            Root.OnStop();
            Root.OnResetWorkFlow();
        }

        public T[] FindNode<T>(Predicate<WorkNode> match) where T : WorkNode
        {
            List<T> result = new List<T>();
            CollectNodes<T>(this, ref result, match);
            T[] array = result.ToArray();
            result.Clear();
            return array;
        }
        

        private void CollectNodes<T>(WorkNodeGroup group, ref List<T> result, Predicate<WorkNode> match)
            where T : WorkNode
        {
            List<WorkNode> list = group.Nodes;
            List<WorkNode> list_result = list.FindAll(match);
            if (list_result != null)
            {
                for (int i = 0, count = list_result.Count; i < count; i++)
                {
                    result.Add((T)list_result[i]);
                }
            }

            List<WorkNode> temp_group = list.FindAll(x => x.GetType().BaseType == typeof(WorkNodeGroup));
            if (temp_group != null)
            {
                for (int i = 0, count = temp_group.Count; i < count; i++)
                {
                    CollectNodes<T>(temp_group[i] as WorkNodeGroup, ref result, match);
                }
            }
        }
    }
}