# HybridCLR DHE 自动版型方案

## 结论

当前方案不再通过 `HybridCLRSetting` 让用户手动选择 DHE，也不在运行时用反射探测 DHE API。

- 工程中存在旗舰版 `HybridCLR.Editor` 的 `Editor/DHE/MetaVersionWorkflow.cs` 时，编辑器自动维护 `LFRAMEWORK_HYBRIDCLR_DHE`。
- 该宏打开后，构建链和 Player 运行时直接编译/调用 8.11.1 的 DHE API。
- 没有该源码时，宏被移除，构建链自动回到社区版的 AOT 元数据 + 普通热更 DLL 流程。
- 旗舰版源码存在但 `libil2cpp` 版本、版本标记或原生 DHE 文件不完整时，构建失败并给出明确错误，不会静默降级。

## 自动判断

实现文件：

`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/HybridCLR/HybridClrEditionDefineSynchronizer.cs`

判断条件同时要求：

1. 编译程序集名为 `HybridCLR.Editor`。
2. 该程序集的 source file 列表中存在 `.../Editor/DHE/MetaVersionWorkflow.cs`。
3. 路径中包含 HybridCLR 目录，避免其他包同名文件误触发。

同步器通过 `CompilationPipeline.GetAssemblies(AssembliesType.Editor)` 读取已编译源文件，通过 `PlayerSettings.Get/SetScriptingDefineSymbols(NamedBuildTarget)` 更新 Standalone、Android、iOS 及当前活动目标。

同步器在两种时机运行：

- `[InitializeOnLoadMethod]`：Unity 编辑器加载后同步一次。
- `AssetPostprocessor.OnPostprocessAllAssets`：HybridCLR 相关源文件导入、删除或移动后排队同步。删除旗舰源码时，这个回调会先移除旧宏，避免社区版继续编译 DHE 类型。

同步只增删内部宏 `LFRAMEWORK_HYBRIDCLR_DHE`，保留其他业务宏，并且幂等。变更后调用 `AssetDatabase.SaveAssets()`，使目标平台宏写入 `ProjectSettings/ProjectSettings.asset`。

## 构建流程

核心文件：

- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/HybridCLR/HybridCLRBuildWorkflow.cs`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildingResource/BuildDllsHelper.cs`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Tasks/BuildDllTask.cs`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Tasks/PostprocessBuildTask.cs`

旗舰分支：

- 直接调用 `HybridCLR.Editor.DHE.MetaVersionWorkflow`。
- 直接写入 `HybridCLRSettings.differentialHybridAssemblies`。
- App 构建生成 DHE 占位 DLL、当前 MetaVersion 和原始 MetaVersion。
- 热更资源构建生成当前 MetaVersion，并要求基线快照存在。
- App 构建后生成新的 AOT 快照。
- 旗舰版资源更新必须同时构建 DLL 和资源，避免资源包与基线快照不匹配。

社区分支：

- 使用 `PrebuildCommand.GenerateAll()`、AOT 元数据裁剪/泛型引用和普通热更 DLL。
- 不引用 DHE 类型或 `differentialHybridAssemblies` 字段。
- 使用 `HybridCLRSettings.hotUpdateAssemblyDefinitions` 配置普通热更程序集。

两种版型都继续复用 `HybridCLRSetting.hotfixAssembliesSort` 和 `logicMainDllName`。旗舰版不再依赖额外的 `differentialAssembliesSort` 或运行时布尔值。

## 运行时加载

实现文件：

`Framework/LFramework/Scripts/Runtime/Components/HotfixComponent/HotfixComponent.cs`

运行时路径如下：

```text
Unity Editor                 -> LoadEditorHotfixAssemblies
Player + LFRAMEWORK...DHE   -> LoadDifferentialHybridAssemblies
Player + community          -> LoadAotAssemblies
                               -> LoadHotfixAssembliesInternal
```

旗舰版 Player 中：

- 内置资源或 `CdnType.Local`：加载原始 Player DHE image，不读取补丁 DLL。
- 远程资源：先尝试读取补丁 DLL；补丁存在时要求当前/原始 MetaVersion 成对存在，否则返回热更错误。
- 每个程序集加载后都会从 `AppDomain` 确认实际程序集已出现，并设置主逻辑程序集。

社区版 Player 中：

- 内置资源或本地资源：沿用 Editor 程序集加载。
- 远程资源：先加载 AOT 元数据，再加载普通热更 DLL。

`runtimeUseDheHybridClr` 已删除，Player 不再读取打包阶段写回 Setting 的状态。

## 程序集边界

`LFramework.Runtime.asmdef` 只引用 `HybridCLR.Runtime`，不再引用 `HybridCLR.Editor`。

`LFramework.Editor.asmdef` 和 `LFramework.Editor.Tests.asmdef` 使用程序集名引用 `HybridCLR.Editor`。这样引用不依赖当前嵌入目录的 GUID，社区版/旗舰版只要提供相同的公共程序集名即可切换。

## 验证记录

- `LFramework.Runtime.csproj` 社区宏路径：编译通过。
- `LFramework.Editor.csproj` 社区宏路径：编译通过。
- 自动同步器纯逻辑 harness：通过版型识别、误判排除、宏添加/删除幂等、删除文件变更检测。
- Unity 正式编译：`compile` 返回 `Success=true`、`ErrorCount=0`；旗舰宏打开后 DHE 分支实际编译通过。
- Unity 源码扫描实际发现 `HybridCLR.Editor` 中的 `Editor/DHE/MetaVersionWorkflow.cs`，并确认自动同步后活动目标含 `LFRAMEWORK_HYBRIDCLR_DHE`。
- Unity EditMode 测试未执行：当前 Unity 编辑器有未保存的 `Assets/Scenes/SampleScene.unity` 修改，测试工具要求保存场景后才能运行。本次未保存或覆盖该用户场景变更。

## 使用要求

安装/替换 HybridCLR 版型后等待 Unity 完成一次脚本编译即可。不要再修改 `HybridCLRSetting` 里的 DHE 选择项，也不要在构建脚本或资源中写入运行时版型布尔值。

旗舰版 DHE 8.11.1 仍要求本地 `libil2cpp` 与包版本一致，并且安装器生成的 DHE 原生文件完整；自动识别只负责版型选择，不替代安装完整性校验。
