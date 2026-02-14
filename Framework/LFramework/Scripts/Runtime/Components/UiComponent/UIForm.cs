using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.UI;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class UIForm : UnityEngine.MonoBehaviour, IUIForm
    {
        private IUILifecycle _uiBehaviour;
        [SerializeField] [Sirenix.OdinInspector.ReadOnly] private int serialID;
        
       
        public int SerialId { get; private set; }
        public string UIFormAssetName { get; private set; }
        public object Handle => gameObject;
        public IUIGroup UIGroup { get; private set; }
        public int DepthInUIGroup { get; private set; }
        public bool PauseCoveredUIForm { get; private set; }

        public GameObject HandleGo => gameObject;
        public IUIBehaviour UIBehaviour => (IUIBehaviour)_uiBehaviour;

        public void OnInit(int serialId, string uiFormAssetName, IUIGroup uiGroup, bool pauseCoveredUIForm,
            bool isNewInstance,
            object userData)
        {
            SerialId = serialId;
            serialID = serialId;
            if (!isNewInstance)
            {
                return;
            }

            UIFormAssetName = uiFormAssetName;
            UIGroup = uiGroup;
            DepthInUIGroup = 0;
            PauseCoveredUIForm = pauseCoveredUIForm;
            var uiBehaviour = this.GetComponent<IUILifecycle>();

            if (uiBehaviour == null)
            {
                Log.Fatal($"UI form '{uiFormAssetName}' can not get Ui Behaviour.");
                return;
            }

            try
            {
                uiBehaviour.OnInit(userData);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{serialId}{uiFormAssetName}]' OnInit with exception '{e}'");
            }

            _uiBehaviour = uiBehaviour;
        }

        public void OnRecycle()
        {
            try
            {
                _uiBehaviour.OnRecycle();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnRecycle with exception '{e}'");
            }

            SerialId = 0;
            DepthInUIGroup = 0;
            //PauseCoveredUIForm = true;
        }

        public void OnRelease()
        {
            try
            {
                _uiBehaviour.OnRelease();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnRelease with exception '{e}'");
            }
        }
        
        public void OnOpen(object userData)
        {
        
            try
            {
                LFrameworkAspect.Instance.Get<ViewComponent>().ViewCreated(_uiBehaviour as IView);
                _uiBehaviour.OnOpen(userData);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnOpen with exception '{e}'");
            }
        }

        public void OnClose(bool isShutdown, object userData)
        {
            try
            {
                _uiBehaviour.OnClose(isShutdown,userData);
                LFrameworkAspect.Instance.Get<ViewComponent>().ViewDestroyed(_uiBehaviour as IView);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnClose with exception '{e}'");
            }
            
           
        }

        public void OnPause()
        {
            try
            {
                _uiBehaviour.OnPause();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnPause with exception '{e}'");
            }
        }

        public void OnResume()
        {
            try
            {
                _uiBehaviour.OnResume();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnResume with exception '{e}'");
            }
        }

        public void OnCover()
        {
            try
            {
                _uiBehaviour.OnCover();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnCover with exception '{e}'");
            }
        }

        public void OnReveal()
        {
            try
            {
                _uiBehaviour.OnReveal();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnReveal with exception '{e}'");
            }
        }

        public void OnRefocus(object userData)
        {
            try
            {
                _uiBehaviour.OnRefocus(userData);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnRefocus with exception '{e}'");
            }
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            try
            {
                _uiBehaviour.OnUpdate(elapseSeconds, realElapseSeconds);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnUpdate with exception '{e}'");
            }
        }

        public void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            DepthInUIGroup = depthInUIGroup;
            try
            {
                _uiBehaviour.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnDepthChanged with exception '{e}'");
            }
        }
    }
}