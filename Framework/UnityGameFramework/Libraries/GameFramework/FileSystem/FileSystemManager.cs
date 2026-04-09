//------------------------------------------------------------
// Game Framework
// Copyright 漏 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Scripting;

namespace GameFramework.FileSystem
{
    /// <summary>
    /// 鏂囦欢绯荤粺绠＄悊鍣ㄣ€?
    /// </summary>
    [Preserve]
    internal sealed class FileSystemManager : GameFrameworkModule, IFileSystemManager
    {
        private readonly Dictionary<string, FileSystem> m_FileSystems;

        private IFileSystemHelper m_FileSystemHelper;

        /// <summary>
        /// 鍒濆鍖栨枃浠剁郴缁熺鐞嗗櫒鐨勬柊瀹炰緥銆?
        /// </summary>
        public FileSystemManager()
        {
            m_FileSystems = new Dictionary<string, FileSystem>(StringComparer.Ordinal);
            m_FileSystemHelper = null;
        }

        /// <summary>
        /// 鑾峰彇娓告垙妗嗘灦妯″潡浼樺厛绾с€?
        /// </summary>
        /// <remarks>浼樺厛绾ц緝楂樼殑妯″潡浼氫紭鍏堣疆璇紝骞朵笖鍏抽棴鎿嶄綔浼氬悗杩涜銆?/remarks>
        internal override int Priority
        {
            get
            {
                return 4;
            }
        }

        /// <summary>
        /// 鑾峰彇鏂囦欢绯荤粺鏁伴噺銆?
        /// </summary>
        public int Count
        {
            get
            {
                return m_FileSystems.Count;
            }
        }

        /// <summary>
        /// 鏂囦欢绯荤粺绠＄悊鍣ㄨ疆璇€?
        /// </summary>
        /// <param name="elapseSeconds">閫昏緫娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        /// <param name="realElapseSeconds">鐪熷疄娴侀€濇椂闂达紝浠ョ涓哄崟浣嶃€?/param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗘枃浠剁郴缁熺鐞嗗櫒銆?
        /// </summary>
        internal override void Shutdown()
        {
            while (m_FileSystems.Count > 0)
            {
                foreach (KeyValuePair<string, FileSystem> fileSystem in m_FileSystems)
                {
                    DestroyFileSystem(fileSystem.Value, false);
                    break;
                }
            }
        }

        /// <summary>
        /// 璁剧疆鏂囦欢绯荤粺杈呭姪鍣ㄣ€?
        /// </summary>
        /// <param name="fileSystemHelper">鏂囦欢绯荤粺杈呭姪鍣ㄣ€?/param>
        public void SetFileSystemHelper(IFileSystemHelper fileSystemHelper)
        {
            if (fileSystemHelper == null)
            {
                throw new GameFrameworkException("File system helper is invalid.");
            }

            m_FileSystemHelper = fileSystemHelper;
        }

        /// <summary>
        /// 妫€鏌ユ槸鍚﹀瓨鍦ㄦ枃浠剁郴缁熴€?
        /// </summary>
        /// <param name="fullPath">瑕佹鏌ョ殑鏂囦欢绯荤粺鐨勫畬鏁磋矾寰勩€?/param>
        /// <returns>鏄惁瀛樺湪鏂囦欢绯荤粺銆?/returns>
        public bool HasFileSystem(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new GameFrameworkException("Full path is invalid.");
            }

            return m_FileSystems.ContainsKey(Utility.Path.GetRegularPath(fullPath));
        }

        /// <summary>
        /// 鑾峰彇鏂囦欢绯荤粺銆?
        /// </summary>
        /// <param name="fullPath">瑕佽幏鍙栫殑鏂囦欢绯荤粺鐨勫畬鏁磋矾寰勩€?/param>
        /// <returns>鑾峰彇鐨勬枃浠剁郴缁熴€?/returns>
        public IFileSystem GetFileSystem(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new GameFrameworkException("Full path is invalid.");
            }

            FileSystem fileSystem = null;
            if (m_FileSystems.TryGetValue(Utility.Path.GetRegularPath(fullPath), out fileSystem))
            {
                return fileSystem;
            }

            return null;
        }

        /// <summary>
        /// 鍒涘缓鏂囦欢绯荤粺銆?
        /// </summary>
        /// <param name="fullPath">瑕佸垱寤虹殑鏂囦欢绯荤粺鐨勫畬鏁磋矾寰勩€?/param>
        /// <param name="access">瑕佸垱寤虹殑鏂囦欢绯荤粺鐨勮闂柟寮忋€?/param>
        /// <param name="maxFileCount">瑕佸垱寤虹殑鏂囦欢绯荤粺鐨勬渶澶ф枃浠舵暟閲忋€?/param>
        /// <param name="maxBlockCount">瑕佸垱寤虹殑鏂囦欢绯荤粺鐨勬渶澶у潡鏁版嵁鏁伴噺銆?/param>
        /// <returns>鍒涘缓鐨勬枃浠剁郴缁熴€?/returns>
        public IFileSystem CreateFileSystem(string fullPath, FileSystemAccess access, int maxFileCount, int maxBlockCount)
        {
            if (m_FileSystemHelper == null)
            {
                throw new GameFrameworkException("File system helper is invalid.");
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                throw new GameFrameworkException("Full path is invalid.");
            }

            if (access == FileSystemAccess.Unspecified)
            {
                throw new GameFrameworkException("Access is invalid.");
            }

            if (access == FileSystemAccess.Read)
            {
                throw new GameFrameworkException("Access read is invalid.");
            }

            fullPath = Utility.Path.GetRegularPath(fullPath);
            if (m_FileSystems.ContainsKey(fullPath))
            {
                throw new GameFrameworkException(Utility.Text.Format("File system '{0}' is already exist.", fullPath));
            }

            FileSystemStream fileSystemStream = m_FileSystemHelper.CreateFileSystemStream(fullPath, access, true);
            if (fileSystemStream == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Create file system stream for '{0}' failure.", fullPath));
            }

            FileSystem fileSystem = FileSystem.Create(fullPath, access, fileSystemStream, maxFileCount, maxBlockCount);
            if (fileSystem == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Create file system '{0}' failure.", fullPath));
            }

            m_FileSystems.Add(fullPath, fileSystem);
            return fileSystem;
        }

        /// <summary>
        /// 鍔犺浇鏂囦欢绯荤粺銆?
        /// </summary>
        /// <param name="fullPath">瑕佸姞杞界殑鏂囦欢绯荤粺鐨勫畬鏁磋矾寰勩€?/param>
        /// <param name="access">瑕佸姞杞界殑鏂囦欢绯荤粺鐨勮闂柟寮忋€?/param>
        /// <returns>鍔犺浇鐨勬枃浠剁郴缁熴€?/returns>
        public IFileSystem LoadFileSystem(string fullPath, FileSystemAccess access)
        {
            if (m_FileSystemHelper == null)
            {
                throw new GameFrameworkException("File system helper is invalid.");
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                throw new GameFrameworkException("Full path is invalid.");
            }

            if (access == FileSystemAccess.Unspecified)
            {
                throw new GameFrameworkException("Access is invalid.");
            }

            fullPath = Utility.Path.GetRegularPath(fullPath);
            if (m_FileSystems.ContainsKey(fullPath))
            {
                throw new GameFrameworkException(Utility.Text.Format("File system '{0}' is already exist.", fullPath));
            }

            FileSystemStream fileSystemStream = m_FileSystemHelper.CreateFileSystemStream(fullPath, access, false);
            if (fileSystemStream == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Create file system stream for '{0}' failure.", fullPath));
            }

            FileSystem fileSystem = FileSystem.Load(fullPath, access, fileSystemStream);
            if (fileSystem == null)
            {
                fileSystemStream.Close();
                throw new GameFrameworkException(Utility.Text.Format("Load file system '{0}' failure.", fullPath));
            }

            m_FileSystems.Add(fullPath, fileSystem);
            return fileSystem;
        }

        /// <summary>
        /// 閿€姣佹枃浠剁郴缁熴€?
        /// </summary>
        /// <param name="fileSystem">瑕侀攢姣佺殑鏂囦欢绯荤粺銆?/param>
        /// <param name="deletePhysicalFile">鏄惁鍒犻櫎鏂囦欢绯荤粺瀵瑰簲鐨勭墿鐞嗘枃浠躲€?/param>
        public void DestroyFileSystem(IFileSystem fileSystem, bool deletePhysicalFile)
        {
            if (fileSystem == null)
            {
                throw new GameFrameworkException("File system is invalid.");
            }

            string fullPath = fileSystem.FullPath;
            ((FileSystem)fileSystem).Shutdown();
            m_FileSystems.Remove(fullPath);

            if (deletePhysicalFile && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋枃浠剁郴缁熼泦鍚堛€?
        /// </summary>
        /// <returns>鑾峰彇鐨勬墍鏈夋枃浠剁郴缁熼泦鍚堛€?/returns>
        public IFileSystem[] GetAllFileSystems()
        {
            int index = 0;
            IFileSystem[] results = new IFileSystem[m_FileSystems.Count];
            foreach (KeyValuePair<string, FileSystem> fileSystem in m_FileSystems)
            {
                results[index++] = fileSystem.Value;
            }

            return results;
        }

        /// <summary>
        /// 鑾峰彇鎵€鏈夋枃浠剁郴缁熼泦鍚堛€?
        /// </summary>
        /// <param name="results">鑾峰彇鐨勬墍鏈夋枃浠剁郴缁熼泦鍚堛€?/param>
        public void GetAllFileSystems(List<IFileSystem> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, FileSystem> fileSystem in m_FileSystems)
            {
                results.Add(fileSystem.Value);
            }
        }
    }
}
