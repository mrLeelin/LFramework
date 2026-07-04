namespace LFramework.Runtime
{
    public delegate void NetworkMessageSuccessfulCallBack(int messageID, ulong serverTime, object response);

    public delegate void NetworkMessageErrorCallBack(int messageID, ulong serverTime, int code);

    public delegate void NetworkMessageCallBack(
        ulong serialID,
        int messageID,
        byte[] bytes,
        NetworkMessageSuccessfulCallBack successfulCallBack,
        NetworkMessageErrorCallBack errorCallBack);

    public delegate void NetworkRequestFailedCallBack(
        NetworkRequestFailure failure,
        ulong serialID,
        int messageID,
        NetworkRequestInfo requestInfo);

    public delegate bool NetworkRequestErrorCallBack(
        NetworkRequestFailure failure,
        int messageID,
        NetworkRequestInfo requestInfo,
        int retryCount);
}
