using System;
using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Hotfix
{
    public interface IWorldRegister
    {
        /// <summary>
        /// 尝试注册世界
        /// </summary>
        /// <param name="procedureState"></param>
        IWorld TryRegisterWorld(int procedureState);

        /// <summary>
        /// 解除世界注册
        /// </summary>
        void TryUnRegisterWorld();
    }

}
