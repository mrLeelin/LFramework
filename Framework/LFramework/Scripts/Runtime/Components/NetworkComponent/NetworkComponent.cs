using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework;

namespace LFramework.Runtime
{
    public partial class NetworkComponent : GameFrameworkComponent
    {
        [SerializeField] private int _loginTimeOutSecond = 10;
        [SerializeField] private int _timeOutSecond = 10;
        [SerializeField] private string _networkChannelTypeName = null;

        private INetworkChannel _networkChannel;
        private INetworkComponentHelper _helper;
        private RequestQueue _requestQueue;

        private ulong _serialID;
        private bool _isSetUp;
        private bool _channelInitialized;

        public override void AwakeComponent()
        {
            base.AwakeComponent();
            CreateInstance();
            CreateConfiguredChannel();
        }

        public override void SetUpComponent()
        {
            base.SetUpComponent();
            _isSetUp = true;
            InitializeChannelIfReady();
        }

        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            base.UpdateComponent(elapseSeconds, realElapseSeconds);
            _networkChannel?.OnUpdate(elapseSeconds, realElapseSeconds);
            _requestQueue?.OnUpdate();
        }

        public override void ShutDown()
        {
            base.ShutDown();
            if (_networkChannel != null)
            {
                _networkChannel.TokenErrorCallBack -= OnTokenErrorCallBack;
            }

            _helper = null;
            _requestQueue?.Dispose();
            _requestQueue = null;
            DestroyChannelObject(_networkChannel);
            _networkChannel = null;
            _channelInitialized = false;
            _isSetUp = false;
        }

        public void SetHelper(INetworkComponentHelper helper)
        {
            _helper = helper;
        }

        private void SetChannel(INetworkChannel networkChannel)
        {
            if (_networkChannel == networkChannel)
            {
                return;
            }

            if (_networkChannel != null)
            {
                _networkChannel.TokenErrorCallBack -= OnTokenErrorCallBack;
            }

            var previousChannel = _networkChannel;
            _requestQueue?.Dispose();
            _networkChannel = networkChannel;
            _requestQueue = _networkChannel == null
                ? null
                : new RequestQueue(_networkChannel, OnRequestErrorCallBack);
            _channelInitialized = false;
            DestroyChannelObject(previousChannel);

            if (_networkChannel is NetworkChannelBase channelBase)
            {
                channelBase.name = "Network Channel";
                if (Instance != null)
                {
                    channelBase.transform.SetParent(Instance);
                    channelBase.transform.localScale = Vector3.one;
                }
            }

            if (_networkChannel != null)
            {
                _networkChannel.TokenErrorCallBack += OnTokenErrorCallBack;
            }

            InitializeChannelIfReady();
        }

        public void ResetNetwork()
        {
            _requestQueue?.Clear();
            _networkChannel?.Clear();
        }

        public void Connect(string url)
        {
            _networkChannel?.Connect(url);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="request"></param>
        /// <param name="successfulCallBack"></param>
        /// <param name="errorCallBack"></param>
        /// <typeparam name="TRequest"></typeparam>
        public void Send<TRequest>(TRequest request, NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            if (_helper == null)
            {
                Log.Error("NetworkComponent helper is not registered.");
                errorCallBack?.Invoke(0, 0, (int)NetworkRuntimeErrorCode.HelperNotRegistered);
                return;
            }

            var serialID = GetNextSerialID();
            if (!_helper.TryBuildRequest(request, serialID, out var param, errorCallBack))
            {
                return;
            }

            if (_requestQueue == null)
            {
                Log.Error("NetworkComponent request queue is not initialized.");
                errorCallBack?.Invoke(param.MessageID,
                    0,
                    (int)NetworkRuntimeErrorCode.RequestQueueNotInitialized);
                return;
            }

            _requestQueue.PushRequestMsg(param, OnResp, successfulCallBack, errorCallBack);
        }

        private ulong GetNextSerialID()
        {
            return ++_serialID;
        }

        private void OnResp(
            ulong serialID,
            int clientMessageID,
            byte[] responseArg,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            if (_helper == null)
            {
                Log.Error("NetworkComponent helper is not registered.");
                errorCallBack?.Invoke(clientMessageID, 0, (int)NetworkRuntimeErrorCode.HelperNotRegistered);
                return;
            }

            if (!_helper.TryParseResponse(serialID, clientMessageID, responseArg, errorCallBack, out var result))
            {
                return;
            }

            try
            {
                if (result.IsLoginMessage)
                {
                    LFrameworkAspect.Instance.FireNow(this, NetworkLoginCompleteArgs.Create(result.Package));
                }

                _helper.SyncCommonData(result.CommonData, result.IsLoginMessage);
                LFrameworkAspect.Instance.FireNow(
                    this,
                    NetworkCompleteArgs.Create(result.ServerTime, result.Package));
                if (result.IsLoginMessage)
                {
                    LFrameworkAspect.Instance.FireNow(
                        this,
                        NetworkLoginCompleteAfterOnUpdateArgs.Create(result.Package));
                }

                successfulCallBack?.Invoke(result.ClientMessageID, result.ServerTime, result.Receive);
            }
            finally
            {
                result.Package?.Release();
            }
        }

        private void OnTokenErrorCallBack(object sender, System.EventArgs e)
        {
            _helper?.HandleTokenError(sender, e);
            LFrameworkAspect.Instance.FireNow(this, NetworkTokenErrorArgs.Create());
        }

        private bool OnRequestErrorCallBack(
            NetworkRequestFailure failure,
            int messageID,
            NetworkRequestInfo requestInfo,
            int retryCount)
        {
            if (_helper == null)
            {
                Log.Error("NetworkComponent helper is not registered.");
                return false;
            }

            if (!requestInfo.Reconnect)
            {
                return false;
            }

            return _helper.HandleRequestError(failure, messageID, requestInfo, retryCount);
        }

        private void InitializeChannelIfReady()
        {
            if (!_isSetUp || _networkChannel == null || _channelInitialized)
            {
                return;
            }

            _networkChannel.Initialize();
            _networkChannel.SetTimeOut(_loginTimeOutSecond, _timeOutSecond);
            _channelInitialized = true;
        }

        private void CreateConfiguredChannel()
        {
            if (string.IsNullOrWhiteSpace(_networkChannelTypeName))
            {
                Log.Error("NetworkComponent channel type is not configured.");
                return;
            }

            var channelType = Utility.Assembly.GetType(_networkChannelTypeName);
            if (channelType == null)
            {
                Log.Error("NetworkComponent channel type '{0}' is not found.", _networkChannelTypeName);
                return;
            }

            if (channelType.IsAbstract ||
                channelType.IsInterface ||
                !typeof(NetworkChannelBase).IsAssignableFrom(channelType))
            {
                Log.Error(
                    "NetworkComponent channel type '{0}' must be a non-abstract type that inherits from '{1}'.",
                    _networkChannelTypeName,
                    typeof(NetworkChannelBase).FullName);
                return;
            }

            SetChannel(new GameObject().AddComponent(channelType) as INetworkChannel);
        }

        private static void DestroyChannelObject(INetworkChannel networkChannel)
        {
            if (networkChannel is not NetworkChannelBase channelBase || channelBase == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(channelBase.gameObject);
            }
            else
            {
                Object.DestroyImmediate(channelBase.gameObject);
            }
        }
    }
}
