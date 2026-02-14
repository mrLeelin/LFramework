using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public struct ImageResource
    {
        public string SpriteAtlasPath { get; set; }
        public string SpritePath { get; set; }
        public bool IsSpriteAtlas { get; set; }
        public bool IsInvalid => string.IsNullOrEmpty(SpriteAtlasPath) && string.IsNullOrEmpty(SpritePath);

        public string OriginUrl { get; set; }

        public static implicit operator ImageResource(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return default;
            }

            var result = new ImageResource
            {
                OriginUrl = url,
                IsSpriteAtlas = url.ExtractKeyAndSubKey(out var mainKey, out var subKey)
            };
            if (result.IsSpriteAtlas)
            {
                result.SpriteAtlasPath = mainKey;
                result.SpritePath = subKey;
            }
            else
            {
                result.SpritePath = url;
            }

            return result;
        }
    }

    public class CustomImage : Image
    {
        [SerializeField] public bool keepNativeSize = true;


        private SpriteAtlas _spriteAtlas;
        private AsyncOperationHandle<SpriteAtlas> _spriteAtlasHandle;
        private string _spriteAtlasPath;

        private AsyncOperationHandle<Sprite> _singleSpriteHandle;
        private Sprite _singleSprite;
        private string _singleSpritePath;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnLoad();
        }

        public virtual void SetSprite(Sprite spriteValue)
        {
            
            sprite = spriteValue;
            if (keepNativeSize && sprite != null)
            {
                SetNativeSize();
            }
            
        }

        public void SetImage(ImageResource imageResource)
        {
            _ = SetImageTask(imageResource);
        }

        public async UniTask SetImageTask(ImageResource imageResource)
        {
            if (imageResource.IsInvalid)
            {
                Log.Error("CustomImage SetSpriteTask imageResource is invalid url:{0}", imageResource.OriginUrl);
                return;
            }

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
                if (keepNativeSize && sprite != null)
                {
                    SetNativeSize();
                }

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
            var c = this.color;
            c.a = value;
            this.color = c;
        }
    }
}