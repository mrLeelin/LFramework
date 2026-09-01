using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework;
using Cysharp.Threading.Tasks;

#if HybridCLR_SUPPORT
using HybridCLR;
#endif


using LFramework.Runtime.Method;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// 热更系统组件。
    /// </summary>
    public partial class HotfixComponent : GameFrameworkComponent
    {
        [Inject] private ProcedureComponent ProcedureComponent { get; set; }
        [Inject] private GameSetting GameSetting { get; set; }
        [Inject] private HybridCLRSetting HybridClrSetting { get; set; }
        [Inject] private ResourceDownloadComponent ResourceDownloadComponent { get; set; }
        [Inject] private ResourceComponent ResourceComponent { get; set; }

        private Dictionary<string, Type> _allTypes;
        private readonly List<Assembly> _hotfixAssemblies = new();
        private Assembly _mainLogicAssembly;
        private readonly GameFrameworkMultiDictionary<Type, Type> _allAttributeTypes = new();


        /// <summary>
        /// 热更的程序集列表。
        /// </summary>
        public List<Assembly> HotfixAssemblies => _hotfixAssemblies;


        #region Public Method

        /// <summary>
        /// 获取所有热更类
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Type> GetHotfixAssemblyAllTypes() => _allTypes;

        /// <summary>
        /// 获取热更类型
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public Type GetHotfixAssemblyType(string typeName) => _allTypes[typeName];


        /// <summary>
        /// 获取所有实现Attribute的类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public GameFrameworkLinkedListRange<Type>? GetTypesFromAttribute<T>() where T : GameAttribute
        {
            if (_allAttributeTypes.TryGetValue(typeof(T), out var list))
            {
                return list;
            }

            return null;
        }

        public async UniTask<HotfixCodeResult> LoadHotfixAssembliesAsync()
        {
            try
            {
                
                HotfixCodeResult result = default;
#if UNITY_EDITOR
                result = LoadEditorHotfixAssemblies();
#elif HybridCLR_SUPPORT
                if (HybridClrSetting.loadMode == HybridCLRLoadMode.DifferentialHybridMetaVersion)
                {
                    bool loadPatchAssets = !GameSetting.isResourcesBuildIn && GameSetting.cdnType != CdnType.Local;
                    result = await LoadDifferentialHybridAssemblies(loadPatchAssets);
                }
                else
                if (GameSetting.isResourcesBuildIn || GameSetting.cdnType == CdnType.Local)
                {
                    result = LoadEditorHotfixAssemblies();
                }
                else
                {
                    result = await LoadAotAssemblies();
                    if (result.ResultType == LoadAssemblyResultType.Successful)
                    {
                        result = await LoadHotfixAssembliesInternal();
                    }
                }
#endif
                if (result.ResultType != LoadAssemblyResultType.Successful)
                {
                    return result;
                }

                Log.Info("The hotfix scripts loaded successfully.");
                if (_hotfixAssemblies == null || _hotfixAssemblies.Count == 0)
                {
                    return new HotfixCodeResult()
                    {
                        ResultType = LoadAssemblyResultType.HotfixError,
                        Message = "No hotfix assemblies loaded."
                    };
                }

                if (null == _mainLogicAssembly)
                {
                    return new HotfixCodeResult()
                    {
                        ResultType = LoadAssemblyResultType.HotfixError,
                        Message = "Main logic assembly missing."
                    };
                }

                ParseHotfixAssembly();
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.Successful
                };
            }
            catch (Exception e)
            {
                Log.Error($"LoadHotfixAssemblies encountered an unhandled exception: {e}");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = $"Unhandled exception in LoadHotfixAssemblies: {e.Message}"
                };
            }
        }

        public async void LoadHotfixAssemblies(Action<HotfixCodeResult> callBack)
        {
            try
            {
                callBack?.Invoke(await LoadHotfixAssembliesAsync());
            }
            catch (Exception e)
            {
                Log.Error($"LoadHotfixAssemblies callback failed: {e}");
            }
        }

        /// <summary>
        /// 进入热更程序集
        /// </summary>
        public void EnterHotfixAssembly()
        {
            IStaticMethod start = new StaticMethod(_mainLogicAssembly, "LFramework.Hotfix.Entry", "HotfixEntryStart");
            start.Run();
        }

        #endregion


        #region Private Method

        private void ParseHotfixAssembly()
        {
            try
            {
                if (_hotfixAssemblies is not { Count: > 0 })
                {
                    return;
                }

                _allTypes = new Dictionary<string, Type>();
                foreach (var assembly in _hotfixAssemblies)
                {
                    Utility.Assembly.GetAssemblyTypes(assembly, _allTypes);
                }

                ParseAttributes(_allTypes);
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }
        
        private HotfixCodeResult LoadEditorHotfixAssemblies()
        {
            Assembly mainLogicAssembly = null;
            var configuredAssemblies = GetConfiguredAssemblyNames();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Compare(HybridClrSetting.logicMainDllName, asm.GetName().Name,
                        StringComparison.Ordinal) == 0)
                {
                    mainLogicAssembly = asm;
                }

                foreach (var hotUpdateDllName in configuredAssemblies.Where(hotUpdateDllName =>
                             hotUpdateDllName == asm.GetName().Name))
                {
                    _hotfixAssemblies.Add(asm);
                }

            }

            _mainLogicAssembly = mainLogicAssembly;
            return new HotfixCodeResult()
            {
                ResultType = LoadAssemblyResultType.Successful
            };
        }

        private IReadOnlyList<string> GetConfiguredAssemblyNames()
        {
            var configuredAssemblies = HybridClrSetting.loadMode == HybridCLRLoadMode.DifferentialHybridMetaVersion
                ? HybridClrSetting.differentialHybridAssembliesSort
                : HybridClrSetting.hotfixAssembliesSort;
            var result = configuredAssemblies?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(HybridClrSetting.logicMainDllName) &&
                !result.Contains(HybridClrSetting.logicMainDllName, StringComparer.Ordinal))
            {
                result.Add(HybridClrSetting.logicMainDllName);
            }

            return result;
        }

#if HybridCLR_SUPPORT
        /// <summary>
        /// 加载Aot代码
        /// </summary>
        /// <returns></returns>
        private async UniTask<HotfixCodeResult> LoadAotAssemblies()
        {
            List<TextAsset> dlls;
            ResourceBatchHandle<TextAsset> handle = null;
            try
            {
                handle = ResourceComponent.LoadAssetsByTagHandle<TextAsset>(HybridClrSetting.defaultAotDllLabel);
                dlls = await handle;
            }
            catch (Exception e)
            {
                Log.Error($"The load aot dlls failed. {e.Message}");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = e.Message
                };
            }
            finally
            {
                handle?.Release();
            }

            if (dlls == null || dlls.Count == 0)
            {
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = "The aot count is zero."
                };
            }

            Log.Info($"The Aot dll loaded successfully. count is '{dlls.Count}';");

            foreach (var dll in dlls)
            {
                Log.Info("The aot dll loaded Start. " + dll.name);
                var err =
                    HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(AESUtility.Decrypt(dll.bytes),
                        HomologousImageMode.SuperSet);
                if (err != LoadImageErrorCode.OK)
                {
                    Log.Error($"The dill error '{err.ToString()}'");
                    return new HotfixCodeResult()
                    {
                        ResultType = LoadAssemblyResultType.LoadAotError,
                        Message = $"The dill error '{err.ToString()}'"
                    };
                }
                else
                {
                    Log.Info("The aot dll loaded successfully. " + dll.name);
                }
            }

            return new HotfixCodeResult()
            {
                ResultType = LoadAssemblyResultType.Successful
            };
        }
#endif


#if HybridCLR_SUPPORT

        private async UniTask<HotfixCodeResult> LoadDifferentialHybridAssemblies(bool loadPatchAssets)
        {
            try
            {
                if (!TryResolveDheRuntimeMethods(
                        out MethodInfo loadOriginalMethod,
                        out MethodInfo loadPatchedMethod))
                {
                    return CreateDheError(
                        "DHE mode requires a HybridCLR flagship runtime exposing the 8.11.1 MetaVersion APIs.");
                }

                var dllByAssembly = loadPatchAssets
                    ? await LoadOptionalTaggedArtifactBytes(HybridClrSetting.defaultCodeDllLabel)
                    : new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (string placeholderName in dllByAssembly
                             .Where(pair => pair.Value == null || pair.Value.Length <= 1)
                             .Select(pair => pair.Key)
                             .ToArray())
                {
                    dllByAssembly.Remove(placeholderName);
                }

                Dictionary<string, byte[]> currentMetaByAssembly = null;
                Dictionary<string, byte[]> originalMetaByAssembly = null;
                if (dllByAssembly.Count > 0)
                {
                    currentMetaByAssembly = await LoadTaggedArtifactBytes(
                        HybridClrSetting.dheCurrentMetaLabel);
                    originalMetaByAssembly = await LoadTaggedArtifactBytes(
                        HybridClrSetting.dheOriginalMetaLabel);
                }

                foreach (string assemblyName in GetConfiguredAssemblyNames())
                {
                    LoadImageErrorCode errorCode;
                    if (!dllByAssembly.TryGetValue(assemblyName, out byte[] dllBytes))
                    {
                        Log.Info($"Loading original DHE assembly '{assemblyName}'.");
                        errorCode = InvokeDheRuntimeMethod(loadOriginalMethod, assemblyName);
                    }
                    else
                    {
                        if (!currentMetaByAssembly.TryGetValue(assemblyName, out byte[] currentMeta) ||
                            currentMeta == null || currentMeta.Length <= 1 ||
                            !originalMetaByAssembly.TryGetValue(assemblyName, out byte[] originalMeta) ||
                            originalMeta == null || originalMeta.Length <= 1)
                        {
                            return CreateDheError(
                                $"DHE patch metadata is incomplete for assembly '{assemblyName}'.");
                        }

                        Log.Info($"Loading patched DHE assembly '{assemblyName}'.");
                        errorCode = InvokeDheRuntimeMethod(
                            loadPatchedMethod,
                            AESUtility.Decrypt(dllBytes),
                            null,
                            originalMeta,
                            currentMeta);
                    }

                    if (errorCode != LoadImageErrorCode.OK)
                    {
                        return CreateDheError(
                            $"Loading DHE assembly '{assemblyName}' failed with '{errorCode}'.");
                    }

                    Assembly loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(assembly => string.Equals(
                            assembly.GetName().Name,
                            assemblyName,
                            StringComparison.Ordinal));
                    if (loadedAssembly == null)
                    {
                        return CreateDheError(
                            $"DHE assembly '{assemblyName}' was loaded but is missing from the AppDomain.");
                    }

                    if (!_hotfixAssemblies.Contains(loadedAssembly))
                    {
                        _hotfixAssemblies.Add(loadedAssembly);
                    }

                    if (string.Equals(HybridClrSetting.logicMainDllName, assemblyName,
                            StringComparison.Ordinal))
                    {
                        _mainLogicAssembly = loadedAssembly;
                    }
                }

                return new HotfixCodeResult { ResultType = LoadAssemblyResultType.Successful };
            }
            catch (Exception exception)
            {
                return CreateDheError($"Loading DHE assemblies failed. {exception.Message}");
            }
        }

        private async UniTask<Dictionary<string, byte[]>> LoadTaggedArtifactBytes(string label)
        {
            ResourceBatchHandle<TextAsset> handle = null;
            try
            {
                handle = ResourceComponent.LoadAssetsByTagHandle<TextAsset>(label);
                List<TextAsset> assets = await handle ?? new List<TextAsset>();
                return IndexArtifactBytes(assets);
            }
            finally
            {
                handle?.Release();
            }
        }

        private async UniTask<Dictionary<string, byte[]>> LoadOptionalTaggedArtifactBytes(string label)
        {
            try
            {
                return await LoadTaggedArtifactBytes(label);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    "Optional DHE patch label '{0}' is unavailable. Loading the original Player assemblies instead. {1}",
                    label,
                    exception.Message);
                return new Dictionary<string, byte[]>(StringComparer.Ordinal);
            }
        }

        private static Dictionary<string, byte[]> IndexArtifactBytes(IEnumerable<TextAsset> assets)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (TextAsset asset in assets ?? Enumerable.Empty<TextAsset>())
            {
                if (asset == null)
                {
                    continue;
                }

                string assemblyName = NormalizeArtifactName(asset.name);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    byte[] bytes = asset.bytes;
                    result[assemblyName] = bytes == null ? null : bytes.ToArray();
                }
            }

            return result;
        }

        private static bool TryResolveDheRuntimeMethods(
            out MethodInfo loadOriginalMethod,
            out MethodInfo loadPatchedMethod)
        {
            Type runtimeApiType = typeof(HybridCLR.RuntimeApi);
            loadOriginalMethod = runtimeApiType.GetMethod(
                "LoadOriginalDifferentialHybridAssembly",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            loadPatchedMethod = runtimeApiType.GetMethod(
                "LoadDifferentialHybridAssemblyWithMetaVersion",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(byte[]), typeof(byte[]), typeof(byte[]), typeof(byte[]) },
                null);
            return loadOriginalMethod != null && loadPatchedMethod != null;
        }

        private static LoadImageErrorCode InvokeDheRuntimeMethod(MethodInfo method, params object[] arguments)
        {
            try
            {
                return (LoadImageErrorCode)method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw new InvalidOperationException(exception.InnerException.Message, exception.InnerException);
            }
        }

        private static string NormalizeArtifactName(string assetName)
        {
            string result = assetName;
            string[] suffixes = { ".bytes", ".dll", ".pdb", ".mv" };
            bool removedSuffix;
            do
            {
                removedSuffix = false;
                foreach (string suffix in suffixes)
                {
                    if (result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        result = result.Substring(0, result.Length - suffix.Length);
                        removedSuffix = true;
                    }
                }
            } while (removedSuffix);

            return result;
        }

        private static HotfixCodeResult CreateDheError(string message)
        {
            Log.Error(message);
            return new HotfixCodeResult
            {
                ResultType = LoadAssemblyResultType.HotfixError,
                Message = message
            };
        }
        /// <summary>
        /// 加载热更代码
        /// </summary>
        /// <returns></returns>
        private async UniTask<HotfixCodeResult> LoadHotfixAssembliesInternal()
        {
            List<TextAsset> dlls;
            ResourceBatchHandle<TextAsset> handle = null;
            try
            {
                handle = ResourceComponent.LoadAssetsByTagHandle<TextAsset>(HybridClrSetting.defaultCodeDllLabel);
                dlls = await handle;
            }
            catch (Exception e)
            {
                Log.Error($"The load Hotfix dlls failed. {e.Message}");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = e.Message
                };
            }
            finally
            {
                handle?.Release();
            }

            if (dlls == null || dlls.Count == 0)
            {
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = "The hotfix dll count is zero."
                };
            }

            Log.Info($"The Hotfix dll loaded successfully. count is '{dlls.Count}';");

            SortHotfixDll(dlls);
            try
            {
                foreach (var dll in dlls)
                {
                    var hotfixAssembly = Assembly.Load(AESUtility.Decrypt(dll.bytes));
                    _hotfixAssemblies.Add(hotfixAssembly);
                    if (string.Compare(HybridClrSetting.logicMainDllName, hotfixAssembly.GetName().Name,
                            StringComparison.Ordinal) == 0)
                    {
                        _mainLogicAssembly = hotfixAssembly;
                    }
                }
            }
            catch (Exception e)
            {
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = $"The load hotfix dll failed. {e.Message}"
                };
            }

            Log.Info($"The Hotfix dll all successfully. main logic is '{_mainLogicAssembly.GetName().Name}'");
            return new HotfixCodeResult()
            {
                ResultType = LoadAssemblyResultType.Successful
            };
        }

        private void SortHotfixDll(List<TextAsset> dlls)
        {
            if (dlls.Count != HybridClrSetting.hotfixAssembliesSort.Count)
            {
                Log.Fatal("The hotfix dlls count is not match with setting. " +
                          $"Expected: {HybridClrSetting.hotfixAssembliesSort.Count}, " +
                          $"Actual: {dlls.Count}");
            }

            foreach (var dll in dlls)
            {
                Log.Info("The hotfix dll name is " + dll.name);
            }

            dlls.Sort((x, y) =>
            {
                var xName = x.name.Replace(".dll", "");
                var yName = y.name.Replace(".dll", "");
                Log.Info("The hotfix sort dll name is " + xName + " - " + yName);
                var xIndex = HybridClrSetting.hotfixAssembliesSort.IndexOf(xName);
                var yIndex = HybridClrSetting.hotfixAssembliesSort.IndexOf(yName);
                if (xIndex < 0 || yIndex < 0)
                {
                    Log.Fatal($"The hotfix dlls sort failed. {x.name} or {y.name} not in setting.");
                    return 0;
                }

                return xIndex.CompareTo(yIndex);
            });
            foreach (var dll in dlls)
            {
                Log.Info("The hotfix sort dll name is " + dll.name);
            }
        }

#endif


        /// <summary>
        /// 添加流程
        /// </summary>
        private void ParseAttributes(Dictionary<string, Type> types)
        {
            _allAttributeTypes.Clear();

            foreach (var type in types.Values)
            {
                foreach (var customAttributeData in type.GetCustomAttributes(typeof(GameAttribute), true))
                {
                    _allAttributeTypes.Add(customAttributeData.GetType(), type);
                }
            }
        }

        /// <summary>
        /// 获取所有BaseAttributes的类
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        private List<Type> GetBaseAttributes(Dictionary<string, Type> types)
        {
            return types.Values.Where(type => !type.IsAbstract)
                .Where(type => type.IsSubclassOf(typeof(GameAttribute))).ToList();
        }

        #endregion
    }
}
