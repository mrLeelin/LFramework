//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Config
{
    /// <summary>
    /// 鍏ㄥ眬閰嶇疆绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class ConfigManager : GameFrameworkModule, IConfigManager
    {
        private readonly Dictionary<string, ConfigData> m_ConfigDatas;
        private readonly DataProvider<IConfigManager> m_DataProvider;
        private IConfigHelper m_ConfigHelper;

        /// <summary>
        /// 鍒濆鍖栧叏灞€閰嶇疆绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public ConfigManager()
        {
            m_ConfigDatas = new Dictionary<string, ConfigData>(StringComparer.Ordinal);
            m_DataProvider = new DataProvider<IConfigManager>(this);
            m_ConfigHelper = null;
        }

        /// <summary>
        /// 鑾峰彇鍏ㄥ眬閰嶇疆椤规暟閲忋€?
        /// </summary>
        public int Count
        {
            get
            {
                return m_ConfigDatas.Count;
            }
        }

        /// <summary>
        /// 鑾峰彇缂撳啿浜岃繘鍒舵祦鐨勫ぇ灏忋€?
        /// </summary>
        public int CachedBytesSize
        {
            get
            {
                return DataProvider<IConfigManager>.CachedBytesSize;
            }
        }

        /// <summary>
        /// 璇诲彇鍏ㄥ眬閰嶇疆鎴愬姛浜嬩欢銆?
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
        /// 璇诲彇鍏ㄥ眬閰嶇疆澶辫触浜嬩欢銆?
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
        /// 璇诲彇鍏ㄥ眬閰嶇疆鏇存柊浜嬩欢銆?
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
        /// 璇诲彇鍏ㄥ眬閰嶇疆鏃跺姞杞戒緷璧栬祫婧愪簨浠躲€?
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
        /// 鍏ㄥ眬閰嶇疆绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗗叏灞€閰嶇疆绠＄悊鍣ㄣ€?
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
        /// 璁剧疆鍏ㄥ眬閰嶇疆鏁版嵁鎻愪緵鑰呰緟鍔╁櫒銆?
        /// </summary>
        /// <param name="dataProviderHelper">鍏ㄥ眬閰嶇疆鏁版嵁鎻愪緵鑰呰緟鍔╁櫒銆?/param>
        public void SetDataProviderHelper(IDataProviderHelper<IConfigManager> dataProviderHelper)
        {
            m_DataProvider.SetDataProviderHelper(dataProviderHelper);
        }

        /// <summary>
        /// 璁剧疆鍏ㄥ眬閰嶇疆杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="configHelper">鍏ㄥ眬閰嶇疆杈呭姪鍣ㄣ€?/param>
        public void SetConfigHelper(IConfigHelper configHelper)
        {
            if (configHelper == null)
            {
                throw new GameFrameworkException("Config helper is invalid.");
            }

            m_ConfigHelper = configHelper;
        }

        /// <summary>
        /// 纭繚浜岃繘鍒舵祦缂撳瓨鍒嗛厤瓒冲澶у皬鐨勫唴瀛樺苟缂撳瓨銆?
        /// </summary>
        /// <param name="ensureSize">瑕佺‘淇濅簩杩涘埗娴佺紦瀛樺垎閰嶅唴瀛樼殑澶у皬銆?/param>
        public void EnsureCachedBytesSize(int ensureSize)
        {
            DataProvider<IConfigManager>.EnsureCachedBytesSize(ensureSize);
        }

        /// <summary>
        /// 閲婃斁缂撳瓨鐨勪簩杩涘埗娴併€?
        /// </summary>
        public void FreeCachedBytes()
        {
            DataProvider<IConfigManager>.FreeCachedBytes();
        }

        /// <summary>
        /// 璇诲彇鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configAssetName">鍏ㄥ眬閰嶇疆璧勬簮鍚嶇О銆?/param>
        public void ReadData(string configAssetName)
        {
            m_DataProvider.ReadData(configAssetName);
        }

        /// <summary>
        /// 璇诲彇鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configAssetName">鍏ㄥ眬閰嶇疆璧勬簮鍚嶇О銆?/param>
        /// <param name="priority">鍔犺浇鍏ㄥ眬閰嶇疆璧勬簮鐨勪紭鍏堢骇銆?/param>
        public void ReadData(string configAssetName, int priority)
        {
            m_DataProvider.ReadData(configAssetName, priority);
        }

        /// <summary>
        /// 璇诲彇鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configAssetName">鍏ㄥ眬閰嶇疆璧勬簮鍚嶇О銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void ReadData(string configAssetName, object userData)
        {
            m_DataProvider.ReadData(configAssetName, userData);
        }

        /// <summary>
        /// 璇诲彇鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configAssetName">鍏ㄥ眬閰嶇疆璧勬簮鍚嶇О銆?/param>
        /// <param name="priority">鍔犺浇鍏ㄥ眬閰嶇疆璧勬簮鐨勪紭鍏堢骇銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        public void ReadData(string configAssetName, int priority, object userData)
        {
            m_DataProvider.ReadData(configAssetName, priority, userData);
        }

        /// <summary>
        /// 瑙ｆ瀽鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configString">瑕佽В鏋愮殑鍏ㄥ眬閰嶇疆瀛楃涓层€?/param>
        /// <returns>鏄惁瑙ｆ瀽鍏ㄥ眬閰嶇疆鎴愬姛銆?/returns>
        public bool ParseData(string configString)
        {
            return m_DataProvider.ParseData(configString);
        }

        /// <summary>
        /// 瑙ｆ瀽鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configString">瑕佽В鏋愮殑鍏ㄥ眬閰嶇疆瀛楃涓层€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏄惁瑙ｆ瀽鍏ㄥ眬閰嶇疆鎴愬姛銆?/returns>
        public bool ParseData(string configString, object userData)
        {
            return m_DataProvider.ParseData(configString, userData);
        }

        /// <summary>
        /// 瑙ｆ瀽鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configBytes">瑕佽В鏋愮殑鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦銆?/param>
        /// <returns>鏄惁瑙ｆ瀽鍏ㄥ眬閰嶇疆鎴愬姛銆?/returns>
        public bool ParseData(byte[] configBytes)
        {
            return m_DataProvider.ParseData(configBytes);
        }

        /// <summary>
        /// 瑙ｆ瀽鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configBytes">瑕佽В鏋愮殑鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦銆?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏄惁瑙ｆ瀽鍏ㄥ眬閰嶇疆鎴愬姛銆?/returns>
        public bool ParseData(byte[] configBytes, object userData)
        {
            return m_DataProvider.ParseData(configBytes, userData);
        }

        /// <summary>
        /// 瑙ｆ瀽鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configBytes">瑕佽В鏋愮殑鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦銆?/param>
        /// <param name="startIndex">鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦鐨勮捣濮嬩綅缃€?/param>
        /// <param name="length">鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦鐨勯暱搴︺€?/param>
        /// <returns>鏄惁瑙ｆ瀽鍏ㄥ眬閰嶇疆鎴愬姛銆?/returns>
        public bool ParseData(byte[] configBytes, int startIndex, int length)
        {
            return m_DataProvider.ParseData(configBytes, startIndex, length);
        }

        /// <summary>
        /// 瑙ｆ瀽鍏ㄥ眬閰嶇疆銆?
        /// </summary>
        /// <param name="configBytes">瑕佽В鏋愮殑鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦銆?/param>
        /// <param name="startIndex">鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦鐨勮捣濮嬩綅缃€?/param>
        /// <param name="length">鍏ㄥ眬閰嶇疆浜岃繘鍒舵祦鐨勯暱搴︺€?/param>
        /// <param name="userData">鐢ㄦ埛鑷畾涔夋暟鎹€?/param>
        /// <returns>鏄惁瑙ｆ瀽鍏ㄥ眬閰嶇疆鎴愬姛銆?/returns>
        public bool ParseData(byte[] configBytes, int startIndex, int length, object userData)
        {
            return m_DataProvider.ParseData(configBytes, startIndex, length, userData);
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ寚瀹氬叏灞€閰嶇疆椤广€?
        /// </summary>
        /// <param name="configName">瑕佹鏌ュ叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <returns>鎸囧畾鐨勫叏灞€閰嶇疆椤规槸鍚﹀瓨鍦ㄣ€?/returns>
        public bool HasConfig(string configName)
        {
            return GetConfigData(configName).HasValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇甯冨皵鍊笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <returns>璇诲彇鐨勫竷灏斿€笺€?/returns>
        public bool GetBool(string configName)
        {
            ConfigData? configData = GetConfigData(configName);
            if (!configData.HasValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("Config name '{0}' is not exist.", configName));
            }

            return configData.Value.BoolValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇甯冨皵鍊笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑鍏ㄥ眬閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勫竷灏斿€笺€?/returns>
        public bool GetBool(string configName, bool defaultValue)
        {
            ConfigData? configData = GetConfigData(configName);
            return configData.HasValue ? configData.Value.BoolValue : defaultValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇鏁存暟鍊笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <returns>璇诲彇鐨勬暣鏁板€笺€?/returns>
        public int GetInt(string configName)
        {
            ConfigData? configData = GetConfigData(configName);
            if (!configData.HasValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("Config name '{0}' is not exist.", configName));
            }

            return configData.Value.IntValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇鏁存暟鍊笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑鍏ㄥ眬閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勬暣鏁板€笺€?/returns>
        public int GetInt(string configName, int defaultValue)
        {
            ConfigData? configData = GetConfigData(configName);
            return configData.HasValue ? configData.Value.IntValue : defaultValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇娴偣鏁板€笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <returns>璇诲彇鐨勬诞鐐规暟鍊笺€?/returns>
        public float GetFloat(string configName)
        {
            ConfigData? configData = GetConfigData(configName);
            if (!configData.HasValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("Config name '{0}' is not exist.", configName));
            }

            return configData.Value.FloatValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇娴偣鏁板€笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑鍏ㄥ眬閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勬诞鐐规暟鍊笺€?/returns>
        public float GetFloat(string configName, float defaultValue)
        {
            ConfigData? configData = GetConfigData(configName);
            return configData.HasValue ? configData.Value.FloatValue : defaultValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇瀛楃涓插€笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <returns>璇诲彇鐨勫瓧绗︿覆鍊笺€?/returns>
        public string GetString(string configName)
        {
            ConfigData? configData = GetConfigData(configName);
            if (!configData.HasValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("Config name '{0}' is not exist.", configName));
            }

            return configData.Value.StringValue;
        }

        /// <summary>
        /// 浠庢寚瀹氬叏灞€閰嶇疆椤逛腑璇诲彇瀛楃涓插€笺€?
        /// </summary>
        /// <param name="configName">瑕佽幏鍙栧叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑鍏ㄥ眬閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勫瓧绗︿覆鍊笺€?/returns>
        public string GetString(string configName, string defaultValue)
        {
            ConfigData? configData = GetConfigData(configName);
            return configData.HasValue ? configData.Value.StringValue : defaultValue;
        }

        /// <summary>
        /// 澧炲姞鎸囧畾鍏ㄥ眬閰嶇疆椤广€?
        /// </summary>
        /// <param name="configName">瑕佸鍔犲叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <param name="configValue">鍏ㄥ眬閰嶇疆椤圭殑鍊笺€?/param>
        /// <returns>鏄惁澧炲姞鍏ㄥ眬閰嶇疆椤规垚鍔熴€?/returns>
        public bool AddConfig(string configName, string configValue)
        {
            bool boolValue = false;
            bool.TryParse(configValue, out boolValue);

            int intValue = 0;
            int.TryParse(configValue, out intValue);

            float floatValue = 0f;
            float.TryParse(configValue, out floatValue);

            return AddConfig(configName, boolValue, intValue, floatValue, configValue);
        }

        /// <summary>
        /// 澧炲姞鎸囧畾鍏ㄥ眬閰嶇疆椤广€?
        /// </summary>
        /// <param name="configName">瑕佸鍔犲叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        /// <param name="boolValue">鍏ㄥ眬閰嶇疆椤瑰竷灏斿€笺€?/param>
        /// <param name="intValue">鍏ㄥ眬閰嶇疆椤规暣鏁板€笺€?/param>
        /// <param name="floatValue">鍏ㄥ眬閰嶇疆椤规诞鐐规暟鍊笺€?/param>
        /// <param name="stringValue">鍏ㄥ眬閰嶇疆椤瑰瓧绗︿覆鍊笺€?/param>
        /// <returns>鏄惁澧炲姞鍏ㄥ眬閰嶇疆椤规垚鍔熴€?/returns>
        public bool AddConfig(string configName, bool boolValue, int intValue, float floatValue, string stringValue)
        {
            if (HasConfig(configName))
            {
                return false;
            }

            m_ConfigDatas.Add(configName, new ConfigData(boolValue, intValue, floatValue, stringValue));
            return true;
        }

        /// <summary>
        /// 绉婚櫎鎸囧畾鍏ㄥ眬閰嶇疆椤广€?
        /// </summary>
        /// <param name="configName">瑕佺Щ闄ゅ叏灞€閰嶇疆椤圭殑鍚嶇О銆?/param>
        public bool RemoveConfig(string configName)
        {
            if (!HasConfig(configName))
            {
                return false;
            }

            return m_ConfigDatas.Remove(configName);
        }

        /// <summary>
        /// 娓呯┖鎵€鏈夊叏灞€閰嶇疆椤广€?
        /// </summary>
        public void RemoveAllConfigs()
        {
            m_ConfigDatas.Clear();
        }

        private ConfigData? GetConfigData(string configName)
        {
            if (string.IsNullOrEmpty(configName))
            {
                throw new GameFrameworkException("Config name is invalid.");
            }

            ConfigData configData = default(ConfigData);
            if (m_ConfigDatas.TryGetValue(configName, out configData))
            {
                return configData;
            }

            return null;
        }
    }
}
