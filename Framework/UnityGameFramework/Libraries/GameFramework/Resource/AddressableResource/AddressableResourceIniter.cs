using System.Collections;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    public sealed class AddressableResourceIniter
    {
        private readonly AddressableResourceManager m_ResourceManager;
        
        public GameFrameworkAction ResourceInitComplete;

        /// <summary>
        /// 初始化资源初始化器的新实例。
        /// </summary>
        /// <param name="resourceManager">资源管理器。</param>
        public AddressableResourceIniter(AddressableResourceManager resourceManager)
        {
            m_ResourceManager = resourceManager;
            ResourceInitComplete = null;
        }

        /// <summary>
        /// 关闭并清理资源初始化器。
        /// </summary>
        public void Shutdown()
        {
            ResourceInitComplete = null;
        }

        /// <summary>
        /// 初始化资源。
        /// </summary>
        public void InitResources()
        {
            if (m_ResourceManager.m_ResourceHelper == null)
            {
                throw new GameFrameworkException("Resource helper is invalid.");
            }
            
            m_ResourceManager.m_ResourceHelper.InitializeResources(new ResourceInitCallBack(OnInitializeResourcesSuccess, OnInitializeResourcesFailure));
        }

        private void OnInitializeResourcesFailure(string errormessage)
        {
            throw new GameFrameworkException(
                Utility.Text.Format("Resource init is invalid, error message is '{1}'.", 
                    string.IsNullOrEmpty(errormessage) ? "<Empty>" : errormessage));

        }

        private void OnInitializeResourcesSuccess()
        {
            ResourceInitComplete();
        }
    }
}