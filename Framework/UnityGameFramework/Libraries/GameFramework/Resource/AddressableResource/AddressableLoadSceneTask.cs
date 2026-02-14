using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Resource
{
    internal class AddressableLoadSceneTask : AddressableLoadResourceTask
    {
        private LoadSceneCallbacks m_LoadSceneCallbacks;

        public AddressableLoadSceneTask()
        {
            m_LoadSceneCallbacks = null;
        }

        public override bool IsScene
        {
            get { return true; }
        }

        public static AddressableLoadSceneTask Create(string sceneAssetName, int priority,
            LoadSceneCallbacks loadSceneCallbacks, object userData)
        {
            AddressableLoadSceneTask loadSceneTask = ReferencePool.Acquire<AddressableLoadSceneTask>();
            loadSceneTask.Initialize(sceneAssetName, null, priority,false, userData);
            loadSceneTask.m_LoadSceneCallbacks = loadSceneCallbacks;
            return loadSceneTask;
        }

        public override void Clear()
        {
            base.Clear();
            m_LoadSceneCallbacks = null;
        }

        public override void OnLoadAssetSuccess(AddressableLoadResourceAgent agent, object asset, float duration)
        {
            base.OnLoadAssetSuccess(agent, asset, duration);
            if (m_LoadSceneCallbacks.LoadSceneSuccessCallback != null)
            {
                m_LoadSceneCallbacks.LoadSceneSuccessCallback(AssetName, duration, UserData);
            }
        }

        public override void OnLoadAssetFailure(AddressableLoadResourceAgent agent, LoadResourceStatus status,
            string errorMessage)
        {
            base.OnLoadAssetFailure(agent, status, errorMessage);
            if (m_LoadSceneCallbacks.LoadSceneFailureCallback != null)
            {
                m_LoadSceneCallbacks.LoadSceneFailureCallback(AssetName, status, errorMessage, UserData);
            }
        }

        public override void OnLoadAssetUpdate(AddressableLoadResourceAgent agent, LoadResourceProgress type,
            float progress)
        {
            base.OnLoadAssetUpdate(agent, type, progress);
            if (type == LoadResourceProgress.LoadScene)
            {
                if (m_LoadSceneCallbacks.LoadSceneUpdateCallback != null)
                {
                    m_LoadSceneCallbacks.LoadSceneUpdateCallback(AssetName, progress, UserData);
                }
            }
        }
    }
}