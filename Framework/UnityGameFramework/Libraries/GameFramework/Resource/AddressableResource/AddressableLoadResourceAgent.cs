using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Resource
{
    internal class AddressableLoadResourceAgent : ITaskAgent<AddressableLoadResourceTask>
    {

        private readonly ILoadResourceAgentHelper m_Helper;
        private readonly IResourceHelper m_ResourceHelper;
        private readonly AddressableResourceLoader m_ResourceLoader;
        private AddressableLoadResourceTask m_Task;
        
        public AddressableLoadResourceAgent(
            ILoadResourceAgentHelper loadResourceAgentHelper
            , IResourceHelper resourceHelper
            , AddressableResourceLoader resourceLoader)
        {
            if (loadResourceAgentHelper == null)
            {
                throw new GameFrameworkException("Load resource agent helper is invalid.");
            }

            if (resourceHelper == null)
            {
                throw new GameFrameworkException("Resource helper is invalid.");
            }

            if (resourceLoader == null)
            {
                throw new GameFrameworkException("Resource loader is invalid.");
            }

            m_Helper = loadResourceAgentHelper;
            m_ResourceHelper = resourceHelper;
            m_ResourceLoader = resourceLoader;
            m_Task = null;
        }

        public AddressableLoadResourceTask Task => m_Task;
        
        public void Initialize()
        {
            m_Helper.LoadResourceAgentHelperUpdate += OnLoadResourceAgentHelperUpdate;
            m_Helper.LoadResourceAgentHelperLoadComplete += OnLoadResourceAgentHelperLoadComplete;
            m_Helper.LoadResourceAgentHelperError += OnLoadResourceAgentHelperError;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            
        }

        public void Shutdown()
        {
            Reset();
            m_Helper.LoadResourceAgentHelperUpdate -= OnLoadResourceAgentHelperUpdate;
            m_Helper.LoadResourceAgentHelperLoadComplete -= OnLoadResourceAgentHelperLoadComplete;
            m_Helper.LoadResourceAgentHelperError -= OnLoadResourceAgentHelperError;
        }

        public StartTaskStatus Start(AddressableLoadResourceTask task)
        {
            m_Task = task ?? throw new GameFrameworkException("Task is invalid.");
            m_Task.StartTime = DateTime.UtcNow;
            if (m_Task.IsInstantiate)
            {
                m_Helper.InstantiateAsset(m_Task.AssetName);
            }
            else
            {
                m_Helper.LoadAsset(null, m_Task.AssetName, m_Task.AssetType, m_Task.IsScene);
            }
       
            return StartTaskStatus.CanResume;
        }

        public void Reset()
        {
            m_Helper.Reset();
            m_Task = null;
        }
        
        private void OnError(LoadResourceStatus status, string errorMessage)
        {
            m_Helper.Reset();
            m_Task.OnLoadAssetFailure(this, status, errorMessage);
            m_Task.Done = true;
        }

        private void OnLoadResourceAgentHelperUpdate(object sender, LoadResourceAgentHelperUpdateEventArgs e)
        {
            m_Task.OnLoadAssetUpdate(this, e.Type, e.Progress);
        }
        
        private void OnLoadResourceAgentHelperLoadComplete(object sender, LoadResourceAgentHelperLoadCompleteEventArgs e)
        {
            OnAssetObjectReady(e.Asset);
        }
        private void OnLoadResourceAgentHelperError(object sender, LoadResourceAgentHelperErrorEventArgs e)
        {
            OnError(e.Status, e.ErrorMessage);
        }
        
        private void OnAssetObjectReady(object assetObject)
        {
            m_Helper.Reset();
            
            if (m_Task.IsScene)
            {
                m_ResourceLoader.m_SceneToAssetMap.Add(m_Task.AssetName, assetObject);
            }

            m_Task.OnLoadAssetSuccess(this, assetObject, (float)(DateTime.UtcNow - m_Task.StartTime).TotalSeconds);
            m_Task.Done = true;
         
        }
    }
}

