using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Resource
{
    internal class AddressableLoadAssetTask : AddressableLoadResourceTask
    {
        private LoadAssetCallbacks m_LoadAssetCallbacks;

        public AddressableLoadAssetTask()
        {
            m_LoadAssetCallbacks = null;
        }

        public override bool IsScene
        {
            get { return false; }
        }

        public static AddressableLoadAssetTask Create(string assetName, int priority, Type assetType,
            LoadAssetCallbacks loadAssetCallbacks, bool isInstantiate,object userData)
        {
            AddressableLoadAssetTask loadAssetTask = ReferencePool.Acquire<AddressableLoadAssetTask>();
            loadAssetTask.Initialize(assetName, assetType, priority,isInstantiate, userData);
            loadAssetTask.m_LoadAssetCallbacks = loadAssetCallbacks;
            return loadAssetTask;
        }

        public override void Clear()
        {
            base.Clear();
            m_LoadAssetCallbacks = null;
        }

        public override void OnLoadAssetSuccess(AddressableLoadResourceAgent agent, object asset, float duration)
        {
            base.OnLoadAssetSuccess(agent, asset, duration);
            if (m_LoadAssetCallbacks.LoadAssetSuccessCallback != null)
            {
                m_LoadAssetCallbacks.LoadAssetSuccessCallback(AssetName, asset, duration, UserData);
            }
        }

        public override void OnLoadAssetFailure(AddressableLoadResourceAgent agent, LoadResourceStatus status,
            string errorMessage)
        {
            base.OnLoadAssetFailure(agent, status, errorMessage);
            if (m_LoadAssetCallbacks.LoadAssetFailureCallback != null)
            {
                m_LoadAssetCallbacks.LoadAssetFailureCallback(AssetName, status, errorMessage, UserData);
            }
        }

        public override void OnLoadAssetUpdate(AddressableLoadResourceAgent agent, LoadResourceProgress type,
            float progress)
        {
            base.OnLoadAssetUpdate(agent, type, progress);
            if (type == LoadResourceProgress.LoadAsset)
            {
                if (m_LoadAssetCallbacks.LoadAssetUpdateCallback != null)
                {
                    m_LoadAssetCallbacks.LoadAssetUpdateCallback(AssetName, progress, UserData);
                }
            }
        }
    }
}