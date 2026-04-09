//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine.Scripting;

namespace GameFramework.Network
{
    /// <summary>
    /// 缃戠粶绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private readonly Dictionary<string, NetworkChannelBase> m_NetworkChannels;

        private EventHandler<NetworkConnectedEventArgs> m_NetworkConnectedEventHandler;
        private EventHandler<NetworkClosedEventArgs> m_NetworkClosedEventHandler;
        private EventHandler<NetworkMissHeartBeatEventArgs> m_NetworkMissHeartBeatEventHandler;
        private EventHandler<NetworkErrorEventArgs> m_NetworkErrorEventHandler;
        private EventHandler<NetworkCustomErrorEventArgs> m_NetworkCustomErrorEventHandler;

        /// <summary>
        /// 鍒濆鍖栫綉缁滅鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public NetworkManager()
        {
            m_NetworkChannels = new Dictionary<string, NetworkChannelBase>(StringComparer.Ordinal);
            m_NetworkConnectedEventHandler = null;
            m_NetworkClosedEventHandler = null;
            m_NetworkMissHeartBeatEventHandler = null;
            m_NetworkErrorEventHandler = null;
            m_NetworkCustomErrorEventHandler = null;
        }

        /// <summary>
        /// 鑾峰彇缃戠粶棰戦亾鏁伴噺銆?
        /// </summary>
        public int NetworkChannelCount
        {
            get
            {
                return m_NetworkChannels.Count;
            }
        }

        /// <summary>
        /// 缃戠粶杩炴帴鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<NetworkConnectedEventArgs> NetworkConnected
        {
            add
            {
                m_NetworkConnectedEventHandler += value;
            }
            remove
            {
                m_NetworkConnectedEventHandler -= value;
            }
        }

        /// <summary>
        /// 缃戠粶杩炴帴鍏抽棴浜嬩欢銆?
        /// </summary>
        public event EventHandler<NetworkClosedEventArgs> NetworkClosed
        {
            add
            {
                m_NetworkClosedEventHandler += value;
            }
            remove
            {
                m_NetworkClosedEventHandler -= value;
            }
        }

        /// <summary>
        /// 缃戠粶蹇冭烦鍖呬涪澶变簨浠躲€?
        /// </summary>
        public event EventHandler<NetworkMissHeartBeatEventArgs> NetworkMissHeartBeat
        {
            add
            {
                m_NetworkMissHeartBeatEventHandler += value;
            }
            remove
            {
                m_NetworkMissHeartBeatEventHandler -= value;
            }
        }

        /// <summary>
        /// 缃戠粶閿欒浜嬩欢銆?
        /// </summary>
        public event EventHandler<NetworkErrorEventArgs> NetworkError
        {
            add
            {
                m_NetworkErrorEventHandler += value;
            }
            remove
            {
                m_NetworkErrorEventHandler -= value;
            }
        }

        /// <summary>
        /// 鐢ㄦ埛鑷畾涔夌綉缁滈敊璇簨浠躲€?
        /// </summary>
        public event EventHandler<NetworkCustomErrorEventArgs> NetworkCustomError
        {
            add
            {
                m_NetworkCustomErrorEventHandler += value;
            }
            remove
            {
                m_NetworkCustomErrorEventHandler -= value;
            }
        }

        /// <summary>
        /// 缃戠粶绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in m_NetworkChannels)
            {
                networkChannel.Value.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗙綉缁滅鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in m_NetworkChannels)
            {
                NetworkChannelBase networkChannelBase = networkChannel.Value;
                networkChannelBase.NetworkChannelConnected -= OnNetworkChannelConnected;
                networkChannelBase.NetworkChannelClosed -= OnNetworkChannelClosed;
                networkChannelBase.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
                networkChannelBase.NetworkChannelError -= OnNetworkChannelError;
                networkChannelBase.NetworkChannelCustomError -= OnNetworkChannelCustomError;
                networkChannelBase.Shutdown();
            }

            m_NetworkChannels.Clear();
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄧ綉缁滈閬撱€?
        /// </summary>
        /// <param name="name">缃戠粶棰戦亾鍚嶇О銆?/param>
        /// <returns>鏄惁瀛樺湪缃戠粶棰戦亾銆?/returns>
        public bool HasNetworkChannel(string name)
        {
            return m_NetworkChannels.ContainsKey(name ?? string.Empty);
        }

        /// <summary>
        /// 鑾峰彇缃戠粶棰戦亾銆?
        /// </summary>
        /// <param name="name">缃戠粶棰戦亾鍚嶇О銆?/param>
        /// <returns>瑕佽幏鍙栫殑缃戠粶棰戦亾銆?/returns>
        public INetworkChannel GetNetworkChannel(string name)
        {
            NetworkChannelBase networkChannel = null;
            if (m_NetworkChannels.TryGetValue(name ?? string.Empty, out networkChannel))
            {
                return networkChannel;
            }

            return null;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夌綉缁滈閬撱€?
        /// </summary>
        /// <returns>鎵€鏈夌綉缁滈閬撱€?/returns>
        public INetworkChannel[] GetAllNetworkChannels()
        {
            int index = 0;
            INetworkChannel[] results = new INetworkChannel[m_NetworkChannels.Count];
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in m_NetworkChannels)
            {
                results[index++] = networkChannel.Value;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夌綉缁滈閬撱€?
        /// </summary>
        /// <param name="results">鎵€鏈夌綉缁滈閬撱€?/param>
        public void GetAllNetworkChannels(List<INetworkChannel> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in m_NetworkChannels)
            {
                results.Add(networkChannel.Value);
            }
        }

        /// <summary>
        /// 鍒涘缓缃戠粶棰戦亾銆?
        /// </summary>
        /// <param name="name">缃戠粶棰戦亾鍚嶇О銆?/param>
        /// <param name="serviceType">缃戠粶鏈嶅姟绫诲瀷銆?/param>
        /// <param name="networkChannelHelper">缃戠粶棰戦亾杈呭姪鍣ㄣ€?/param>
        /// <returns>瑕佸垱寤虹殑缃戠粶棰戦亾銆?/returns>
        public INetworkChannel CreateNetworkChannel(string name, ServiceType serviceType, INetworkChannelHelper networkChannelHelper)
        {
            if (networkChannelHelper == null)
            {
                throw new GameFrameworkException("Network channel helper is invalid.");
            }

            if (networkChannelHelper.PacketHeaderLength < 0)
            {
                throw new GameFrameworkException("Packet header length is invalid.");
            }

            if (HasNetworkChannel(name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist network channel '{0}'.", name ?? string.Empty));
            }

            NetworkChannelBase networkChannel = null;
            switch (serviceType)
            {
                case ServiceType.Tcp:
                    networkChannel = new TcpNetworkChannel(name, networkChannelHelper);
                    break;

                case ServiceType.TcpWithSyncReceive:
                    networkChannel = new TcpWithSyncReceiveNetworkChannel(name, networkChannelHelper);
                    break;

                default:
                    throw new GameFrameworkException(Utility.Text.Format("Not supported service type '{0}'.", serviceType));
            }

            networkChannel.NetworkChannelConnected += OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed += OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat += OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError += OnNetworkChannelError;
            networkChannel.NetworkChannelCustomError += OnNetworkChannelCustomError;
            m_NetworkChannels.Add(name, networkChannel);
            return networkChannel;
        }

        /// <summary>
        /// 閿€姣佺綉缁滈閬撱€?
        /// </summary>
        /// <param name="name">缃戠粶棰戦亾鍚嶇О銆?/param>
        /// <returns>鏄惁閿€姣佺綉缁滈閬撴垚鍔熴€?/returns>
        public bool DestroyNetworkChannel(string name)
        {
            NetworkChannelBase networkChannel = null;
            if (m_NetworkChannels.TryGetValue(name ?? string.Empty, out networkChannel))
            {
                networkChannel.NetworkChannelConnected -= OnNetworkChannelConnected;
                networkChannel.NetworkChannelClosed -= OnNetworkChannelClosed;
                networkChannel.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
                networkChannel.NetworkChannelError -= OnNetworkChannelError;
                networkChannel.NetworkChannelCustomError -= OnNetworkChannelCustomError;
                networkChannel.Shutdown();
                return m_NetworkChannels.Remove(name);
            }

            return false;
        }

        private void OnNetworkChannelConnected(NetworkChannelBase networkChannel, object userData)
        {
            if (m_NetworkConnectedEventHandler != null)
            {
                lock (m_NetworkConnectedEventHandler)
                {
                    NetworkConnectedEventArgs networkConnectedEventArgs = NetworkConnectedEventArgs.Create(networkChannel, userData);
                    m_NetworkConnectedEventHandler(this, networkConnectedEventArgs);
                    ReferencePool.Release(networkConnectedEventArgs);
                }
            }
        }

        private void OnNetworkChannelClosed(NetworkChannelBase networkChannel)
        {
            if (m_NetworkClosedEventHandler != null)
            {
                lock (m_NetworkClosedEventHandler)
                {
                    NetworkClosedEventArgs networkClosedEventArgs = NetworkClosedEventArgs.Create(networkChannel);
                    m_NetworkClosedEventHandler(this, networkClosedEventArgs);
                    ReferencePool.Release(networkClosedEventArgs);
                }
            }
        }

        private void OnNetworkChannelMissHeartBeat(NetworkChannelBase networkChannel, int missHeartBeatCount)
        {
            if (m_NetworkMissHeartBeatEventHandler != null)
            {
                lock (m_NetworkMissHeartBeatEventHandler)
                {
                    NetworkMissHeartBeatEventArgs networkMissHeartBeatEventArgs = NetworkMissHeartBeatEventArgs.Create(networkChannel, missHeartBeatCount);
                    m_NetworkMissHeartBeatEventHandler(this, networkMissHeartBeatEventArgs);
                    ReferencePool.Release(networkMissHeartBeatEventArgs);
                }
            }
        }

        private void OnNetworkChannelError(NetworkChannelBase networkChannel, NetworkErrorCode errorCode, SocketError socketErrorCode, string errorMessage)
        {
            if (m_NetworkErrorEventHandler != null)
            {
                lock (m_NetworkErrorEventHandler)
                {
                    NetworkErrorEventArgs networkErrorEventArgs = NetworkErrorEventArgs.Create(networkChannel, errorCode, socketErrorCode, errorMessage);
                    m_NetworkErrorEventHandler(this, networkErrorEventArgs);
                    ReferencePool.Release(networkErrorEventArgs);
                }
            }
        }

        private void OnNetworkChannelCustomError(NetworkChannelBase networkChannel, object customErrorData)
        {
            if (m_NetworkCustomErrorEventHandler != null)
            {
                lock (m_NetworkCustomErrorEventHandler)
                {
                    NetworkCustomErrorEventArgs networkCustomErrorEventArgs = NetworkCustomErrorEventArgs.Create(networkChannel, customErrorData);
                    m_NetworkCustomErrorEventHandler(this, networkCustomErrorEventArgs);
                    ReferencePool.Release(networkCustomErrorEventArgs);
                }
            }
        }
    }
}
