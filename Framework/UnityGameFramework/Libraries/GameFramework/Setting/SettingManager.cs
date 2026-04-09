//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Setting
{
    /// <summary>
    /// 娓告垙閰嶇疆绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed class SettingManager : GameFrameworkModule, ISettingManager
    {
        private ISettingHelper m_SettingHelper;

        /// <summary>
        /// 鍒濆鍖栨父鎴忛厤缃鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public SettingManager()
        {
            m_SettingHelper = null;
        }

        /// <summary>
        /// 鑾峰彇娓告垙閰嶇疆椤规暟閲忋€?
        /// </summary>
        public int Count
        {
            get
            {
                if (m_SettingHelper == null)
                {
                    throw new GameFrameworkException("Setting helper is invalid.");
                }

                return m_SettingHelper.Count;
            }
        }

        /// <summary>
        /// 娓告垙閰嶇疆绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘父鎴忛厤缃鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            Save();
        }

        /// <summary>
        /// 璁剧疆娓告垙閰嶇疆杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="settingHelper">娓告垙閰嶇疆杈呭姪鍣ㄣ€?/param>
        public void SetSettingHelper(ISettingHelper settingHelper)
        {
            if (settingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            m_SettingHelper = settingHelper;
        }

        /// <summary>
        /// 鍔犺浇娓告垙閰嶇疆銆?
        /// </summary>
        /// <returns>鏄惁鍔犺浇娓告垙閰嶇疆鎴愬姛銆?/returns>
        public bool Load()
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            return m_SettingHelper.Load();
        }

        /// <summary>
        /// 淇濆瓨娓告垙閰嶇疆銆?
        /// </summary>
        /// <returns>鏄惁淇濆瓨娓告垙閰嶇疆鎴愬姛銆?/returns>
        public bool Save()
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            return m_SettingHelper.Save();
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋父鎴忛厤缃」鐨勫悕绉般€?
        /// </summary>
        /// <returns>鎵€鏈夋父鎴忛厤缃」鐨勫悕绉般€?/returns>
        public string[] GetAllSettingNames()
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            return m_SettingHelper.GetAllSettingNames();
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋父鎴忛厤缃」鐨勫悕绉般€?
        /// </summary>
        /// <param name="results">鎵€鏈夋父鎴忛厤缃」鐨勫悕绉般€?/param>
        public void GetAllSettingNames(List<string> results)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            m_SettingHelper.GetAllSettingNames(results);
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ寚瀹氭父鎴忛厤缃」銆?
        /// </summary>
        /// <param name="settingName">瑕佹鏌ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>鎸囧畾鐨勬父鎴忛厤缃」鏄惁瀛樺湪銆?/returns>
        public bool HasSetting(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.HasSetting(settingName);
        }

        /// <summary>
        /// 绉婚櫎鎸囧畾娓告垙閰嶇疆椤广€?
        /// </summary>
        /// <param name="settingName">瑕佺Щ闄ゆ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>鏄惁绉婚櫎鎸囧畾娓告垙閰嶇疆椤规垚鍔熴€?/returns>
        public bool RemoveSetting(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.RemoveSetting(settingName);
        }

        /// <summary>
        /// 娓呯┖鎵€鏈夋父鎴忛厤缃」銆?
        /// </summary>
        public void RemoveAllSettings()
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            m_SettingHelper.RemoveAllSettings();
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧竷灏斿€笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>璇诲彇鐨勫竷灏斿€笺€?/returns>
        public bool GetBool(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetBool(settingName);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧竷灏斿€笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑娓告垙閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勫竷灏斿€笺€?/returns>
        public bool GetBool(string settingName, bool defaultValue)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetBool(settingName, defaultValue);
        }

        /// <summary>
        /// 鍚戞寚瀹氭父鎴忛厤缃」鍐欏叆甯冨皵鍊笺€?
        /// </summary>
        /// <param name="settingName">瑕佸啓鍏ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="value">瑕佸啓鍏ョ殑甯冨皵鍊笺€?/param>
        public void SetBool(string settingName, bool value)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            m_SettingHelper.SetBool(settingName, value);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栨暣鏁板€笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>璇诲彇鐨勬暣鏁板€笺€?/returns>
        public int GetInt(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetInt(settingName);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栨暣鏁板€笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑娓告垙閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勬暣鏁板€笺€?/returns>
        public int GetInt(string settingName, int defaultValue)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetInt(settingName, defaultValue);
        }

        /// <summary>
        /// 鍚戞寚瀹氭父鎴忛厤缃」鍐欏叆鏁存暟鍊笺€?
        /// </summary>
        /// <param name="settingName">瑕佸啓鍏ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="value">瑕佸啓鍏ョ殑鏁存暟鍊笺€?/param>
        public void SetInt(string settingName, int value)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            m_SettingHelper.SetInt(settingName, value);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栨诞鐐规暟鍊笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>璇诲彇鐨勬诞鐐规暟鍊笺€?/returns>
        public float GetFloat(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetFloat(settingName);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栨诞鐐规暟鍊笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑娓告垙閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勬诞鐐规暟鍊笺€?/returns>
        public float GetFloat(string settingName, float defaultValue)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetFloat(settingName, defaultValue);
        }

        /// <summary>
        /// 鍚戞寚瀹氭父鎴忛厤缃」鍐欏叆娴偣鏁板€笺€?
        /// </summary>
        /// <param name="settingName">瑕佸啓鍏ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="value">瑕佸啓鍏ョ殑娴偣鏁板€笺€?/param>
        public void SetFloat(string settingName, float value)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            m_SettingHelper.SetFloat(settingName, value);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧瓧绗︿覆鍊笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>璇诲彇鐨勫瓧绗︿覆鍊笺€?/returns>
        public string GetString(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetString(settingName);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧瓧绗︿覆鍊笺€?
        /// </summary>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="defaultValue">褰撴寚瀹氱殑娓告垙閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ€笺€?/param>
        /// <returns>璇诲彇鐨勫瓧绗︿覆鍊笺€?/returns>
        public string GetString(string settingName, string defaultValue)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetString(settingName, defaultValue);
        }

        /// <summary>
        /// 鍚戞寚瀹氭父鎴忛厤缃」鍐欏叆瀛楃涓插€笺€?
        /// </summary>
        /// <param name="settingName">瑕佸啓鍏ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="value">瑕佸啓鍏ョ殑瀛楃涓插€笺€?/param>
        public void SetString(string settingName, string value)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            m_SettingHelper.SetString(settingName, value);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧璞°€?
        /// </summary>
        /// <typeparam name="T">瑕佽鍙栧璞＄殑绫诲瀷銆?/typeparam>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>璇诲彇鐨勫璞°€?/returns>
        public T GetObject<T>(string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetObject<T>(settingName);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧璞°€?
        /// </summary>
        /// <param name="objectType">瑕佽鍙栧璞＄殑绫诲瀷銆?/param>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <returns>璇诲彇鐨勫璞°€?/returns>
        public object GetObject(Type objectType, string settingName)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetObject(objectType, settingName);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧璞°€?
        /// </summary>
        /// <typeparam name="T">瑕佽鍙栧璞＄殑绫诲瀷銆?/typeparam>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="defaultObj">褰撴寚瀹氱殑娓告垙閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ璞°€?/param>
        /// <returns>璇诲彇鐨勫璞°€?/returns>
        public T GetObject<T>(string settingName, T defaultObj)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetObject(settingName, defaultObj);
        }

        /// <summary>
        /// 浠庢寚瀹氭父鎴忛厤缃」涓鍙栧璞°€?
        /// </summary>
        /// <param name="objectType">瑕佽鍙栧璞＄殑绫诲瀷銆?/param>
        /// <param name="settingName">瑕佽幏鍙栨父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="defaultObj">褰撴寚瀹氱殑娓告垙閰嶇疆椤逛笉瀛樺湪鏃讹紝杩斿洖姝ら粯璁ゅ璞°€?/param>
        /// <returns>璇诲彇鐨勫璞°€?/returns>
        public object GetObject(Type objectType, string settingName, object defaultObj)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (objectType == null)
            {
                throw new GameFrameworkException("Object type is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            return m_SettingHelper.GetObject(objectType, settingName, defaultObj);
        }

        /// <summary>
        /// 鍚戞寚瀹氭父鎴忛厤缃」鍐欏叆瀵硅薄銆?
        /// </summary>
        /// <typeparam name="T">瑕佸啓鍏ュ璞＄殑绫诲瀷銆?/typeparam>
        /// <param name="settingName">瑕佸啓鍏ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="obj">瑕佸啓鍏ョ殑瀵硅薄銆?/param>
        public void SetObject<T>(string settingName, T obj)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            m_SettingHelper.SetObject(settingName, obj);
        }

        /// <summary>
        /// 鍚戞寚瀹氭父鎴忛厤缃」鍐欏叆瀵硅薄銆?
        /// </summary>
        /// <param name="settingName">瑕佸啓鍏ユ父鎴忛厤缃」鐨勫悕绉般€?/param>
        /// <param name="obj">瑕佸啓鍏ョ殑瀵硅薄銆?/param>
        public void SetObject(string settingName, object obj)
        {
            if (m_SettingHelper == null)
            {
                throw new GameFrameworkException("Setting helper is invalid.");
            }

            if (string.IsNullOrEmpty(settingName))
            {
                throw new GameFrameworkException("Setting name is invalid.");
            }

            m_SettingHelper.SetObject(settingName, obj);
        }
    }
}
