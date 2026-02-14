using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Resource
{
    internal abstract class AddressableLoadResourceTask : TaskBase
    {
        
        private static int s_Serial = 0;
        private DateTime m_StartTime;
        private string m_AssetName;
        private Type m_AssetType;
        private bool _isInstantiate;
        public AddressableLoadResourceTask()
        {
            _isInstantiate = false;
        }
        
        public string AssetName
        {
            get
            {
                return m_AssetName;
            }
        }

        public Type AssetType
        {
            get
            {
                return m_AssetType;
            }
        }

        public DateTime StartTime
        {
            get
            {
                return m_StartTime;
            }
            set
            {
                m_StartTime = value;
            }
        }
        
        public bool IsInstantiate
        {
            get
            {
                return _isInstantiate;
            }
        }
        public abstract bool IsScene
        {
            get;
        }
        
        
        public virtual void OnLoadAssetFailure(AddressableLoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
        {
        }
        
        public virtual void OnLoadAssetUpdate(AddressableLoadResourceAgent agent, LoadResourceProgress type, float progress)
        {
        }
        public virtual void OnLoadAssetSuccess(AddressableLoadResourceAgent agent, object asset, float duration)
        {
        }
        
        protected void Initialize(string assetName, Type assetType, int priority, bool isInstantiate, object userData)
        {
            Initialize(++s_Serial, null, priority, userData);
            m_AssetName = assetName;
            m_AssetType = assetType;
            this._isInstantiate = isInstantiate;
        }

    }

}
