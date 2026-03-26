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
        [Inject] private ResourceComponent ResourceComponent { get; }

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
#elif HybridCLR_SUPPORT
            result = await LoadAotAssemblies();
            if (result.ResultType == LoadAssemblyResultType.Successful)
            {
                result = await LoadHotfixAssembliesInternal();
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
            callBack(new HotfixCodeResult()
            {
                ResultType = LoadAssemblyResultType.Successful
            });
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

#if HybridCLR_SUPPORT
        /// <summary>
        /// 加载Aot代码
        /// </summary>
        /// <returns></returns>
        private async UniTask<HotfixCodeResult> LoadAotAssemblies()
        {
            List<TextAsset> dlls;
            try
            {
                var handle = ResourceComponent.LoadAssetsByTagHandle<TextAsset>(HybridClrSetting.defaultAotDllLabel);
                dlls = await handle;
                handle.Release();
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
