using System;

using Sirenix.OdinInspector;


namespace LFramework.Runtime
{
    public partial class SpriteCollectionComponent
    {
        [Serializable]
        public class LoadSpriteObject
        {
            [ShowInInspector]
            public ISetSpriteObject SpriteObject { get; }
            [ShowInInspector]
            public SpriteCollection Collection { get; }
#if UNITY_EDITOR
            public bool IsSelect { get; set; }
#endif

            public LoadSpriteObject(ISetSpriteObject obj, SpriteCollection collection)
            {
                SpriteObject = obj;
                Collection = collection;
            }
        }
    }
}