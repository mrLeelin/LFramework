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
        public object Handle => this != null ? gameObject : null;
        public IUIGroup UIGroup { get; private set; }
        public int DepthInUIGroup { get; private set; }
        public bool PauseCoveredUIForm { get; private set; }

        public GameObject HandleGo => this != null ? gameObject : null;
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
                uiBehaviour.OnInterInit(userData);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{serialId}{uiFormAssetName}]' OnInit with exception '{e}'");
            }

            _uiBehaviour = uiBehaviour;
        }

        public void OnRecycle()
        {
            if (!IsUIBehaviourAlive())
            {
                SerialId = 0;
                DepthInUIGroup = 0;
                return;
            }

            try
            {
                _uiBehaviour.OnInterRecycle();
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
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterRelease();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnRelease with exception '{e}'");
            }
        }
        
        public void OnOpen(object userData)
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }
        
            try
            {
                LFrameworkAspect.Instance.Get<ViewComponent>().ViewCreated(_uiBehaviour as IView);
                _uiBehaviour.OnInterOpen(userData);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnOpen with exception '{e}'");
            }
        }

        public void OnClose(bool isShutdown, object userData)
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                if (!isShutdown)
                {
                    _uiBehaviour.OnInterClose(isShutdown,userData);
                }
                LFrameworkAspect.Instance.Get<ViewComponent>().ViewDestroyed(_uiBehaviour as IView);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnClose with exception '{e}'");
            }
            
           
        }

        public void OnPause()
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterPause();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnPause with exception '{e}'");
            }
        }

        public void OnResume()
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterResume();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnResume with exception '{e}'");
            }
        }

        public void OnCover()
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterCover();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnCover with exception '{e}'");
            }
        }

        public void OnReveal()
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterReveal();
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnReveal with exception '{e}'");
            }
        }

        public void OnRefocus(object userData)
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterRefocus(userData);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnRefocus with exception '{e}'");
            }
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterUpdate(elapseSeconds, realElapseSeconds);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnUpdate with exception '{e}'");
            }
        }

        public void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            DepthInUIGroup = depthInUIGroup;
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            try
            {
                _uiBehaviour.OnInterDepthChanged(uiGroupDepth, depthInUIGroup);
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' OnDepthChanged with exception '{e}'");
            }
        }

        private bool IsUIBehaviourAlive()
        {
            if (_uiBehaviour == null)
            {
                return false;
            }

            return !(_uiBehaviour is UnityEngine.Object unityObject) || unityObject != null;
        }
    }
}
