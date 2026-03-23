using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CustomSpriteRenderer : UnityEngine.MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private ResourceComponent _resourceComponent;
        private SpriteAtlas _spriteAtlas;
        private ResourceAssetHandle<SpriteAtlas> _spriteAtlasHandle;
        private string _spriteAtlasPath = string.Empty;
        private ResourceAssetHandle<Sprite> _singleSpriteHandle;
        private Sprite _singleSprite;
        private string _singleSpritePath = string.Empty;
        private int _requestSerial;
        private bool _isDestroyed;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;

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
                    Log.Error("CustomSpriteRenderer can not resolve ResourceComponent.");
                    return null;
                }

                _resourceComponent = LFrameworkAspect.Instance.Get<ResourceComponent>();
                return _resourceComponent;
            }
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Log.Fatal("The SpriteRenderer is invalid.");
            }
        }

        protected void OnDestroy()
        {
            _isDestroyed = true;
            ReleaseLoadedResources();
        }

        public void SetSprite(Sprite spriteValue)
        {
            ThrowSpriteRenderer();
            _spriteRenderer.sprite = spriteValue;
        }

        public void SetImage(ImageResource imageResource)
        {
            ThrowSpriteRenderer();
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

            ThrowSpriteRenderer();
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

                Log.Error("CustomSpriteRenderer failed to load atlas '{0}' for '{1}': {2}",
                    imageResource.SpriteAtlasPath, imageResource.OriginUrl, ex.Message);
                return;
            }

            if (ShouldIgnoreRequest(requestSerial))
            {
                handle.Release();
                return;
            }

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
                SetSprite(_singleSprite);
                return;
            }

            var resourceComponent = ResourceComponent;
            if (resourceComponent == null)
            {
                return;
            }

            ReleaseLoadedResources();

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

                Log.Error("CustomSpriteRenderer failed to load sprite '{0}' for '{1}': {2}",
                    imageResource.SpritePath, imageResource.OriginUrl, ex.Message);
                return;
            }

            if (ShouldIgnoreRequest(requestSerial))
            {
                handle.Release();
                return;
            }

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

        private void ThrowSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                Log.Fatal("The SpriteRenderer is invalid.");
            }
        }
    }
}
