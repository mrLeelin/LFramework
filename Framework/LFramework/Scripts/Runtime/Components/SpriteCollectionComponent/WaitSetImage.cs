using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace LFramework.Runtime
{
    [System.Serializable]
    public class WaitSetImage : ISetSpriteObject 
    {
        private enum SpriteType
        {
            None,
            Image,
            SpriteRenderer
        }

        /// <summary>
        /// 
        /// </summary>
        private SpriteType _spriteType;

        [ShowInInspector] private Image m_Image;

        [ShowInInspector] private SpriteRenderer _spriteRenderer;

        public static WaitSetImage Create(Image obj, string collection, string spriteName)
        {
            WaitSetImage waitSetImage = ReferencePool.Acquire<WaitSetImage>();
            waitSetImage.m_Image = obj;
            waitSetImage.SpritePath = spriteName;
            waitSetImage.CollectionPath = collection;
            waitSetImage._spriteType = SpriteType.Image;
            return waitSetImage;
        }


        public static WaitSetImage Create(SpriteRenderer obj, string collection, string spriteName)
        {
            WaitSetImage waitSetImage = ReferencePool.Acquire<WaitSetImage>();
            waitSetImage._spriteRenderer = obj;
            waitSetImage.SpritePath = spriteName;
            waitSetImage.CollectionPath = collection;
            waitSetImage._spriteType = SpriteType.SpriteRenderer;
            return waitSetImage;
        }

        [ShowInInspector] public string SpritePath { get; protected set; }
        [ShowInInspector] public Sprite SpriteInstance { get; protected set; }
        [ShowInInspector] public string CollectionPath { get; protected set; }
        [ShowInInspector] private string SpriteName { get; set; }

        public virtual void SetSprite(Sprite sprite)
        {
            if (_spriteType == SpriteType.Image && m_Image != null)
            {
                m_Image.sprite = sprite;
            }
            else if (_spriteType == SpriteType.SpriteRenderer && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }

            SpriteInstance = sprite;
        }

        public virtual bool IsCanRelease()
        {
            if (_spriteType == SpriteType.Image)
            {
                return m_Image == null || m_Image.sprite == null || m_Image.sprite != SpriteInstance;
            }

            if (_spriteType == SpriteType.SpriteRenderer)
            {
                if (_spriteRenderer == null)
                {
                    return true;
                }

                var sprite = _spriteRenderer.sprite;
                return sprite == null ||
                       sprite != SpriteInstance;
            }

            return true;
        }

        public virtual void Clear()
        {
            m_Image = null;
            _spriteRenderer = null;
            SpritePath = null;
            CollectionPath = null;
            SpriteName = null;
            _spriteType = SpriteType.None;
            SpriteInstance = null;
        }
    }
}