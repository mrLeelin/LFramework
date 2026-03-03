using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework;
using Cysharp.Threading.Tasks;

#if USE_HybridCLR
using HybridCLR;

#endif

#if UNITY_EDITOR && USE_HybridCLR
using HybridCLR.Editor.Settings;
#endif

using LFramework.Runtime.Method;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    /// <summary>
    /// 热更系统组件。
    /// </summary>
    public class HotfixComponent : GameFrameworkComponent
    {
        [Inject] private ProcedureComponent ProcedureComponent { get; set; }
        [Inject] private GameSetting GameSetting { get; }
        [Inject] private HybridCLRSetting HybridClrSetting { get; }
        [Inject] private ResourceDownloadComponent ResourceDownloadComponent { get; }

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

        public async void LoadHotfixAssemblies(Action<HotfixCodeResult> callBack)
        {
            HotfixCodeResult result = default;
#if UNITY_EDITOR
            result = LoadEditorHotfixAssemblies();
#elif USE_HybridCLR
            result = await LoadAotAssemblies();
            if (result.ResultType == LoadAssemblyResultType.Successful)
            {
                result = await LoadHotfixAssemblies();
            }
#endif
            if (result.ResultType != LoadAssemblyResultType.Successful)
            {
                callBack(result);
                return;
            }

            Log.Info("The hotfix scripts loaded successfully.");
            if (_hotfixAssemblies == null || _hotfixAssemblies.Count == 0)
            {
                callBack(new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = "No hotfix assemblies loaded."
                });
                return;
            }

            if (null == _mainLogicAssembly)
            {
                callBack(new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = "Main logic assembly missing."
                });
                return;
            }

            ParseHotfixAssembly();
            IStaticMethod start = new StaticMethod(_mainLogicAssembly, "LFramework.Hotfix.Entry", "HotfixEntryStart");
            start.Run();
            callBack(new HotfixCodeResult()
            {
                ResultType = LoadAssemblyResultType.Successful
            });
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

#if UNITY_EDITOR
        private HotfixCodeResult LoadEditorHotfixAssemblies()
        {
            Assembly mainLogicAssembly = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Compare(HybridClrSetting.logicMainDllName, asm.GetName().Name,
                        StringComparison.Ordinal) == 0)
                {
                    mainLogicAssembly = asm;
                }

                foreach (var hotUpdateDllName in
                         HybridClrSetting.hotfixAssembliesSort.Where(hotUpdateDllName =>
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
#endif

#if USE_HybridCLR
        /// <summary>
        /// 加载Aot代码
        /// </summary>
        /// <returns></returns>
        private async UniTask<HotfixCodeResult> LoadAotAssemblies()
        {
            var result = await LoadAssembliesFormLabel(GameSetting.hybridClrSetting.defaultAotDllLabel);
            if (result.Item1.ResultType != LoadAssemblyResultType.Successful)
            {
                return result.Item1;
            }

            if (result.Item2 == null || result.Item2.Count == 0)
            {
                return result.Item1;
            }

            return await LoadAotLocations(result.Item2);
        }
        
        
        private async UniTask<HotfixCodeResult> LoadAotLocations(IList<IResourceLocation> locations)
        {
            /*
            foreach (var location in locations)
            {
                if (!location.InternalId.EndsWith(".dll.bytes")) // 再次确认是 .dll.bytes 文件
                {
                    Log.Fatal($"The load aot locations failed. {{not dll file '{location.InternalId}'}}");
                    return LoadAssemblyResultType.LoadAotError;
                }
            }
            */

            var dlls = new List<TextAsset>();
            var status = AsyncOperationStatus.Failed;
            var loadedHandle = Addressables.LoadAssetsAsync<TextAsset>(locations, null, false);
            try
            {
                await loadedHandle.Task;
                status = loadedHandle.Status;
                dlls.AddRange(loadedHandle.Result);
            }
            catch (Exception e)
            {
                status = AsyncOperationStatus.Failed;
                Log.Error($"The load aot dlls failed. {loadedHandle.OperationException}");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = loadedHandle.OperationException.Message
                };
            }
            finally
            {
                Addressables.Release(loadedHandle);
            }

            if (status != AsyncOperationStatus.Succeeded)
            {
                Log.Error(
                    $"The Aot dll loaded error. count is '{dlls.Count}'  message '{loadedHandle.OperationException.Message}';");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = loadedHandle.OperationException.Message
                };
            }

            Log.Info($"The Aot dll loaded successfully. count is '{dlls.Count}';");
            if (dlls.Count == 0)
            {
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = "The aot count is zero."
                };
            }

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
        


#if USE_HybridCLR
        
        /// <summary>
        /// 加载热更代码
        /// </summary>
        /// <returns></returns>
        private async UniTask<HotfixCodeResult> LoadHotfixAssemblies()
        {
            var result = await LoadAssembliesFormLabel(GameSetting.hybridClrSetting.defaultCodeDllLabel);
            if (result.Item1.ResultType != LoadAssemblyResultType.Successful)
            {
                return result.Item1;
            }

            if (result.Item2 == null || result.Item2.Count == 0)
            {
                return result.Item1;
            }

            return await LoadHotfixLocations(result.Item2);
        }
        
        private async UniTask<HotfixCodeResult> LoadHotfixLocations(IList<IResourceLocation> locations)
        {
            var dlls = new List<TextAsset>();
            var status = AsyncOperationStatus.Failed;
            var loadedHandle = Addressables.LoadAssetsAsync<TextAsset>(locations, null, false);
            try
            {
                await loadedHandle.Task;
                status = loadedHandle.Status;
                dlls.AddRange(loadedHandle.Result);
            }
            catch (Exception e)
            {
                status = AsyncOperationStatus.Failed;
                Log.Error($"The load Hotfix dlls failed. {loadedHandle.OperationException}");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = loadedHandle.OperationException.Message
                };
            }
            finally
            {
                Addressables.Release(loadedHandle);
            }

            if (status != AsyncOperationStatus.Succeeded)
            {
                Log.Error(
                    $"The Hotfix dll loaded error. count is '{dlls.Count}'  message '{loadedHandle.OperationException.Message}';");
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = loadedHandle.OperationException.Message
                };
            }

            Log.Info($"The Aot dll loaded successfully. count is '{dlls.Count}';");
            if (dlls.Count == 0)
            {
                return new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.HotfixError,
                    Message = "The aot count is zero."
                };
            }

            SortHotfixDll(dlls);
            try
            {
                foreach (var dll in dlls)
                {
                    var hotfixAssembly = Assembly.Load(AESUtility.Decrypt(dll.bytes));
                    _hotfixAssemblies.Add(hotfixAssembly);
                    if (string.Compare(GameSetting.hybridClrSetting.logicMainDllName, hotfixAssembly.GetName().Name,
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
            if (dlls.Count != GameSetting.hybridClrSetting.hotfixAssembliesSort.Count)
            {
                Log.Fatal("The hotfix dlls count is not match with setting. " +
                          $"Expected: {GameSetting.hybridClrSetting.hotfixAssembliesSort.Count}, " +
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
                var xIndex = GameSetting.hybridClrSetting.hotfixAssembliesSort.IndexOf(xName);
                var yIndex = GameSetting.hybridClrSetting.hotfixAssembliesSort.IndexOf(yName);
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

         private async UniTask<(HotfixCodeResult, IList<IResourceLocation>)> LoadAssembliesFormLabel(string label)
        {
            var handle = Addressables.LoadResourceLocationsAsync(label);
            IList<IResourceLocation> locations = null;
            AsyncOperationStatus status = AsyncOperationStatus.Failed;
            try
            {
                await handle.Task;
                locations = handle.Result;
                status = handle.Status;
            }
            catch (Exception e)
            {
                status = AsyncOperationStatus.Failed;
                Log.Error($"The load aot locations failed. {e.Message}");
                return (new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = e.Message
                }, null);
            }
            finally
            {
                Addressables.Release(handle);
            }

            if (status != AsyncOperationStatus.Succeeded)
            {
                return (new HotfixCodeResult()
                {
                    ResultType = LoadAssemblyResultType.LoadAotError,
                    Message = handle.OperationException.Message
                }, null);
            }

            return (new HotfixCodeResult()
            {
                ResultType = LoadAssemblyResultType.Successful
            }, locations);
        }

#endif
        


        
       

        /// <summary>
        /// 添加流程
        /// </summary>
        private void ParseAttributes(Dictionary<string, Type> types)
        {
            // var baseAttributeTypes = GetBaseAttributes(types);
            _allAttributeTypes.Clear();

            foreach (var type in types.Values)
            {
                foreach (var customAttributeData in type.GetCustomAttributes(typeof(GameAttribute), true))
                {
                    _allAttributeTypes.Add(customAttributeData.GetType(), type);
                }
            }

            /*
            foreach (var baseAttributeType in baseAttributeTypes)
            {
                foreach (var type in types.Values)
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    if (type.GetCustomAttribute(baseAttributeType) == null)
                    {
                        continue;
                    }

                    _allAttributeTypes.Add(baseAttributeType, type);
                }
            }
            */
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