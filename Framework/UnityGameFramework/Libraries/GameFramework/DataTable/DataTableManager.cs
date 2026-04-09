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

namespace GameFramework.DataTable
{
    /// <summary>
    /// 鏁版嵁琛ㄧ鐞嗗櫒銆?
    /// </summary>
    [Preserve]
    internal sealed partial class DataTableManager : GameFrameworkModule, IDataTableManager
    {
        private readonly Dictionary<TypeNamePair, DataTableBase> m_DataTables;
        private IResourceManager m_ResourceManager;
        private IDataProviderHelper<DataTableBase> m_DataProviderHelper;
        private IDataTableHelper m_DataTableHelper;

        /// <summary>
        /// 鍒濆鍖栨暟鎹〃绠＄悊鍣ㄧ殑鏂板疄渚嬨€?
        /// </summary>
        public DataTableManager()
        {
            m_DataTables = new Dictionary<TypeNamePair, DataTableBase>();
            m_ResourceManager = null;
            m_DataProviderHelper = null;
            m_DataTableHelper = null;
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁琛ㄦ暟閲忋€?
        /// </summary>
        public int Count
        {
            get
            {
                return m_DataTables.Count;
            }
        }

        /// <summary>
        /// 鑾峰彇缂撳啿浜岃繘鍒舵祦鐨勫ぇ灏忋€?
        /// </summary>
        public int CachedBytesSize
        {
            get
            {
                return DataProvider<DataTableBase>.CachedBytesSize;
            }
        }

        /// <summary>
        /// 鏁版嵁琛ㄧ鐞嗗櫒杞銆?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘暟鎹〃绠＄悊鍣ㄣ€?
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<TypeNamePair, DataTableBase> dataTable in m_DataTables)
            {
                dataTable.Value.Shutdown();
            }

            m_DataTables.Clear();
        }

        /// <summary>
        /// 璁剧疆璧勬簮绠＄悊鍣ㄣ€?
        /// </summary>
        /// <param name="resourceManager">璧勬簮绠＄悊鍣ㄣ€?/param>
        public void SetResourceManager(IResourceManager resourceManager)
        {
            if (resourceManager == null)
            {
                throw new GameFrameworkException("Resource manager is invalid.");
            }

            m_ResourceManager = resourceManager;
        }

        /// <summary>
        /// 璁剧疆鏁版嵁琛ㄦ暟鎹彁渚涜€呰緟鍔╁櫒銆?
        /// </summary>
        /// <param name="dataProviderHelper">鏁版嵁琛ㄦ暟鎹彁渚涜€呰緟鍔╁櫒銆?/param>
        public void SetDataProviderHelper(IDataProviderHelper<DataTableBase> dataProviderHelper)
        {
            if (dataProviderHelper == null)
            {
                throw new GameFrameworkException("Data provider helper is invalid.");
            }

            m_DataProviderHelper = dataProviderHelper;
        }

        /// <summary>
        /// 璁剧疆鏁版嵁琛ㄨ緟鍔╁櫒銆?
        /// </summary>
        /// <param name="dataTableHelper">鏁版嵁琛ㄨ緟鍔╁櫒銆?/param>
        public void SetDataTableHelper(IDataTableHelper dataTableHelper)
        {
            if (dataTableHelper == null)
            {
                throw new GameFrameworkException("Data table helper is invalid.");
            }

            m_DataTableHelper = dataTableHelper;
        }

        /// <summary>
        /// 纭繚浜岃繘鍒舵祦缂撳瓨鍒嗛厤瓒冲澶у皬鐨勫唴瀛樺苟缂撳瓨銆?
        /// </summary>
        /// <param name="ensureSize">瑕佺‘淇濅簩杩涘埗娴佺紦瀛樺垎閰嶅唴瀛樼殑澶у皬銆?/param>
        public void EnsureCachedBytesSize(int ensureSize)
        {
            DataProvider<DataTableBase>.EnsureCachedBytesSize(ensureSize);
        }

        /// <summary>
        /// 閲婃斁缂撳瓨鐨勪簩杩涘埗娴併€?
        /// </summary>
        public void FreeCachedBytes()
        {
            DataProvider<DataTableBase>.FreeCachedBytes();
        }

        /// <summary>
        /// 鏄惁瀛樺湪鏁版嵁琛ㄣ€?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <returns>鏄惁瀛樺湪鏁版嵁琛ㄣ€?/returns>
        public bool HasDataTable<T>() where T : IDataRow
        {
            return InternalHasDataTable(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 鏄惁瀛樺湪鏁版嵁琛ㄣ€?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <returns>鏄惁瀛樺湪鏁版嵁琛ㄣ€?/returns>
        public bool HasDataTable(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalHasDataTable(new TypeNamePair(dataRowType));
        }

        /// <summary>
        /// 鏄惁瀛樺湪鏁版嵁琛ㄣ€?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>鏄惁瀛樺湪鏁版嵁琛ㄣ€?/returns>
        public bool HasDataTable<T>(string name) where T : IDataRow
        {
            return InternalHasDataTable(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 鏄惁瀛樺湪鏁版嵁琛ㄣ€?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>鏄惁瀛樺湪鏁版嵁琛ㄣ€?/returns>
        public bool HasDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalHasDataTable(new TypeNamePair(dataRowType, name));
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁琛ㄣ€?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <returns>瑕佽幏鍙栫殑鏁版嵁琛ㄣ€?/returns>
        public IDataTable<T> GetDataTable<T>() where T : IDataRow
        {
            return (IDataTable<T>)InternalGetDataTable(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁琛ㄣ€?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <returns>瑕佽幏鍙栫殑鏁版嵁琛ㄣ€?/returns>
        public DataTableBase GetDataTable(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalGetDataTable(new TypeNamePair(dataRowType));
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁琛ㄣ€?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>瑕佽幏鍙栫殑鏁版嵁琛ㄣ€?/returns>
        public IDataTable<T> GetDataTable<T>(string name) where T : IDataRow
        {
            return (IDataTable<T>)InternalGetDataTable(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 鑾峰彇鏁版嵁琛ㄣ€?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>瑕佽幏鍙栫殑鏁版嵁琛ㄣ€?/returns>
        public DataTableBase GetDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalGetDataTable(new TypeNamePair(dataRowType, name));
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋暟鎹〃銆?
        /// </summary>
        /// <returns>鎵€鏈夋暟鎹〃銆?/returns>
        public DataTableBase[] GetAllDataTables()
        {
            int index = 0;
            DataTableBase[] results = new DataTableBase[m_DataTables.Count];
            foreach (KeyValuePair<TypeNamePair, DataTableBase> dataTable in m_DataTables)
            {
                results[index++] = dataTable.Value;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋暟鎹〃銆?
        /// </summary>
        /// <param name="results">鎵€鏈夋暟鎹〃銆?/param>
        public void GetAllDataTables(List<DataTableBase> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeNamePair, DataTableBase> dataTable in m_DataTables)
            {
                results.Add(dataTable.Value);
            }
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁琛ㄣ€?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <returns>瑕佸垱寤虹殑鏁版嵁琛ㄣ€?/returns>
        public IDataTable<T> CreateDataTable<T>() where T : class, IDataRow, new()
        {
            return CreateDataTable<T>(string.Empty);
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁琛ㄣ€?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <returns>瑕佸垱寤虹殑鏁版嵁琛ㄣ€?/returns>
        public DataTableBase CreateDataTable(Type dataRowType)
        {
            return CreateDataTable(dataRowType, string.Empty);
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁琛ㄣ€?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>瑕佸垱寤虹殑鏁版嵁琛ㄣ€?/returns>
        public IDataTable<T> CreateDataTable<T>(string name) where T : class, IDataRow, new()
        {
            if (m_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (m_DataProviderHelper == null)
            {
                throw new GameFrameworkException("You must set data provider helper first.");
            }

            TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasDataTable<T>(name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist data table '{0}'.", typeNamePair));
            }

            DataTable<T> dataTable = new DataTable<T>(name);
            dataTable.SetResourceManager(m_ResourceManager);
            dataTable.SetDataProviderHelper(m_DataProviderHelper);
            m_DataTables.Add(typeNamePair, dataTable);
            return dataTable;
        }

        /// <summary>
        /// 鍒涘缓鏁版嵁琛ㄣ€?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>瑕佸垱寤虹殑鏁版嵁琛ㄣ€?/returns>
        public DataTableBase CreateDataTable(Type dataRowType, string name)
        {
            if (m_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (m_DataProviderHelper == null)
            {
                throw new GameFrameworkException("You must set data provider helper first.");
            }

            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            TypeNamePair typeNamePair = new TypeNamePair(dataRowType, name);
            if (HasDataTable(dataRowType, name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist data table '{0}'.", typeNamePair));
            }

            Type dataTableType = typeof(DataTable<>).MakeGenericType(dataRowType);
            DataTableBase dataTable = (DataTableBase)Activator.CreateInstance(dataTableType, name);
            dataTable.SetResourceManager(m_ResourceManager);
            dataTable.SetDataProviderHelper(m_DataProviderHelper);
            m_DataTables.Add(typeNamePair, dataTable);
            return dataTable;
        }

        /// <summary>
        /// 閿€姣佹暟鎹〃銆?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        public bool DestroyDataTable<T>() where T : IDataRow
        {
            return InternalDestroyDataTable(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 閿€姣佹暟鎹〃銆?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <returns>鏄惁閿€姣佹暟鎹〃鎴愬姛銆?/returns>
        public bool DestroyDataTable(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalDestroyDataTable(new TypeNamePair(dataRowType));
        }

        /// <summary>
        /// 閿€姣佹暟鎹〃銆?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        public bool DestroyDataTable<T>(string name) where T : IDataRow
        {
            return InternalDestroyDataTable(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 閿€姣佹暟鎹〃銆?
        /// </summary>
        /// <param name="dataRowType">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/param>
        /// <param name="name">鏁版嵁琛ㄥ悕绉般€?/param>
        /// <returns>鏄惁閿€姣佹暟鎹〃鎴愬姛銆?/returns>
        public bool DestroyDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new GameFrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new GameFrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalDestroyDataTable(new TypeNamePair(dataRowType, name));
        }

        /// <summary>
        /// 閿€姣佹暟鎹〃銆?
        /// </summary>
        /// <typeparam name="T">鏁版嵁琛ㄨ鐨勭被鍨嬨€?/typeparam>
        /// <param name="dataTable">瑕侀攢姣佺殑鏁版嵁琛ㄣ€?/param>
        /// <returns>鏄惁閿€姣佹暟鎹〃鎴愬姛銆?/returns>
        public bool DestroyDataTable<T>(IDataTable<T> dataTable) where T : IDataRow
        {
            if (dataTable == null)
            {
                throw new GameFrameworkException("Data table is invalid.");
            }

            return InternalDestroyDataTable(new TypeNamePair(typeof(T), dataTable.Name));
        }

        /// <summary>
        /// 閿€姣佹暟鎹〃銆?
        /// </summary>
        /// <param name="dataTable">瑕侀攢姣佺殑鏁版嵁琛ㄣ€?/param>
        /// <returns>鏄惁閿€姣佹暟鎹〃鎴愬姛銆?/returns>
        public bool DestroyDataTable(DataTableBase dataTable)
        {
            if (dataTable == null)
            {
                throw new GameFrameworkException("Data table is invalid.");
            }

            return InternalDestroyDataTable(new TypeNamePair(dataTable.Type, dataTable.Name));
        }

        private bool InternalHasDataTable(TypeNamePair typeNamePair)
        {
            return m_DataTables.ContainsKey(typeNamePair);
        }

        private DataTableBase InternalGetDataTable(TypeNamePair typeNamePair)
        {
            DataTableBase dataTable = null;
            if (m_DataTables.TryGetValue(typeNamePair, out dataTable))
            {
                return dataTable;
            }

            return null;
        }

        private bool InternalDestroyDataTable(TypeNamePair typeNamePair)
        {
            DataTableBase dataTable = null;
            if (m_DataTables.TryGetValue(typeNamePair, out dataTable))
            {
                dataTable.Shutdown();
                return m_DataTables.Remove(typeNamePair);
            }

            return false;
        }
    }
}
