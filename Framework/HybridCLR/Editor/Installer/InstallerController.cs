#if UNITY_6000_3_OR_NEWER && UNITY_EDITOR_OSX
#define NEW_IL2CPP_PATH
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Text.RegularExpressions;
using System.Linq;
using HybridCLR.Editor.Settings;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace HybridCLR.Editor.Installer
{

    public class InstallerController
    {
        private const string hybridclr_repo_path = "hybridclr_repo";

        private const string il2cpp_plus_repo_path = "il2cpp_plus_repo";

        private const string SupportedModifiedDllUnityVersion = "6000.3.6f1";

        private const string ModifiedDllVersion = "2022.3.62f1";

        private const string ModifiedDllSha256 = "AA9A3D76EC3649EE907CA22EDFD8661D340972B36A32A69137855B72A84C1911";

        public int MajorVersion => _curVersion.major;

        private readonly UnityVersion _curVersion;

        private readonly HybridclrVersionManifest _versionManifest;
        private readonly HybridclrVersionInfo _curDefaultVersion;

        public string PackageVersion { get; private set; }

        public string InstalledLibil2cppVersion { get; private set; }

        public InstallerController()
        {
            _curVersion = ParseUnityVersion(Application.unityVersion);
            _versionManifest = GetHybridCLRVersionManifest();
            _curDefaultVersion = _versionManifest.versions.FirstOrDefault(v => {
                return _curVersion.isTuanjieEngine? v.unity_version == $"{_curVersion.major}-tuanjie"
#if UNITY_6000_3_OR_NEWER
                    : v.unity_version == "6000.3.x"
#else
                    : v.unity_version == _curVersion.major.ToString()
#endif
                    ;
            });
            PackageVersion = LoadPackageInfo().version;
            InstalledLibil2cppVersion = ReadLocalVersion();
        }

        private HybridclrVersionManifest GetHybridCLRVersionManifest()
        {
            string versionFile = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/hybridclr_version.json";
            return JsonUtility.FromJson<HybridclrVersionManifest>(File.ReadAllText(versionFile, Encoding.UTF8));
        }

        private PackageInfo LoadPackageInfo()
        {
            string packageJson = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/hybridclr_package.json";
            return JsonUtility.FromJson<PackageInfo>(File.ReadAllText(packageJson, Encoding.UTF8));
        }


        [Serializable]
        class PackageInfo
        {
            public string name;

            public string version;
        }

        [Serializable]
        class VersionDesc
        {
            public string branch;

            //public string hash;
        }

        [Serializable]
        class HybridclrVersionInfo
        {
            public string unity_version;

            public VersionDesc hybridclr;

            public VersionDesc il2cpp_plus;
        }

        [Serializable]
        class HybridclrVersionManifest
        {
            public List<HybridclrVersionInfo> versions;
        }

        private class UnityVersion
        {
            public int major;
            public int minor1;
            public int minor2;
            public bool isTuanjieEngine;

            public override string ToString()
            {
                return $"{major}.{minor1}.{minor2}";
            }
        }

        private static readonly Regex s_unityVersionPat = new Regex(@"(\d+)\.(\d+)\.(\d+)");

        private UnityVersion ParseUnityVersion(string versionStr)
        {
            var matches = s_unityVersionPat.Matches(versionStr);
            if (matches.Count == 0)
            {
                return null;
            }
            Match match = matches[matches.Count - 1];
            int major = int.Parse(match.Groups[1].Value);
            int minor1 = int.Parse(match.Groups[2].Value);
            int minor2 = int.Parse(match.Groups[3].Value);
            bool isTuanjieEngine = versionStr.Contains("t");
            return new UnityVersion { major = major, minor1 = minor1, minor2 = minor2, isTuanjieEngine = isTuanjieEngine };
        }

        public string GetCurrentUnityVersionMinCompatibleVersionStr()
        {
            return GetMinCompatibleVersion(MajorVersion);
        }

        public string GetMinCompatibleVersion(int majorVersion)
        {
            switch(majorVersion)
            {
                case 2019: return "2019.4.0";
                case 2020: return "2020.3.0";
                case 2021: return "2021.3.0";
                case 2022: return "2022.3.0";
                case 2023: return "2023.2.0";
                #if UNITY_6000_3_OR_NEWER
                case 6000: return "6000.3.0";
                #else
                case 6000: return "6000.0.0";
                #endif
                default: return $"2020.3.0";
            }
        }

        public enum CompatibleType
        {
            Compatible,
            MaybeIncompatible,
            Incompatible,
        }

        public CompatibleType GetCompatibleType()
        {
            UnityVersion version = _curVersion;
            if (version == null)
            {
                return CompatibleType.Incompatible;
            }
            if ((version.major == 2019 && version.minor1 < 4)
                || (version.major >= 2020 &&  version.major <= 2022 && version.minor1 < 3))
            {
                return CompatibleType.MaybeIncompatible;
            }
            return CompatibleType.Compatible;
        }

        public string HybridclrLocalVersion => _curDefaultVersion?.hybridclr?.branch;

        public string Il2cppPlusLocalVersion => _curDefaultVersion?.il2cpp_plus?.branch;

        public string ApplicationIl2cppPath
        {
            get
            {
                Debug.Log($"application path:{EditorApplication.applicationPath} {EditorApplication.applicationContentsPath}");
#if NEW_IL2CPP_PATH
#if UNITY_IOS
                string platformDirName = "iOSSupport";
#elif UNITY_TVOS
                string platformDirName = "AppleTVSupport";
#elif UNITY_VISIONOS
                string platformDirName = "VisionOSPlayer";
#else
                string platformDirName = "iOSSupport";
#endif
                return $"{EditorApplication.applicationContentsPath}/../../PlaybackEngines/{platformDirName}/il2cpp";
#else
                return $"{EditorApplication.applicationContentsPath}/il2cpp";
  #endif
            }
        }

        public string LocalVersionFile => $"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr/generated/libil2cpp-version.txt";

        private string ReadLocalVersion()
        {
            if (!File.Exists(LocalVersionFile))
            {
                return null;
            }
            return File.ReadAllText(LocalVersionFile, Encoding.UTF8);
        }

        public void WriteLocalVersion()
        {
            InstalledLibil2cppVersion = PackageVersion;
            File.WriteAllText(LocalVersionFile, PackageVersion, Encoding.UTF8);
            Debug.Log($"Write installed version:'{PackageVersion}' to {LocalVersionFile}");
        }

        public void InstallDefaultHybridCLR()
        {
            InstallFromLocal(PrepareLibil2cppWithHybridclrFromGitRepo());
        }

        public bool HasInstalledHybridCLR()
        {
            return Directory.Exists($"{SettingsUtil.LocalIl2CppDir}/libil2cpp/hybridclr");
        }

        private string GetUnityIl2CppDllInstallLocation()
        {
#if UNITY_EDITOR_WIN
            return $"{SettingsUtil.LocalIl2CppDir}/build/deploy/net471/Unity.IL2CPP.dll";
#else
            return $"{SettingsUtil.LocalIl2CppDir}/build/deploy/il2cppcore/Unity.IL2CPP.dll";
#endif
        }

        private string GetUnityIl2CppDllModifiedPath(string curVersionStr)
        {
#if UNITY_EDITOR_WIN
            return $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedUnityAssemblies/{curVersionStr}/Unity.IL2CPP-Win.dll";
#else
            return $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedUnityAssemblies/{curVersionStr}/Unity.IL2CPP-Mac.dll";
#endif
        }

        void CloneBranch(string workDir, string repoUrl, string branch, string repoDir)
        {
            BashUtil.RemoveDir(repoDir);
            BashUtil.RunCommand(workDir, "git", new string[] {"clone", "-b", branch, "--depth", "1", repoUrl, repoDir});
        }

        private string PrepareLibil2cppWithHybridclrFromGitRepo()
        {
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);
            //BashUtil.RecreateDir(workDir);

            // clone hybridclr
            string hybridclrRepoURL = HybridCLRSettings.Instance.hybridclrRepoURL;
            string hybridclrRepoDir = $"{workDir}/{hybridclr_repo_path}";
            CloneBranch(workDir, hybridclrRepoURL, _curDefaultVersion.hybridclr.branch, hybridclrRepoDir);

            if (!Directory.Exists(hybridclrRepoDir))
            {
                throw new Exception($"clone hybridclr fail. url: {hybridclrRepoURL}");
            }

            // clone il2cpp_plus
            string il2cppPlusRepoURL = HybridCLRSettings.Instance.il2cppPlusRepoURL;
            string il2cppPlusRepoDir = $"{workDir}/{il2cpp_plus_repo_path}";
            CloneBranch(workDir, il2cppPlusRepoURL, _curDefaultVersion.il2cpp_plus.branch, il2cppPlusRepoDir);

            if (!Directory.Exists(il2cppPlusRepoDir))
            {
                throw new Exception($"clone il2cpp_plus fail. url: {il2cppPlusRepoDir}");
            }

            Directory.Move($"{hybridclrRepoDir}/hybridclr", $"{il2cppPlusRepoDir}/libil2cpp/hybridclr");
            return $"{il2cppPlusRepoDir}/libil2cpp";
        }

        public void InstallFromLocal(string libil2cppWithHybridclrSourceDir)
        {
            RunInitLocalIl2CppData(ApplicationIl2cppPath, libil2cppWithHybridclrSourceDir, _curVersion);
        }

        private void RunInitLocalIl2CppData(string editorIl2cppPath, string libil2cppWithHybridclrSourceDir, UnityVersion version)
        {
            if (GetCompatibleType() == CompatibleType.Incompatible)
            {
                Debug.LogError($"Incompatible with current version, minimum compatible version: {GetCurrentUnityVersionMinCompatibleVersionStr()}");
                return;
            }
            string workDir = SettingsUtil.HybridCLRDataDir;
            Directory.CreateDirectory(workDir);

            // create LocalIl2Cpp
            string localUnityDataDir = SettingsUtil.LocalUnityDataDir;
            BashUtil.RecreateDir(localUnityDataDir);
#if !NEW_IL2CPP_PATH
            // copy MonoBleedingEdge
            BashUtil.CopyDir($"{Directory.GetParent(editorIl2cppPath)}/MonoBleedingEdge", $"{localUnityDataDir}/MonoBleedingEdge", true);
#endif
            // copy il2cpp
            BashUtil.CopyDir(editorIl2cppPath, SettingsUtil.LocalIl2CppDir, true);
#if NEW_IL2CPP_PATH
            string buildDir = $"{SettingsUtil.LocalIl2CppDir}/build";
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                BashUtil.CopyDir($"{buildDir}/deploy_arm64", $"{buildDir}/deploy", false);
            }
            else
            {
                BashUtil.CopyDir($"{buildDir}/deploy_x86_64", $"{buildDir}/deploy", false);
            }
#endif

            // replace libil2cpp
            string dstLibil2cppDir = $"{SettingsUtil.LocalIl2CppDir}/libil2cpp";
            BashUtil.CopyDir($"{libil2cppWithHybridclrSourceDir}", dstLibil2cppDir, true);

            // clean Il2cppBuildCache
            BashUtil.RemoveDir($"{SettingsUtil.ProjectDir}/Library/Il2cppBuildCache", true);
            if (version.major == 2019)
            {
                string curVersionStr = version.ToString();
                string srcIl2CppDll = GetUnityIl2CppDllModifiedPath(curVersionStr);
                if (File.Exists(srcIl2CppDll))
                {
                    string dstIl2CppDll = GetUnityIl2CppDllInstallLocation();
                    File.Copy(srcIl2CppDll, dstIl2CppDll, true);
                    Debug.Log($"copy {srcIl2CppDll} => {dstIl2CppDll}");
                }
                else
                {
                    throw new Exception($"the modified Unity.IL2CPP.dll of {curVersionStr} isn't found. please install hybridclr in 2019.4.40 first, then switch to your unity version");
                }
            }
            InstallModifiedUnityIl2CppDll(version);
#if UNITY_2023_1_OR_NEWER
            string originalIl2CppExePath = SettingsUtil.Il2CppExecutablePath;
            string newIl2CppExePath = SettingsUtil.Il2CppOriginExecutablePath;
            if (!File.Exists(originalIl2CppExePath))
            {
                throw new FileNotFoundException("The Unity il2cpp executable was not found.", originalIl2CppExePath);
            }
            if (File.Exists(newIl2CppExePath))
            {
                File.Delete(newIl2CppExePath);
            }
            File.Move(originalIl2CppExePath, newIl2CppExePath);
            Debug.Log($"move {originalIl2CppExePath} => {newIl2CppExePath}");
#if UNITY_EDITOR_WIN
            string il2cppWrapper = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/Il2CppWrapper/Windows/il2cpp.exe";
#else
            var architecture = RuntimeInformation.ProcessArchitecture;
            string arch = architecture == Architecture.Arm64 ? "arm64" : "x64";
            string il2cppWrapper = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/Il2CppWrapper/MacOS-{arch}/il2cpp";
#endif
            if (!File.Exists(il2cppWrapper))
            {
                throw new FileNotFoundException("The HybridCLR il2cpp wrapper was not found.", il2cppWrapper);
            }
            File.Copy(il2cppWrapper, originalIl2CppExePath);
#if UNITY_EDITOR_OSX
            EnsureExecutable(originalIl2CppExePath);
#endif
            Debug.Log($"copy {il2cppWrapper} => {originalIl2CppExePath}");
#endif
            if (HasInstalledHybridCLR())
            {
                WriteLocalVersion();
                Debug.Log("Install Sucessfully");
            }
            else
            {
                Debug.LogError("Installation failed!");
            }
        }

        private void InstallModifiedUnityIl2CppDll(UnityVersion version)
        {
#if UNITY_2023_1_OR_NEWER
            if (version.major != 6000)
            {
                return;
            }
            if (!string.Equals(Application.unityVersion, SupportedModifiedDllUnityVersion, StringComparison.Ordinal))
            {
                throw new Exception($"The bundled modified Unity.IL2CPP.dll only supports Unity {SupportedModifiedDllUnityVersion}, current version is {Application.unityVersion}.");
            }

            string sourceDll = $"{SettingsUtil.ProjectDir}/{SettingsUtil.HybridCLRDataPathInPackage}/ModifiedDlls/{ModifiedDllVersion}/Unity.IL2CPP.dll";
            if (!File.Exists(sourceDll))
            {
                throw new FileNotFoundException("The bundled modified Unity.IL2CPP.dll was not found.", sourceDll);
            }
            ValidateModifiedDll(sourceDll);

            string destinationDll = SettingsUtil.UnityIl2CppDllPath;
            Directory.CreateDirectory(Path.GetDirectoryName(destinationDll));
            File.Copy(sourceDll, destinationDll, true);
            ValidateModifiedDll(destinationDll);
            Debug.Log($"copy and verify modified Unity.IL2CPP.dll {sourceDll} => {destinationDll}");
#endif
        }

#if UNITY_EDITOR_OSX
        private static void EnsureExecutable(string filePath)
        {
            int exitCode = BashUtil.RunCommand(
                "/",
                "/bin/chmod",
                new[] { "u+x", filePath },
                false);
            if (exitCode != 0)
            {
                throw new IOException($"Failed to set executable permission on '{filePath}'.");
            }
        }
#endif

        private static void ValidateModifiedDll(string dllPath)
        {
            string actualHash;
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(dllPath))
            {
                actualHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
            }
            if (!string.Equals(actualHash, ModifiedDllSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The modified Unity.IL2CPP.dll hash is invalid. Expected {ModifiedDllSha256}, actual {actualHash}, path: {dllPath}");
            }
        }
    }
}
