//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameFramework.Localization
{
    /// <summary>
    /// 鏈湴鍖栫鐞嗗櫒銆?
    /// </summary>
    [Preserve]
    internal sealed partial class LocalizationManager : GameFrameworkModule, ILocalizationManager
    {
        private readonly Dictionary<string, string> m_Dictionary;
        private readonly DataProvider<ILocalizationManager> m_DataProvider;
        private ILocalizationHelper m_LocalizationHelper;
        private Language m_Language;

        /// <summary>
        /// 鍒濆鍖栨湰鍦板寲绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public LocalizationManager()
        {
            m_Dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            m_DataProvider = new DataProvider<ILocalizationManager>(this);
            m_LocalizationHelper = null;
            m_Language = Language.Unspecified;
        }

        /// <summary>
        /// 鑾峰彇鎴栬缃湰鍦板寲璇█銆?
        /// </summary>
        public Language Language
        {
            get
            {
                return m_Language;
            }
            set
            {
                if (value == Language.Unspecified)
                {
                    throw new GameFrameworkException("Language is invalid.");
                }

                m_Language = value;
            }
        }

        /// <summary>
        /// 鑾峰彇绯荤粺璇█銆?
        /// </summary>
        public Language SystemLanguage
        {
            get
            {
                
                
                
                
                
                if (m_LocalizationHelper == null)
                {
                    throw new GameFrameworkException("You must set localization helper first.");
                }

                
                return m_LocalizationHelper.SystemLanguage;
                
            }
        }

        /// <summary>
        /// 鑾峰彇瀛楀吀鏁伴噺銆?
        /// </summary>
        public int DictionaryCount
        {
            get
            {
                return m_Dictionary.Count;
            }
        }

        /// <summary>
        /// 鑾峰彇缂撳啿浜岃繘鍒舵祦鐨勫ぇ灏忋€?
        /// </summary>
        public int CachedBytesSize
        {
            get
            {
                return DataProvider<ILocalizationManager>.CachedBytesSize;
            }
        }

        /// <summary>
        /// 璇诲彇瀛楀吀鎴愬姛浜嬩欢銆?
        /// </summary>
        public event EventHandler<ReadDataSuccessEventArgs> ReadDataSuccess
        {
            add
            {
                m_DataProvider.ReadDataSuccess += value;
            }
            remove
            {
                m_DataProvider.ReadDataSuccess -= value;
            }
        }

        /// <summary>
        /// 璇诲彇瀛楀吀澶辫触浜嬩欢銆?
        /// </summary>
        public event EventHandler<ReadDataFailureEventArgs> ReadDataFailure
        {
            add
            {
                m_DataProvider.ReadDataFailure += value;
            }
            remove
            {
                m_DataProvider.ReadDataFailure -= value;
            }
        }

        /// <summary>
        /// 璇诲彇瀛楀吀鏇存柊浜嬩欢銆?
        /// </summary>
        public event EventHandler<ReadDataUpdateEventArgs> ReadDataUpdate
        {
            add
            {
                m_DataProvider.ReadDataUpdate += value;
            }
            remove
            {
                m_DataProvider.ReadDataUpdate -= value;
            }
        }

        /// <summary>
        /// 璇诲彇瀛楀吀鏃跺姞杞戒緷璧栬祫婧愪簨浠躲€?
        /// </summary>
        public event EventHandler<ReadDataDependencyAssetEventArgs> ReadDataDependencyAsset
        {
            add
            {
                m_DataProvider.ReadDataDependencyAsset += value;
            }
            remove
            {
                m_DataProvider.ReadDataDependencyAsset -= value;
            }
        }

        /// <summary>
        /// 鏈湴鍖栫鐞嗗櫒杞銆?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘湰鍦板寲绠＄悊鍣ㄣ€?
        /// </summary>
        internal override void Shutdown()
        {
        }

        /// <summary>
        /// 璁剧疆璧勬簮绠＄悊鍣ㄣ€?
        /// </summary>
        /// <param name="resourceManager">璧勬簮绠＄悊鍣ㄣ€?/param>
        public void SetResourceManager(IResourceManager resourceManager)
        {
            m_DataProvider.SetResourceManager(resourceManager);
        }

        /// <summary>
        /// 璁剧疆鏈湴鍖栨暟鎹彁渚涜€呰緟鍔╁櫒銆?
        /// </summary>
        /// <param name="dataProviderHelper">鏈湴鍖栨暟鎹彁渚涜€呰緟鍔╁櫒銆?/param>
        public void SetDataProviderHelper(IDataProviderHelper<ILocalizationManager> dataProviderHelper)
        {
            m_DataProvider.SetDataProviderHelper(dataProviderHelper);
        }

        /// <summary>
        /// 璁剧疆鏈湴鍖栬緟鍔╁櫒銆?
        /// </summary>
        /// <param name="localizationHelper">鏈湴鍖栬緟鍔╁櫒銆?/param>
        public void SetLocalizationHelper(ILocalizationHelper localizationHelper)
        {
            if (localizationHelper == null)
            {
                throw new GameFrameworkException("Localization helper is invalid.");
            }

            m_LocalizationHelper = localizationHelper;
        }

        /// <summary>
        /// 纭繚浜岃繘鍒舵祦缂撳瓨鍒嗛厤瓒冲澶у皬鐨勫唴瀛樺苟缂撳瓨銆?
        /// </summary>
        /// <param name="ensureSize">瑕佺‘淇濅簩杩涘埗娴佺紦瀛樺垎閰嶅唴瀛樼殑澶у皬銆?/param>
        public void EnsureCachedBytesSize(int ensureSize)
        {
            DataProvider<ILocalizationManager>.EnsureCachedBytesSize(ensureSize);
        }

        /// <summary>
        /// 閲婃斁缂撳瓨鐨勪簩杩涘埗娴併€?
        /// </summary>
        public void FreeCachedBytes()
        {
            DataProvider<ILocalizationManager>.FreeCachedBytes();
        }

        /// <summary>
        /// 璇诲彇瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryAssetName">瀛楀吀璧勬簮鍚嶇О銆?/param>
        public void ReadData(string dictionaryAssetName)
        {
            m_DataProvider.ReadData(dictionaryAssetName);
        }

        /// <summary>
        /// 璇诲彇瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryAssetName">瀛楀吀璧勬簮鍚嶇О銆?/param>
        /// <param name="priority">鍔犺浇瀛楀吀璧勬簮鐨勪紭鍏堢骇銆?/param>
        public void ReadData(string dictionaryAssetName, int priority)
        {
            m_DataProvider.ReadData(dictionaryAssetName, priority);
        }

        /// <summary>
        /// 璇诲彇瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryAssetName">瀛楀吀璧勬簮鍚嶇О銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void ReadData(string dictionaryAssetName, object userData)
        {
            m_DataProvider.ReadData(dictionaryAssetName, userData);
        }

        /// <summary>
        /// 璇诲彇瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryAssetName">瀛楀吀璧勬簮鍚嶇О銆?/param>
        /// <param name="priority">鍔犺浇瀛楀吀璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void ReadData(string dictionaryAssetName, int priority, object userData)
        {
            m_DataProvider.ReadData(dictionaryAssetName, priority, userData);
        }

        /// <summary>
        /// 瑙ｆ瀽瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryString">瑕佽В鏋愮殑瀛楀吀瀛楃涓层€?/param>
        /// <returns>鏄惁瑙ｆ瀽瀛楀吀鎴愬姛銆?/returns>
        public bool ParseData(string dictionaryString)
        {
            return m_DataProvider.ParseData(dictionaryString);
        }

        /// <summary>
        /// 瑙ｆ瀽瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryString">瑕佽В鏋愮殑瀛楀吀瀛楃涓层€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏄惁瑙ｆ瀽瀛楀吀鎴愬姛銆?/returns>
        public bool ParseData(string dictionaryString, object userData)
        {
            return m_DataProvider.ParseData(dictionaryString, userData);
        }

        /// <summary>
        /// 瑙ｆ瀽瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryBytes">瑕佽В鏋愮殑瀛楀吀浜岃繘鍒舵祦銆?/param>
        /// <returns>鏄惁瑙ｆ瀽瀛楀吀鎴愬姛銆?/returns>
        public bool ParseData(byte[] dictionaryBytes)
        {
            return m_DataProvider.ParseData(dictionaryBytes);
        }

        /// <summary>
        /// 瑙ｆ瀽瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryBytes">瑕佽В鏋愮殑瀛楀吀浜岃繘鍒舵祦銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏄惁瑙ｆ瀽瀛楀吀鎴愬姛銆?/returns>
        public bool ParseData(byte[] dictionaryBytes, object userData)
        {
            return m_DataProvider.ParseData(dictionaryBytes, userData);
        }

        /// <summary>
        /// 瑙ｆ瀽瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryBytes">瑕佽В鏋愮殑瀛楀吀浜岃繘鍒舵祦銆?/param>
        /// <param name="startIndex">瀛楀吀浜岃繘鍒舵祦鐨勮捣濮嬩綅缃€?/param>
        /// <param name="length">瀛楀吀浜岃繘鍒舵祦鐨勯暱搴︺€?/param>
        /// <returns>鏄惁瑙ｆ瀽瀛楀吀鎴愬姛銆?/returns>
        public bool ParseData(byte[] dictionaryBytes, int startIndex, int length)
        {
            return m_DataProvider.ParseData(dictionaryBytes, startIndex, length);
        }

        /// <summary>
        /// 瑙ｆ瀽瀛楀吀銆?
        /// </summary>
        /// <param name="dictionaryBytes">瑕佽В鏋愮殑瀛楀吀浜岃繘鍒舵祦銆?/param>
        /// <param name="startIndex">瀛楀吀浜岃繘鍒舵祦鐨勮捣濮嬩綅缃€?/param>
        /// <param name="length">瀛楀吀浜岃繘鍒舵祦鐨勯暱搴︺€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏄惁瑙ｆ瀽瀛楀吀鎴愬姛銆?/returns>
        public bool ParseData(byte[] dictionaryBytes, int startIndex, int length, object userData)
        {
            return m_DataProvider.ParseData(dictionaryBytes, startIndex, length, userData);
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString(string key)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            return value;
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T">瀛楀吀鍙傛暟鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg">瀛楀吀鍙傛暟銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T>(string key, T arg)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, arg, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2>(string key, T1 arg1, T2 arg2)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4}", key, value, arg1, arg2, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3>(string key, T1 arg1, T2 arg2, T3 arg3)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5}", key, value, arg1, arg2, arg3, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6}", key, value, arg1, arg2, arg3, arg4, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7}", key, value, arg1, arg2, arg3, arg4, arg5, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T11">瀛楀吀鍙傛暟 11 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <param name="arg11">瀛楀吀鍙傛暟 11銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T11">瀛楀吀鍙傛暟 11 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T12">瀛楀吀鍙傛暟 12 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <param name="arg11">瀛楀吀鍙傛暟 11銆?/param>
        /// <param name="arg12">瀛楀吀鍙傛暟 12銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T11">瀛楀吀鍙傛暟 11 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T12">瀛楀吀鍙傛暟 12 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T13">瀛楀吀鍙傛暟 13 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <param name="arg11">瀛楀吀鍙傛暟 11銆?/param>
        /// <param name="arg12">瀛楀吀鍙傛暟 12銆?/param>
        /// <param name="arg13">瀛楀吀鍙傛暟 13銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
            }
            catch (Exception exception)
            {
                return Utility.Text.Format("<Error>{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", key, value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T11">瀛楀吀鍙傛暟 11 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T12">瀛楀吀鍙傛暟 12 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T13">瀛楀吀鍙傛暟 13 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T14">瀛楀吀鍙傛暟 14 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <param name="arg11">瀛楀吀鍙傛暟 11銆?/param>
        /// <param name="arg12">瀛楀吀鍙傛暟 12銆?/param>
        /// <param name="arg13">瀛楀吀鍙傛暟 13銆?/param>
        /// <param name="arg14">瀛楀吀鍙傛暟 14銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
            }
            catch (Exception exception)
            {
                string args = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, args, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T11">瀛楀吀鍙傛暟 11 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T12">瀛楀吀鍙傛暟 12 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T13">瀛楀吀鍙傛暟 13 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T14">瀛楀吀鍙傛暟 14 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T15">瀛楀吀鍙傛暟 15 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <param name="arg11">瀛楀吀鍙傛暟 11銆?/param>
        /// <param name="arg12">瀛楀吀鍙傛暟 12銆?/param>
        /// <param name="arg13">瀛楀吀鍙傛暟 13銆?/param>
        /// <param name="arg14">瀛楀吀鍙傛暟 14銆?/param>
        /// <param name="arg15">瀛楀吀鍙傛暟 15銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
            }
            catch (Exception exception)
            {
                string args = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}", arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, args, exception);
            }
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍐呭瀛楃涓层€?
        /// </summary>
        /// <typeparam name="T1">瀛楀吀鍙傛暟 1 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T2">瀛楀吀鍙傛暟 2 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T3">瀛楀吀鍙傛暟 3 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T4">瀛楀吀鍙傛暟 4 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T5">瀛楀吀鍙傛暟 5 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T6">瀛楀吀鍙傛暟 6 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T7">瀛楀吀鍙傛暟 7 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T8">瀛楀吀鍙傛暟 8 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T9">瀛楀吀鍙傛暟 9 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T10">瀛楀吀鍙傛暟 10 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T11">瀛楀吀鍙傛暟 11 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T12">瀛楀吀鍙傛暟 12 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T13">瀛楀吀鍙傛暟 13 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T14">瀛楀吀鍙傛暟 14 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T15">瀛楀吀鍙傛暟 15 鐨勭被鍨嬨€?/typeparam>
        /// <typeparam name="T16">瀛楀吀鍙傛暟 16 鐨勭被鍨嬨€?/typeparam>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="arg1">瀛楀吀鍙傛暟 1銆?/param>
        /// <param name="arg2">瀛楀吀鍙傛暟 2銆?/param>
        /// <param name="arg3">瀛楀吀鍙傛暟 3銆?/param>
        /// <param name="arg4">瀛楀吀鍙傛暟 4銆?/param>
        /// <param name="arg5">瀛楀吀鍙傛暟 5銆?/param>
        /// <param name="arg6">瀛楀吀鍙傛暟 6銆?/param>
        /// <param name="arg7">瀛楀吀鍙傛暟 7銆?/param>
        /// <param name="arg8">瀛楀吀鍙傛暟 8銆?/param>
        /// <param name="arg9">瀛楀吀鍙傛暟 9銆?/param>
        /// <param name="arg10">瀛楀吀鍙傛暟 10銆?/param>
        /// <param name="arg11">瀛楀吀鍙傛暟 11銆?/param>
        /// <param name="arg12">瀛楀吀鍙傛暟 12銆?/param>
        /// <param name="arg13">瀛楀吀鍙傛暟 13銆?/param>
        /// <param name="arg14">瀛楀吀鍙傛暟 14銆?/param>
        /// <param name="arg15">瀛楀吀鍙傛暟 15銆?/param>
        /// <param name="arg16">瀛楀吀鍙傛暟 16銆?/param>
        /// <returns>瑕佽幏鍙栫殑瀛楀吀鍐呭瀛楃涓层€?/returns>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16)
        {
            string value = GetRawString(key);
            if (value == null)
            {
                return Utility.Text.Format("<NoKey>{0}", key);
            }

            try
            {
                return Utility.Text.Format(value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
            }
            catch (Exception exception)
            {
                string args = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
                return Utility.Text.Format("<Error>{0},{1},{2},{3}", key, value, args, exception);
            }
        }

        /// <summary>
        /// 鏄惁瀛樺湪瀛楀吀銆?
        /// </summary>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <returns>鏄惁瀛樺湪瀛楀吀銆?/returns>
        public bool HasRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new GameFrameworkException("Key is invalid.");
            }

            return m_Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// 鏍规嵁瀛楀吀涓婚敭鑾峰彇瀛楀吀鍊笺€?
        /// </summary>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <returns>瀛楀吀鍊笺€?/returns>
        public string GetRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new GameFrameworkException("Key is invalid.");
            }

            string value = null;
            if (m_Dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// 澧炲姞瀛楀吀銆?
        /// </summary>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <param name="value">瀛楀吀鍐呭銆?/param>
        /// <returns>鏄惁澧炲姞瀛楀吀鎴愬姛銆?/returns>
        public bool AddRawString(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new GameFrameworkException("Key is invalid.");
            }

            if (m_Dictionary.ContainsKey(key))
            {
                return false;
            }

            m_Dictionary.Add(key, value ?? string.Empty);
            return true;
        }

        /// <summary>
        /// 绉婚櫎瀛楀吀銆?
        /// </summary>
        /// <param name="key">瀛楀吀涓婚敭銆?/param>
        /// <returns>鏄惁绉婚櫎瀛楀吀鎴愬姛銆?/returns>
        public bool RemoveRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new GameFrameworkException("Key is invalid.");
            }

            return m_Dictionary.Remove(key);
        }

        /// <summary>
        /// 娓呯┖鎵€鏈夊瓧鍏搞€?
        /// </summary>
        public void RemoveAllRawStrings()
        {
            m_Dictionary.Clear();
        }
    }
}
