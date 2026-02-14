using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Resource
{

    public delegate void ResourceInitSuccessCallBack();
    public delegate void ResourceInitFailureCallBack(string errorMessage);

    public sealed class ResourceInitCallBack 
    {
        private readonly ResourceInitSuccessCallBack _resourceInitSuccessCallBack;
        private readonly ResourceInitFailureCallBack _resourceInitFailureCallBack;

        /// <summary>
        /// 初始化加载数据流回调函数集的新实例。
        /// </summary>
        /// <param name="loadBinarySuccessCallback">加载数据流成功回调函数。</param>
        internal ResourceInitCallBack(ResourceInitSuccessCallBack loadBinarySuccessCallback)
            : this(loadBinarySuccessCallback, null)
        {
        }

        /// <summary>
        /// 初始化加载数据流回调函数集的新实例。
        /// </summary>
        /// <param name="loadBytesSuccessCallback">加载数据流成功回调函数。</param>
        /// <param name="loadBytesFailureCallback">加载数据流失败回调函数。</param>
        internal ResourceInitCallBack(ResourceInitSuccessCallBack loadBytesSuccessCallback, ResourceInitFailureCallBack loadBytesFailureCallback)
        {
            if (loadBytesSuccessCallback == null)
            {
                throw new GameFrameworkException("Load bytes success callback is invalid.");
            }

            _resourceInitSuccessCallBack = loadBytesSuccessCallback;
            _resourceInitFailureCallBack = loadBytesFailureCallback;
        }

        /// <summary>
        /// 获取加载数据流成功回调函数。
        /// </summary>
        public ResourceInitSuccessCallBack ResourceInitSuccessCallBack
        {
            get
            {
                return _resourceInitSuccessCallBack;
            }
        }

        /// <summary>
        /// 获取加载数据流失败回调函数。
        /// </summary>
        public ResourceInitFailureCallBack ResourceInitFailureCallBack
        {
            get
            {
                return _resourceInitFailureCallBack;
            }
        }
    }

}
