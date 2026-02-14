using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CustomSpriteRenderer : UnityEngine.MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;



        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        
        private void Awake()
        {
            _spriteRenderer = this.GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Log.Fatal("The SpriteRenderer is invalid.");
            }
        }

        private SpriteAtlas _spriteAtlas;
        private AsyncOperationHandle<SpriteAtlas> _spriteAtlasHandle;
        private string _spriteAtlasPath;

        private AsyncOperationHandle<Sprite> _singleSpriteHandle;
        private Sprite _singleSprite;
        private string _singleSpritePath;

        protected void OnDestroy()
        {
            UnLoad();
        }

        public void SetSprite(Sprite spriteValue)
        {
            ThrowSpriteRenderer();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = this.GetComponent<SpriteRenderer>();
            }
            _spriteRenderer.sprite = spriteValue;
        }

        public void SetImage(ImageResource imageResource)
        {
            ThrowSpriteRenderer();
            _ = SetImageTask(imageResource);
        }

        public async UniTask SetImageTask(ImageResource imageResource)
        {
            if (imageResource.IsInvalid)
            {
                Log.Error("CustomImage SetSpriteTask imageResource is invalid url:{0}", imageResource.OriginUrl);
                return;
            }

            ThrowSpriteRenderer();
            if (!imageResource.IsSpriteAtlas)
            {
                var task = SetImageSingleTask(imageResource);
                await task;
                return;
            }

            if (string.Equals(_spriteAtlasPath, imageResource.SpriteAtlasPath) && _spriteAtlas != null)
            {
                if (_spriteAtlas != null)
                {
                    SetSprite(_spriteAtlas.GetSprite(imageResource.SpritePath));
                }

                return;
            }

            UnLoad();
            _spriteAtlasPath = imageResource.SpriteAtlasPath;
            SetAlpha(0);
            _spriteAtlasHandle = Addressables.LoadAssetAsync<SpriteAtlas>(imageResource.SpriteAtlasPath);
            await _spriteAtlasHandle;
            //这里如果被中断会返回None
            if (_spriteAtlasHandle.Status == AsyncOperationStatus.None)
            {
                return;
            }

            SetAlpha(1);
            if (_spriteAtlasHandle.Status == AsyncOperationStatus.Failed)
            {
                Log.Error("CustomImage SetSpriteTask _spriteAtlasHandle is null url:{0}", _spriteAtlasPath);
                return;
            }

            _spriteAtlas = _spriteAtlasHandle.Result;
            if (_spriteAtlas == null)
            {
                Log.Error("CustomImage SetSpriteTask _spriteAtlas is null url:{0}", _spriteAtlasPath);
                return;
            }

            var spriteInAtlas = _spriteAtlas.GetSprite(imageResource.SpritePath);
            if (spriteInAtlas == null)
            {
                Log.Error("CustomImage SetSpriteTask spriteInAtlas is null url:{0}", imageResource.OriginUrl);
            }

            SetSprite(spriteInAtlas);
        }

        private async UniTask SetImageSingleTask(ImageResource imageResource)
        {
            if (string.Equals(_singleSpritePath, imageResource.SpritePath) && _singleSprite != null)
            {
                return;
            }

            UnLoad();
            SetAlpha(0);
            _singleSpritePath = imageResource.SpritePath;
            _singleSpriteHandle = Addressables.LoadAssetAsync<Sprite>(_singleSpritePath);
            await _singleSpriteHandle;
            //这里如果被中断会返回None
            if (_singleSpriteHandle.Status == AsyncOperationStatus.None)
            {
                return;
            }

            SetAlpha(1);
            if (_singleSpriteHandle.Status == AsyncOperationStatus.Failed)
            {
                Log.Error("CustomImage SetSpriteTask _singleSpriteHandle is null url:{0}  DebugName {1}",
                    _singleSpritePath, imageResource.OriginUrl);
                return;
            }

            _singleSprite = _singleSpriteHandle.Result;
            if (_singleSprite == null)
            {
                Log.Error("CustomImage SetSpriteTask _singleSprite is null url:{0} DebugName {1}", _singleSpritePath,
                    _singleSpriteHandle.DebugName);
            }

            SetSprite(_singleSprite);
        }

        private void UnLoad()
        {
            if (_spriteAtlas != null)
            {
                SetSprite(null);
                _spriteAtlas = null;
            }

            if (_spriteAtlasHandle.IsValid())
            {
                Addressables.Release(_spriteAtlasHandle);
            }

            if (_singleSprite != null)
            {
                SetSprite(null);
                _singleSprite = null;
            }

            if (_singleSpriteHandle.IsValid())
            {
                Addressables.Release(_singleSpriteHandle);
            }

            _spriteAtlasPath = string.Empty;
            _singleSpritePath = string.Empty;
        }

        private void SetAlpha(float value)
        {
         
        }

        private void ThrowSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                Log.Fatal("The SpriteRenderer is invalid.");
            }
        }
    }
}