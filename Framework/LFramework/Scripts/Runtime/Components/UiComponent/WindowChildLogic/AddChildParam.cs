using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public struct AddChildParam
    {
        public Transform ParentTransform;

        public string AssetPath;

        public object UserData;

        public int DependOn;

        public float Size;

        public Vector3 Position;


        /// <summary>
        /// 创建参数
        /// </summary>
        /// <param name="parentTransform"></param>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static AddChildParam Create(Transform parentTransform, string assetPath)
        {
            return new AddChildParam()
            {
                ParentTransform = parentTransform,
                AssetPath = assetPath,
            };
        }

        /// <summary>
        /// 设置自定义数据
        /// </summary>
        /// <param name="userData"></param>
        /// <returns></returns>
        public AddChildParam SetUserData(object userData)
        {
            this.UserData = userData;
            return this;
        }

        /// <summary>
        /// 设置依赖
        /// </summary>
        /// <param name="dependOn"></param>
        /// <returns></returns>
        public AddChildParam SetDependOn(int dependOn)
        {
            this.DependOn = dependOn;
            return this;
        }

        /// <summary>
        /// 设置大小
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public AddChildParam SetSize(float size)
        {
            this.Size = size;
            return this;
        }

        /// <summary>
        /// 设置坐标
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public AddChildParam SetPosition(Vector3 position)
        {
            this.Position = position;
            return this;
        }

        /// <summary>
        /// 是否可用的
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            var flag = ParentTransform != null && !string.IsNullOrEmpty(AssetPath);
            if (!flag)
            {
                Log.Error(
                    $"The [AddChildParam] component is invalid. ParentTransform: '{ParentTransform}' AssetPath:'{AssetPath}']");
            }

            return flag;
        }
    }
}