using UnityEngine;

namespace LFramework.Runtime
{
    public class UIChildEntityData : EntityData
    {
        public UIChildEntityData(string entityAssetsPath, Transform parent, object userData = null) : base(
            entityAssetsPath, 0)
        {
            this.Parent = parent;
            this.UserData = userData;
        }

        /// <summary>
        /// 挂载的父物体
        /// </summary>
        public Transform Parent { get; }
        
        /// <summary>
        /// 依赖的EntityId 只有当依赖的EntityId有父物体才会设置父物体
        /// </summary>
        public int DependOn { get; set; }
        
    }
}