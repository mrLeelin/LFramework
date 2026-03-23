using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
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

        private ResourceComponent _resourceComponent;
        private SpriteAtlas _spriteAtlas;
        private ResourceAssetHandle<SpriteAtlas> _spriteAtlasHandle;
        private string _spriteAtlasPath = string.Empty;

        private ResourceAssetHandle<Sprite> _singleSpriteHandle;
        private Sprite _singleSprite;
        private string _singleSpritePath = string.Empty;
        private int _requestSerial;
        private bool _isDestroyed;

        private ResourceComponent ResourceComponent
        {
            get
            {
                if (_resourceComponent != null)
                {
                    return _resourceComponent;
                }

                if (LFrameworkAspect.Instance == null || !LFrameworkAspect.Instance.HasBinding<ResourceComponent>())
                {
                    Log.Error("CustomImage can not resolve ResourceComponent.");
                    return null;
                }

                _resourceComponent = LFrameworkAspect.Instance.Get<ResourceComponent>();
                return _resourceComponent;
            }
        }

        protected override void OnDestroy()
        {
            _isDestroyed = true;
            ReleaseLoadedResources();
            base.OnDestroy();
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
            int requestSerial = ++_requestSerial;

            if (imageResource.IsInvalid)
            {
                Log.Error("CustomImage SetSpriteTask imageResource is invalid url:{0}", imageResource.OriginUrl);
                ReleaseLoadedResources();
                return;
            }

            if (!imageResource.IsSpriteAtlas)
            {
                await SetImageSingleTask(imageResource, requestSerial);
                return;
            }

            await SetImageAtlasTask(imageResource, requestSerial);
        }

        private async UniTask SetImageAtlasTask(ImageResource imageResource, int requestSerial)
        {
            if (string.Equals(_spriteAtlasPath, imageResource.SpriteAtlasPath, StringComparison.Ordinal) &&
                _spriteAtlas != null)
            {
                SetAlpha(1f);
                var cachedSprite = _spriteAtlas.GetSprite(imageResource.SpritePath);
                if (cachedSprite == null)
                {
                    Log.Error("CustomImage SetSpriteTask spriteInAtlas is null url:{0}", imageResource.OriginUrl);
                    return;
                }

                SetSprite(cachedSprite);
                return;
            }

            var resourceComponent = ResourceComponent;
            if (resourceComponent == null)
            {
                return;
            }

            ReleaseLoadedResources();
            SetAlpha(0f);

            var handle = resourceComponent.LoadAssetHandle<SpriteAtlas>(imageResource.SpriteAtlasPath);
            SpriteAtlas spriteAtlas;
            try
            {
                spriteAtlas = await handle;
            }
            catch (Exception ex)
            {
                handle.Release();
                if (ShouldIgnoreRequest(requestSerial))
                {
                    return;
                }

                SetAlpha(1f);
                Log.Error("CustomImage failed to load atlas '{0}' for '{1}': {2}",
                    imageResource.SpriteAtlasPath, imageResource.OriginUrl, ex.Message);
                return;
            }

            if (ShouldIgnoreRequest(requestSerial))
            {
                handle.Release();
                return;
            }

            SetAlpha(1f);
            if (spriteAtlas == null)
            {
                handle.Release();
                Log.Error("CustomImage SetSpriteTask _spriteAtlas is null url:{0}", imageResource.SpriteAtlasPath);
                return;
            }

            var spriteInAtlas = spriteAtlas.GetSprite(imageResource.SpritePath);
            if (spriteInAtlas == null)
            {
                handle.Release();
                Log.Error("CustomImage SetSpriteTask spriteInAtlas is null url:{0}", imageResource.OriginUrl);
                return;
            }

            _spriteAtlasHandle = handle;
            _spriteAtlas = spriteAtlas;
            _spriteAtlasPath = imageResource.SpriteAtlasPath;
            SetSprite(spriteInAtlas);
        }

        private async UniTask SetImageSingleTask(ImageResource imageResource, int requestSerial)
        {
            if (string.Equals(_singleSpritePath, imageResource.SpritePath, StringComparison.Ordinal) &&
                _singleSprite != null)
            {
                SetAlpha(1f);
                SetSprite(_singleSprite);
                return;
            }

            var resourceComponent = ResourceComponent;
            if (resourceComponent == null)
            {
                return;
            }

            ReleaseLoadedResources();
            SetAlpha(0f);

            var handle = resourceComponent.LoadAssetHandle<Sprite>(imageResource.SpritePath);
            Sprite singleSprite;
            try
            {
                singleSprite = await handle;
            }
            catch (Exception ex)
            {
                handle.Release();
                if (ShouldIgnoreRequest(requestSerial))
                {
                    return;
                }

                SetAlpha(1f);
                Log.Error("CustomImage failed to load sprite '{0}' for '{1}': {2}",
                    imageResource.SpritePath, imageResource.OriginUrl, ex.Message);
                return;
            }

            if (ShouldIgnoreRequest(requestSerial))
            {
                handle.Release();
                return;
            }

            SetAlpha(1f);
            if (singleSprite == null)
            {
                handle.Release();
                Log.Error("CustomImage SetSpriteTask _singleSprite is null url:{0} DebugName {1}",
                    imageResource.SpritePath, imageResource.OriginUrl);
                return;
            }

            _singleSpriteHandle = handle;
            _singleSprite = singleSprite;
            _singleSpritePath = imageResource.SpritePath;
            SetSprite(singleSprite);
        }

        private bool ShouldIgnoreRequest(int requestSerial)
        {
            return _isDestroyed || requestSerial != _requestSerial;
        }

        private void ReleaseLoadedResources()
        {
            SetSprite(null);

            if (_spriteAtlasHandle != null)
            {
                _spriteAtlasHandle.Release();
                _spriteAtlasHandle = null;
            }

            if (_singleSpriteHandle != null)
            {
                _singleSpriteHandle.Release();
                _singleSpriteHandle = null;
            }

            _spriteAtlas = null;
            _singleSprite = null;
            _spriteAtlasPath = string.Empty;
            _singleSpritePath = string.Empty;
        }

        private void SetAlpha(float value)
        {
            var c = color;
            c.a = value;
            color = c;
        }
    }
}
