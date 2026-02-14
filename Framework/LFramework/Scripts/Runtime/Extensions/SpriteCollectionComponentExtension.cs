using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class SpriteCollectionComponentExtension
    {
        public static void SetSprite(this SpriteCollectionComponent spriteCollectionComponent, Image image,
            string collectionPath, string spriteName)
        {
            spriteCollectionComponent.SetSprite(WaitSetImage.Create(image, collectionPath, spriteName));
        }

        public static void SetSprite(this SpriteCollectionComponent spriteCollectionComponent,
            SpriteRenderer spriteRenderer, string collectionPath, string spriteName)
        {
            spriteCollectionComponent.SetSprite(WaitSetImage.Create(spriteRenderer, collectionPath, spriteName));
        }

        public static UniTask SetSpriteAsync(this SpriteCollectionComponent spriteCollectionComponent,
            Image image, string collectionPath, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                Log.Fatal("The spriteName is null.");
            }
            return spriteCollectionComponent.SetSpriteAsync(WaitSetImage.Create(image, collectionPath, spriteName));
        }

        public static UniTask SetSpriteAsync(this SpriteCollectionComponent spriteCollectionComponent,
            SpriteRenderer spriteRenderer, string collectionPath, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                Log.Fatal("The spriteName is null.");
            }
            return spriteCollectionComponent.SetSpriteAsync(WaitSetImage.Create(spriteRenderer, collectionPath,
                spriteName));
        }
    }
}