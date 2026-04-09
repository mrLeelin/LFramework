//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using UnityEngine.Scripting;

namespace GameFramework.DataNode
{
    /// <summary>
    /// 鏁版嵁缁撶偣绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed partial class DataNodeManager : GameFrameworkModule, IDataNodeManager
    {
        private static readonly string[] EmptyStringArray = new string[] { };
        private static readonly string[] PathSplitSeparator = new string[] { ".", "/", "\\" };

        private const string RootName = "<Root>";
        private DataNode m_Root;

        /// <summary>
        /// 鍒濆鍖栨暟鎹粨鐐圭鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public DataNodeManager()
        {
            m_Root = DataNode.Create(RootName, null);
        }

        /// <summary>
        /// 鑾峰彇鏍规暟鎹粨鐐广€?
        /// </summary>
        public IDataNode Root
        {
            get
            {
                return m_Root;
            }
        }

        /// <summary>
        /// 鏁版嵁缁撶偣绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘暟鎹粨鐐圭鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            ReferencePool.Release(m_Root);
            m_Root = null;
        }

        /// <summary>
        /// 鏍规嵁绫诲瀷鑾峰彇鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <typeparam name="T">瑕佽幏鍙栫殑鏁版嵁绫诲瀷銆?/typeparam>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <returns>鎸囧畾绫诲瀷鐨勬暟鎹€?/returns>
        public T GetData<T>(string path) where T : Variable
        {
            return GetData<T>(path, null);
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <returns>鏁版嵁缁撶偣鐨勬暟鎹€?/returns>
        public Variable GetData(string path)
        {
            return GetData(path, null);
        }

        /// <summary>
        /// 鏍规嵁绫诲瀷鑾峰彇鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <typeparam name="T">瑕佽幏鍙栫殑鏁版嵁绫诲瀷銆?/typeparam>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        /// <returns>鎸囧畾绫诲瀷鐨勬暟鎹€?/returns>
        public T GetData<T>(string path, IDataNode node) where T : Variable
        {
            IDataNode current = GetNode(path, node);
            if (current == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Data node is not exist, path '{0}', node '{1}'.", path, node != null ? node.FullName : string.Empty));
            }

            return current.GetData<T>();
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        /// <returns>鏁版嵁缁撶偣鐨勬暟鎹€?/returns>
        public Variable GetData(string path, IDataNode node)
        {
            IDataNode current = GetNode(path, node);
            if (current == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Data node is not exist, path '{0}', node '{1}'.", path, node != null ? node.FullName : string.Empty));
            }

            return current.GetData();
        }

        /// <summary>
        /// 璁剧疆鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <typeparam name="T">瑕佽缃殑鏁版嵁绫诲瀷銆?/typeparam>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="data">瑕佽缃殑鏁版嵁銆?/param>
        public void SetData<T>(string path, T data) where T : Variable
        {
            SetData(path, data, null);
        }

        /// <summary>
        /// 璁剧疆鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="data">瑕佽缃殑鏁版嵁銆?/param>
        public void SetData(string path, Variable data)
        {
            SetData(path, data, null);
        }

        /// <summary>
        /// 璁剧疆鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <typeparam name="T">瑕佽缃殑鏁版嵁绫诲瀷銆?/typeparam>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="data">瑕佽缃殑鏁版嵁銆?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        public void SetData<T>(string path, T data, IDataNode node) where T : Variable
        {
            IDataNode current = GetOrAddNode(path, node);
            current.SetData(data);
        }

        /// <summary>
        /// 璁剧疆鏁版嵁缁撶偣鐨勬暟鎹€?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="data">瑕佽缃殑鏁版嵁銆?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        public void SetData(string path, Variable data, IDataNode node)
        {
            IDataNode current = GetOrAddNode(path, node);
            current.SetData(data);
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁缁撶偣銆?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <returns>鎸囧畾浣嶇疆鐨勬暟鎹粨鐐癸紝濡傛灉娌℃湁鎵惧埌锛屽垯杩斿洖绌恒€?/returns>
        public IDataNode GetNode(string path)
        {
            return GetNode(path, null);
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁缁撶偣銆?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        /// <returns>鎸囧畾浣嶇疆鐨勬暟鎹粨鐐癸紝濡傛灉娌℃湁鎵惧埌锛屽垯杩斿洖绌恒€?/returns>
        public IDataNode GetNode(string path, IDataNode node)
        {
            IDataNode current = node ?? m_Root;
            string[] splitedPath = GetSplitedPath(path);
            foreach (string i in splitedPath)
            {
                current = current.GetChild(i);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// 鑾峰彇鎴栧鍔犳暟鎹粨鐐广€?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <returns>鎸囧畾浣嶇疆鐨勬暟鎹粨鐐癸紝濡傛灉娌℃湁鎵惧埌锛屽垯鍒涘缓鐩稿簲鐨勬暟鎹粨鐐广€?/returns>
        public IDataNode GetOrAddNode(string path)
        {
            return GetOrAddNode(path, null);
        }

        /// <summary>
        /// 鑾峰彇鎴栧鍔犳暟鎹粨鐐广€?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        /// <returns>鎸囧畾浣嶇疆鐨勬暟鎹粨鐐癸紝濡傛灉娌℃湁鎵惧埌锛屽垯澧炲姞鐩稿簲鐨勬暟鎹粨鐐广€?/returns>
        public IDataNode GetOrAddNode(string path, IDataNode node)
        {
            IDataNode current = node ?? m_Root;
            string[] splitedPath = GetSplitedPath(path);
            foreach (string i in splitedPath)
            {
                current = current.GetOrAddChild(i);
            }

            return current;
        }

        /// <summary>
        /// 绉婚櫎鏁版嵁缁撶偣銆?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        public void RemoveNode(string path)
        {
            RemoveNode(path, null);
        }

        /// <summary>
        /// 绉婚櫎鏁版嵁缁撶偣銆?
        /// </summary>
        /// <param name="path">鐩稿浜?node 鐨勬煡鎵捐矾寰勩€?/param>
        /// <param name="node">鏌ユ壘璧峰缁撶偣銆?/param>
        public void RemoveNode(string path, IDataNode node)
        {
            IDataNode current = node ?? m_Root;
            IDataNode parent = current.Parent;
            string[] splitedPath = GetSplitedPath(path);
            foreach (string i in splitedPath)
            {
                parent = current;
                current = current.GetChild(i);
                if (current == null)
                {
                    return;
                }
            }

            if (parent != null)
            {
                parent.RemoveChild(current.Name);
            }
        }

        /// <summary>
        /// 绉婚櫎鎵€鏈夋暟鎹粨鐐广€?
        /// </summary>
        public void Clear()
        {
            m_Root.Clear();
        }

        /// <summary>
        /// 鏁版嵁缁撶偣璺緞鍒囧垎宸ュ叿鍑芥暟銆?
        /// </summary>
        /// <param name="path">瑕佸垏鍒嗙殑鏁版嵁缁撶偣璺緞銆?/param>
        /// <returns>鍒囧垎鍚庣殑瀛楃涓叉暟缁勩€?/returns>
        private static string[] GetSplitedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return EmptyStringArray;
            }

            return path.Split(PathSplitSeparator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
