using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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
        private bool _viewCreated;

        [SerializeField] [Sirenix.OdinInspector.ReadOnly]
        private int serialID;


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
            UIFormAssetName = uiFormAssetName;
            UIGroup = uiGroup;
            DepthInUIGroup = 0;
            PauseCoveredUIForm = pauseCoveredUIForm;
            _viewCreated = false;
            if (!isNewInstance)
            {
                if (!IsUIBehaviourAlive())
                {
                    throw new GameFramework.GameFrameworkException(
                        $"Reused UI form '{uiFormAssetName}' has no live UI behaviour.");
                }

                return;
            }

            var uiBehaviour = this.GetComponent<IUILifecycle>();

            if (uiBehaviour == null)
            {
                throw new GameFramework.GameFrameworkException(
                    $"UI form '{uiFormAssetName}' can not get UI behaviour.");
            }

            _uiBehaviour = uiBehaviour;
            uiBehaviour.OnInterInit(userData);
        }

        public void OnRecycle()
        {
            if (!IsUIBehaviourAlive())
            {
                SerialId = 0;
                serialID = 0;
                DepthInUIGroup = 0;
                _viewCreated = false;
                throw new GameFramework.GameFrameworkException(
                    $"UI form '{UIFormAssetName}' can not recycle without a live UI behaviour.");
            }

            try
            {
                _uiBehaviour.OnInterRecycle();
            }
            finally
            {
                SerialId = 0;
                serialID = 0;
                DepthInUIGroup = 0;
                _viewCreated = false;
            }
        }

        public void OnRelease()
        {
            if (!IsUIBehaviourAlive())
            {
                throw new GameFramework.GameFrameworkException(
                    $"UI form '{UIFormAssetName}' can not release without a live UI behaviour.");
            }

            _uiBehaviour.OnInterRelease();
        }

        public void OnOpen(object userData)
        {
            if (!IsUIBehaviourAlive())
            {
                throw new GameFramework.GameFrameworkException(
                    $"UI form '{UIFormAssetName}' can not open without a live UI behaviour.");
            }

            _viewCreated = true;
            LFrameworkAspect.Instance.Get<ViewComponent>().ViewCreated(_uiBehaviour as IView);
            _uiBehaviour.OnInterOpen(userData);
        }

        public void OnClose(bool isShutdown, object userData)
        {
            if (!IsUIBehaviourAlive())
            {
                return;
            }

            Exception closeException = null;
            try
            {
                _uiBehaviour.OnInterClose(isShutdown, userData);
            }
            catch (Exception exception)
            {
                closeException = exception;
            }

            Exception destroyException = null;
            if (_viewCreated)
            {
                _viewCreated = false;
                try
                {
                    LFrameworkAspect.Instance.Get<ViewComponent>().ViewDestroyed(_uiBehaviour as IView);
                }
                catch (Exception exception)
                {
                    destroyException = exception;
                }
            }

            if (closeException != null && destroyException != null)
            {
                throw new AggregateException("UI form close and view destruction both failed.",
                    closeException, destroyException);
            }

            if (closeException != null)
            {
                ExceptionDispatchInfo.Capture(closeException).Throw();
            }

            if (destroyException != null)
            {
                ExceptionDispatchInfo.Capture(destroyException).Throw();
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

        public TUiBehaviour FindUiBehaviour<TUiBehaviour>() where TUiBehaviour : class
        {
            try
            {
                var result = _uiBehaviour as TUiBehaviour;
                if (result == null)
                {
                    Log.Error(
                        $"UI form '[{SerialId}{UIFormAssetName}]' FindUiBehaviour with exception '{typeof(TUiBehaviour).FullName}' returned null");
                }

                return result;
            }
            catch (Exception e)
            {
                Log.Error($"UI form '[{SerialId}{UIFormAssetName}]' FindUiBehaviour with exception '{e}'");
            }

            return null;
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
