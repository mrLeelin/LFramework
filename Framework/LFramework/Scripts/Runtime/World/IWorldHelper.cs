using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface IWorldHelper : IReference
    {
        /// <summary>
        /// 设置World
        /// </summary>
        /// <param name="worldBase"></param>
        public void SetWorld(WorldBase worldBase);

        /// <summary>
        /// Init
        /// </summary>
        public void Initialize();

        /// <summary>
        /// Start Call
        /// </summary>
        public void StartGame();
        /// <summary>
        /// Instantiate World
        /// </summary>
        /// <returns></returns>
        public UniTask InstantiateWorld();
        /// <summary>
        /// Stop Game
        /// </summary>
        public void StopGame();
    }
}

