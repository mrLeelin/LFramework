using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Window 子实体加载参数。
    /// </summary>
    public struct AddChildParam
    {
        /// <summary>
        /// 子实体显示后要挂载到的父节点。
        /// </summary>
        public Transform ParentTransform;

        /// <summary>
        /// 非零时覆盖子实体本地坐标。
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 子实体资源地址。
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// 传给子实体的自定义数据。
        /// </summary>
        public object UserData;

        /// <summary>
        /// 依赖的实体编号。非 0 时会等依赖实体准备好后再设置父节点。
        /// </summary>
        public int DependOn;

        /// <summary>
        /// 非 0 时按等比缩放设置子实体大小。
        /// </summary>
        public float Size;


        /// <summary>
        /// 创建子实体加载参数。
        /// </summary>
        /// <param name="parentTransform">子实体显示后要挂载到的父节点。</param>
        /// <param name="assetPath">子实体资源地址。</param>
        /// <returns>加载参数。</returns>
        public static AddChildParam Create(Transform parentTransform, string assetPath)
        {
            return new AddChildParam()
            {
                ParentTransform = parentTransform,
                AssetPath = assetPath,
            };
        }

        /// <summary>
        /// 设置传给子实体的自定义数据。
        /// </summary>
        /// <param name="userData">自定义数据。</param>
        /// <returns>更新后的加载参数。</returns>
        public AddChildParam SetUserData(object userData)
        {
            this.UserData = userData;
            return this;
        }

        /// <summary>
        /// 设置依赖实体。子实体会在依赖实体准备好后再挂到目标父节点。
        /// </summary>
        /// <param name="dependOn">依赖实体编号，0 表示不等待依赖。</param>
        /// <returns>更新后的加载参数。</returns>
        public AddChildParam SetDependOn(int dependOn)
        {
            this.DependOn = dependOn;
            return this;
        }

        /// <summary>
        /// 设置等比缩放。
        /// </summary>
        /// <param name="size">缩放倍数，0 表示不覆盖。</param>
        /// <returns>更新后的加载参数。</returns>
        public AddChildParam SetSize(float size)
        {
            this.Size = size;
            return this;
        }

        /// <summary>
        /// 设置本地坐标。
        /// </summary>
        /// <param name="position">本地坐标，Vector3.zero 表示不覆盖。</param>
        /// <returns>更新后的加载参数。</returns>
        public AddChildParam SetPosition(Vector3 position)
        {
            this.Position = position;
            return this;
        }

        /// <summary>
        /// 尝试转换为子实体数据。调用加载接口前统一走这里，避免同步/异步流程字段不一致。
        /// </summary>
        /// <param name="entityData">转换后的子实体数据。</param>
        /// <returns>参数是否合法并成功生成实体数据。</returns>
        public bool TryCreateEntityData(out UIChildEntityData entityData)
        {
            entityData = null;
            if (!IsValid())
            {
                return false;
            }

            entityData = new UIChildEntityData(AssetPath, ParentTransform, UserData)
            {
                DependOn = DependOn,
            };

            if (Position != Vector3.zero)
            {
                entityData.Position = Position;
            }

            if (Size != 0)
            {
                entityData.Size = Vector3.one * Size;
            }

            return true;
        }

        /// <summary>
        /// 检查参数是否足够发起子实体加载。
        /// </summary>
        /// <returns>参数是否有效。</returns>
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
