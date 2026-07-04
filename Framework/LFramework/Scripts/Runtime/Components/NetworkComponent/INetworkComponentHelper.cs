using System;

namespace LFramework.Runtime
{
    public interface INetworkComponentHelper
    {

        /// <summary>
        /// 尝试构建发送的Protobuf.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="serialID"></param>
        /// <param name="requestParam"></param>
        /// <param name="errorCallBack"></param>
        /// <typeparam name="TRequest"></typeparam>
        /// <returns></returns>
        bool TryBuildRequest<TRequest>(
            TRequest request,
            ulong serialID,
            out NetworkRequestParam requestParam,
            NetworkMessageErrorCallBack errorCallBack);

        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="serialID"></param>
        /// <param name="clientMessageID"></param>
        /// <param name="responseArg"></param>
        /// <param name="errorCallBack"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryParseResponse(
            ulong serialID,
            int clientMessageID,
            byte[] responseArg,
            NetworkMessageErrorCallBack errorCallBack,
            out NetworkResponseResult result);

        void HandleTokenError(object sender, EventArgs e);

        void SyncCommonData(object commonData, bool isLoginMessage);

        bool HandleRequestError(
            NetworkRequestFailure failure,
            int messageID,
            NetworkRequestInfo requestInfo,
            int retryCount);
    }
}
