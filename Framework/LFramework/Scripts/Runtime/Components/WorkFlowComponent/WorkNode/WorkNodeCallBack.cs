using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LFramework.Runtime
{
    public class WorkNodeCallBack : WorkNode
    {
        private Action _callBack;
        private Action<object> _callBackOneParam;
        private object _param;
        private WorkFlowStatus _workFlowStatus;
        public override void OnStart()
        {
            base.OnStart();

            if (_callBack != null)
            {
                _callBack.Invoke();
            }

            if (_callBackOneParam != null)
            {
                _callBackOneParam.Invoke(_param);
            }

            _workFlowStatus = WorkFlowStatus.Successful;
        }

        public override WorkFlowStatus OnUpdate()
        {
            return _workFlowStatus;
        }

        public void SetCallBack(Action callBack)
        {
            this._callBack = callBack;
        }

        public void SetCallBack(Action<object> callBack, object userData)
        {
            this._callBackOneParam = callBack;
            _param = userData;
        }
    }
}

