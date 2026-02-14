using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.FileSystem;
using GameFramework.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UnityGameFramework.Runtime
{
    internal class AddressableLoadResourceAgentHelper : LoadResourceAgentHelperBase, IDisposable
    {
        private string m_FileFullPath = null;
        private bool m_Disposed = false;
        private float m_LastProgress = 0f;
        private bool _isLoadAssetsOrScene;

        private AsyncOperationHandle<System.Object>? _assetsBundleOperationHandle;
        private AsyncOperationHandle<SceneInstance>? _sceneOperationHandle;
        private AsyncOperationHandle<GameObject>? _instantiateOperationHandle;


        private EventHandler<LoadResourceAgentHelperUpdateEventArgs> m_LoadResourceAgentHelperUpdateEventHandler = null;


        private EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs>
            m_LoadResourceAgentHelperLoadCompleteEventHandler = null;

        private EventHandler<LoadResourceAgentHelperErrorEventArgs> m_LoadResourceAgentHelperErrorEventHandler = null;


        /// <summary>
        /// 加载资源代理辅助器异步加载资源更新事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperUpdateEventArgs> LoadResourceAgentHelperUpdate
        {
            add { m_LoadResourceAgentHelperUpdateEventHandler += value; }
            remove { m_LoadResourceAgentHelperUpdateEventHandler -= value; }
        }

        /// <summary>
        /// 加载资源代理辅助器异步读取资源文件完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperReadFileCompleteEventArgs>
            LoadResourceAgentHelperReadFileComplete;

        /// <summary>
        /// 加载资源代理辅助器异步读取资源二进制流完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperReadBytesCompleteEventArgs>
            LoadResourceAgentHelperReadBytesComplete;

        /// <summary>
        /// 加载资源代理辅助器异步将资源二进制流转换为加载对象完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperParseBytesCompleteEventArgs>
            LoadResourceAgentHelperParseBytesComplete;

        /// <summary>
        /// 加载资源代理辅助器异步加载资源完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs>
            LoadResourceAgentHelperLoadComplete
            {
                add { m_LoadResourceAgentHelperLoadCompleteEventHandler += value; }
                remove { m_LoadResourceAgentHelperLoadCompleteEventHandler -= value; }
            }

        /// <summary>
        /// 加载资源代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperErrorEventArgs> LoadResourceAgentHelperError
        {
            add { m_LoadResourceAgentHelperErrorEventHandler += value; }
            remove { m_LoadResourceAgentHelperErrorEventHandler -= value; }
        }

        private void Update()
        {
            if (!_isLoadAssetsOrScene)
            {
                return;
            }

            UpdateAssetBundleRequest();
            UpdateSceneRequest();
            UpdateInstantiateOperationRequest();
        }

        public override void ReadFile(string fullPath)
        {
            throw new NotImplementedException();
        }

        public override void ReadFile(IFileSystem fileSystem, string name)
        {
            throw new NotImplementedException();
        }

        public override void ReadBytes(string fullPath)
        {
            throw new NotImplementedException();
        }

        public override void ReadBytes(IFileSystem fileSystem, string name)
        {
            throw new NotImplementedException();
        }

        public override void ParseBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public override void LoadAsset(object resource, string assetName, Type assetType, bool isScene)
        {
            if (m_LoadResourceAgentHelperLoadCompleteEventHandler == null ||
                m_LoadResourceAgentHelperUpdateEventHandler == null ||
                m_LoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs =
                    LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError,
                        "Can not load asset from asset bundle which child name is invalid.");
                m_LoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                return;
            }

            m_FileFullPath = assetName;
            if (isScene)
            {
                _sceneOperationHandle = Addressables.LoadSceneAsync(assetName, LoadSceneMode.Additive, true, 100);
            }
            else
            {
                _assetsBundleOperationHandle = Addressables.LoadAssetAsync<System.Object>(assetName);
            }

            _isLoadAssetsOrScene = true;
        }

        public override void InstantiateAsset(string assetsName)
        {
            if (m_LoadResourceAgentHelperLoadCompleteEventHandler == null ||
                m_LoadResourceAgentHelperUpdateEventHandler == null ||
                m_LoadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            if (string.IsNullOrEmpty(assetsName))
            {
                LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs =
                    LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError,
                        "Can not load asset from asset bundle which child name is invalid.");
                m_LoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                return;
            }

            m_FileFullPath = assetsName;
            _instantiateOperationHandle = Addressables.InstantiateAsync(assetsName);
            _isLoadAssetsOrScene = true;
        }

        public override void Reset()
        {
            m_FileFullPath = null;
            m_LastProgress = 0f;
            _sceneOperationHandle = null;
            _assetsBundleOperationHandle = null;
            _instantiateOperationHandle = null;
        }


        private void UpdateInstantiateOperationRequest()
        {
            if (!_instantiateOperationHandle.HasValue)
            {
                return;
            }

            if (!_instantiateOperationHandle.Value.IsValid())
            {
                return;
            }

            if (_instantiateOperationHandle.Value.IsDone)
            {
                var assets = _instantiateOperationHandle.Value.Result;
                if (_instantiateOperationHandle.Value.Status == AsyncOperationStatus.Succeeded)
                {
                    LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperReadFileCompleteEventArgs
                        = LoadResourceAgentHelperLoadCompleteEventArgs.Create(assets);
                    m_LoadResourceAgentHelperLoadCompleteEventHandler(this,
                        loadResourceAgentHelperReadFileCompleteEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperReadFileCompleteEventArgs);
                    _isLoadAssetsOrScene = false;
                }
                else
                {
                    LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs =
                        LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError,
                            Utility.Text.Format("Can not load asset '{0}' from asset bundle which is not exist.",
                                m_FileFullPath));
                    m_LoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                }

                return;
            }

            if (_instantiateOperationHandle.Value.PercentComplete > m_LastProgress)
            {
                m_LastProgress = _instantiateOperationHandle.Value.PercentComplete;
                LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs =
                    LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadAsset,
                        _instantiateOperationHandle.Value.PercentComplete);
                m_LoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
            }
        }

        private void UpdateSceneRequest()
        {
            if (!_sceneOperationHandle.HasValue)
            {
                return;
            }

            if (!_sceneOperationHandle.Value.IsValid())
            {
                return;
            }

            if (_sceneOperationHandle.Value.IsDone)
            {
                var assets = _sceneOperationHandle.Value.Result;
                if (_sceneOperationHandle.Value.Status == AsyncOperationStatus.Succeeded)
                {
                    LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperReadFileCompleteEventArgs
                        = LoadResourceAgentHelperLoadCompleteEventArgs.Create(assets);
                    m_LoadResourceAgentHelperLoadCompleteEventHandler(this,
                        loadResourceAgentHelperReadFileCompleteEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperReadFileCompleteEventArgs);
                    _isLoadAssetsOrScene = false;
                }
                else
                {
                    LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs =
                        LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError,
                            Utility.Text.Format("Can not load asset '{0}' from asset bundle which is not exist.",
                                m_FileFullPath));
                    m_LoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                }

                return;
            }

            if (_sceneOperationHandle.Value.PercentComplete > m_LastProgress)
            {
                m_LastProgress = _sceneOperationHandle.Value.PercentComplete;
                LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs =
                    LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadScene,
                        _sceneOperationHandle.Value.PercentComplete);
                m_LoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
            }
        }

        private void UpdateAssetBundleRequest()
        {
            if (!_assetsBundleOperationHandle.HasValue)
            {
                return;
            }

            if (!_assetsBundleOperationHandle.Value.IsValid())
            {
                return;
            }

            if (_assetsBundleOperationHandle.Value.IsDone)
            {
                var assets = _assetsBundleOperationHandle.Value.Result;
                if (_assetsBundleOperationHandle.Value.Status == AsyncOperationStatus.Succeeded)
                {
                    LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperReadFileCompleteEventArgs
                        = LoadResourceAgentHelperLoadCompleteEventArgs.Create(assets);
                    m_LoadResourceAgentHelperLoadCompleteEventHandler(this,
                        loadResourceAgentHelperReadFileCompleteEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperReadFileCompleteEventArgs);
                    _isLoadAssetsOrScene = false;
                }
                else
                {
                    LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs =
                        LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError,
                            Utility.Text.Format("Can not load asset '{0}' from asset bundle which is not exist.",
                                m_FileFullPath));
                    m_LoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                }

                return;
            }

            if (_assetsBundleOperationHandle.Value.PercentComplete > m_LastProgress)
            {
                m_LastProgress = _assetsBundleOperationHandle.Value.PercentComplete;
                LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs =
                    LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadAsset,
                        _assetsBundleOperationHandle.Value.PercentComplete);
                m_LoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
            {
                return;
            }

            if (disposing)
            {
                Addressables.Release(_sceneOperationHandle);
                Addressables.Release(_assetsBundleOperationHandle);
            }

            m_Disposed = true;
        }
    }
}