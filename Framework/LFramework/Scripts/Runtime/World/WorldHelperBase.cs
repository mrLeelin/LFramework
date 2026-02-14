using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    [PreLoadZenject]
    public abstract class WorldHelperBase : IWorldHelper
    {
        private WorldBase _worldBase;

        public abstract void Clear();

        public void SetWorld(WorldBase worldBase)
        {
            _worldBase = worldBase;
            if (_worldBase == null)
            {
                Log.Fatal("The worldBase is null.");
            }
        }

        public abstract void Initialize();

        public abstract void StartGame();

        public virtual UniTask InstantiateWorld()
        {
            return UniTask.CompletedTask;
        }

        public abstract void StopGame();


        protected virtual T GetWorld<T>() where T : WorldBase
        {
            return _worldBase as T;
        }
    }
}