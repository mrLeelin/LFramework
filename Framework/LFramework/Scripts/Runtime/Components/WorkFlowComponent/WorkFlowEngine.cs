using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;


namespace LFramework.Runtime
{
    public delegate void WorkFlowFinishedCallBack();
    
    public abstract class WorkFlowEngine : IWorkFlowEngine
    {
        private bool _isRunOnStart;
        private bool _whenFinishRestart;
        private Action<object> _finishNotifyEventId;
        private object _finishNotifyEventParam;
        private Action<object> _startNotifyEventId;
        private object _startNotifyEventParam;
        private WorkFlowFinishedCallBack _workFlowFinishedCallBack;
        private WorkFlow _curGo;
        private int _index;
        
        protected WorkFlow CurWorkFlow => _curGo;


        public virtual void Dispose()
        {
            _isRunOnStart = false;
            _whenFinishRestart = false;
            _finishNotifyEventId = null;
            _finishNotifyEventParam = null;
        }


        public void SetStartNotifyEvent(Action<object> callBack, object param)
        {
            _startNotifyEventId = callBack;
            _startNotifyEventParam = param;
        }

        public void SetFinishNotifyEvent(Action<object> callBack, object param)
        {
            _finishNotifyEventId = callBack;
            _finishNotifyEventParam = param;
        }

        public void SetFinishCallBack(WorkFlowFinishedCallBack callBack)
        {
            if (callBack == null)
            {
                return;
            }

            this._workFlowFinishedCallBack = callBack;
        }

        public void SetIsRunOnStart(bool isRunOnStart) => _isRunOnStart = isRunOnStart;
        
        public void SetWhenFinishIsRestart(bool whenFinishIsRestart) => _whenFinishRestart = whenFinishIsRestart;

        
        public void Fork(ref WorkFlow workFlow) => Fork(ref workFlow, CreateGo(null, $"[WorkFlow:{GetType().Name}]"));

        public void Fork([NotNull] ref WorkFlow workFlow, GameObject go)
        {
            workFlow = go.GetOrAddComponent<WorkFlow>();
            _curGo = workFlow;
            workFlow.IsRunOnStart = this._isRunOnStart;
            workFlow.WhenFinishIsRestart = this._whenFinishRestart;
            CreateStartNotifyNode(workFlow.transform);
            DoFork(workFlow);
            CreateFinishedCallBackNode(workFlow.transform);
            CreateFinishNotifyNode(workFlow.transform);
            CreateAutoDestroyNode(workFlow.transform);
            workFlow.InitAndStart();
        }

        protected abstract void DoFork(WorkFlow workFlow);

        

        private GameObject CreateGo(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }
        
        private void CreateAutoDestroyNode(Transform parent) =>
            CreateNode(parent, typeof(WorkNodeAutoDestroy), "[AutoDestroyNode]");

        private void CreateFinishNotifyNode(Transform parent)
        {
            var binder =
                CreateNode(parent, typeof(WorkNodeCallBack), "[NotifyNode]") as WorkNodeCallBack;
            binder.SetCallBack(_finishNotifyEventId, _finishNotifyEventParam);
        }

        private void CreateStartNotifyNode(Transform parent)
        {
            var binder =
                CreateNode(parent, typeof(WorkNodeCallBack), "[Start NotifyNode]") as WorkNodeCallBack;
            binder.SetCallBack(_startNotifyEventId, _startNotifyEventParam);
        }
        
        private void CreateFinishedCallBackNode(Transform parent)
        {
            if (_workFlowFinishedCallBack == null)
            {
                return;
            }

          
        }
        
        protected WorkNode CreateNode(Transform parent, Type type, string name)
        {
            var go = CreateGo(parent, name);
            if (!typeof(WorkNode).IsAssignableFrom(type))
            {
                Log.Fatal($"The type '{type.FullName}' is not MonoBehaviour");
            }
            var t = go.AddComponent(type) as WorkNode;
            LFrameworkAspect.Instance.DiContainer.Inject(t);
            return t;
        }
 
        
    }
}