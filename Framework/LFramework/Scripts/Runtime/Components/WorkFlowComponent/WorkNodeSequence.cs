using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public class WorkNodeSequence : WorkNodeGroup
    {
        private WorkNode mCurrentNode = null;
        private int mCurrentNodeIndex = 0;

        public WorkNode NextNode
        {
            get
            {
                int nextIndex = mCurrentNodeIndex + 1;
                if (nextIndex >= 0 && nextIndex <= Count - 1)
                    return Nodes[nextIndex];
                return null;
            }
        }

        public WorkNode CurrentNode => mCurrentNode;

        /// <summary>
        /// 外部强制开启下一个引导
        /// </summary>
        public void Next() => OnNext();


        public override void OnStart()
        {
            base.OnStart();
            if (!IsEnable)
            {
                return;
            }
            ResetCurrent();

            if (Count <= 0)
            {
                return;
            }
            mCurrentNode = Nodes[mCurrentNodeIndex];
            while (mCurrentNode.IsEnable == false && mCurrentNodeIndex <= Count - 1)
            {
                mCurrentNodeIndex++;
                mCurrentNode = Nodes[mCurrentNodeIndex];
            }

            mCurrentNode.OnStart();
        }

        public override WorkFlowStatus OnUpdate()
        {
            if (!IsEnable)
            {
                return WorkFlowStatus.Successful;
            }
            if (mCurrentNode == null)
            {
                return base.OnUpdate();
            }
            if (!mCurrentNode.IsEnable)
            {
                return OnNext() ? WorkFlowStatus.Successful : WorkFlowStatus.Running;
            }
            var status = mCurrentNode.OnJumpNode()
                ? WorkFlowStatus.Successful
                : mCurrentNode.OnUpdate();

            if (status == WorkFlowStatus.Failed)
            {
                return WorkFlowStatus.Failed;
            }
            if (status == WorkFlowStatus.Successful)
            {
                return OnNext() ? WorkFlowStatus.Successful : WorkFlowStatus.Running;
            }
            return status;
        }

        private bool OnNext()
        {
            mCurrentNode.OnEnd();
            mCurrentNodeIndex++;
            if (mCurrentNodeIndex >= Count)
            {
                OnEnd();
                return true; // 最后结束本节点
            }
            else
            {
                // 下一个节点
                mCurrentNode = Nodes[mCurrentNodeIndex];
                if (mCurrentNode.OnJumpNode())
                {
                    return false;
                }

                mCurrentNode.OnStart();
                return false;
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            if (IsEnable)
            {
                if (mCurrentNode != null)
                {
                    mCurrentNode.OnStop();
                }

                for (int i = 0, count = Nodes.Count; i < count; i++)
                {
                    Nodes[i].OnStop();
                }

                ResetCurrent();
            }
        }

        public override void OnDispose()
        {
            base.OnDispose();
            if (!IsEnable)
            {
                return;
            }
            OnEnd();
            if (mCurrentNode != null)
            {
                mCurrentNode.OnDispose();
            }

            for (int i = 0, count = Nodes.Count; i < count; i++)
            {
                Nodes[i].OnDispose();
            }

            Clear();
        }
        
        public override void OnEnd()
        {
            base.OnEnd();
            if (IsEnable)
            {
                if (mCurrentNode != null)
                {
                    mCurrentNode.OnEnd();
                }

                ResetCurrent();
            }
        }

        public override void OnResetWorkFlow()
        {
            base.OnResetWorkFlow();
            if (IsEnable)
            {
                if (mCurrentNode != null)
                {
                    mCurrentNode.OnResetWorkFlow();
                }

                for (int i = 0, count = Nodes.Count; i < count; i++)
                {
                    Nodes[i].OnResetWorkFlow();
                }

                ResetCurrent();
            }
        }

        private void ResetCurrent()
        {
            mCurrentNode = null;
            mCurrentNodeIndex = 0;
        }
    }
}