# HybridCLR 热更新全链路代码审查

审查日期：2026-09-02  
审查范围：当前工作树中 HybridCLR、Hotfix、DHE、DLL 构建、资源注册、Player 加载和相关测试的未提交修改。  
审查方式：先进行全链路静态审查，再按确认范围实施修复；基线为当前 `HEAD`，未触碰无关的文档、资源和音频修改。

> **文档状态说明（2026-09-02）**：本文主体记录的是修复前的风险证据，下面的“原始修改不建议直接合并”及其问题清单属于历史审查结论，不能作为当前代码行为说明。当前实现已经按“旗舰源码存在即自动使用、无手动 Setting、无 DHE 反射桥接”的方案修复；请以 [`HYBRIDCLR_DHE_AUTO_EDITION_2026-09-02.md`](HYBRIDCLR_DHE_AUTO_EDITION_2026-09-02.md) 的实现说明和验证记录为准。

当前修复后的关键变化：

- 通过 `HybridClrEditionDefineSynchronizer` 检测 `HybridCLR.Editor/Editor/DHE/MetaVersionWorkflow.cs`，自动同步 `LFRAMEWORK_HYBRIDCLR_DHE`。
- DHE 类型、`differentialHybridAssemblies` 和运行时加载 API 只在该宏下编译，社区版编译路径不再引用旗舰专有成员。
- 删除 `runtimeUseDheHybridClr` 和 `GetHasRequiredApis()`；构建和 Player 的版型由编译期宏统一决定。
- Unity 正式编译、社区版项目编译和版型同步逻辑 harness 均已通过；EditMode 测试因未保存的用户场景未启动。

## 结论

原始修改不建议直接合并。修复后，编辑器检测、构建流程和 Player 运行时的分叉已被收敛，但仍保留一个明确的产品边界：

1. 编辑器构建使用 `HybridCLRBuildWorkflow.IsDheAvailable`。
2. App 构建把结果写入 `HybridCLRSetting.useDheHybridClr`。
3. Player 启动时读取已经打包的 `useDheHybridClr`。

`IsDheAvailable` 现在按 API、包版本、本地 libil2cpp 版本和 DHE 原生源文件共同判断；App 构建把结果写入 Player 设置；ResourcesUpdate 校验该设置与当前环境一致。代码仍然保留对 DHE API 的编译期强依赖，因此 `GetHasRequiredApis()` 是能力探测，不是社区版/付费版许可证兼容层。

## 高风险问题

### 1. 社区版无法在编译阶段进入自动检测

**位置**：

- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/HybridCLR/HybridCLRBuildWorkflow.cs:8-12, 40-42, 63-65, 104-114`
- `Framework/LFramework/Scripts/Runtime/Components/HotfixComponent/HotfixComponent.cs:341, 356`

`GetHasRequiredApis()` 使用反射检查 DHE 类型和方法，但同一程序集仍然直接引用：

- `HybridCLR.Editor.DHE.MetaVersionWorkflow`
- `HybridCLR.Editor.Settings.HybridCLRSettings.differentialHybridAssemblies`
- `HybridCLR.RuntimeApi.LoadOriginalDifferentialHybridAssembly`
- `HybridCLR.RuntimeApi.LoadDifferentialHybridAssemblyWithMetaVersion`

如果社区版缺少这些类型或成员，程序集会在编译期失败，根本不会执行 `GetHasRequiredApis()`。`HybridCLR_SUPPORT` 只表示程序集/包的版本定义满足条件，不等于付费 DHE 授权。

**建议**：如果必须支持两个版本，DHE 代码拆到可选 asmdef，或保留完整反射边界；如果只支持付费版，则删除伪装成兼容检测的反射逻辑，明确在 asmdef/文档中声明最低版本约束。

### 2. 标准 HybridCLR 配置被无条件改写

**位置**：

- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs:48-59`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs:143-166`

`EnsureProjectSettingsSynchronized()` 每次初始化和构建前都会调用 `SyncHybridClrSettingsAsset()`。该方法无条件执行：

```csharp
settings.hotUpdateAssemblies = Array.Empty<string>();
settings.hotUpdateAssemblyDefinitions = Array.Empty<AssemblyDefinitionAsset>();
settings.differentialHybridAssemblies = assemblyNames;
```

这里没有先确认 `IsDheAvailable`、本地 `libil2cpp` 是否安装，甚至在 `TryUpdateDifferentialHybridAssemblies()` 返回 `false` 前已经清空了标准配置。社区版、未安装原生环境或 DHE API 不可用时，标准 HotUpdate 配置可能被内存状态污染，后续 HybridCLR 检查和编译会失败。

**建议**：先完成完整能力检查；DHE 不可用时只同步 `hotUpdateAssemblies/hotUpdateAssemblyDefinitions`，绝不清空标准配置。能力检查失败也不应修改任何设置对象。

### 3. 内置资源或 Local CDN 模式绕过 DHE

**位置**：`Framework/LFramework/Scripts/Runtime/Components/HotfixComponent/HotfixComponent.cs:81-105`

当前运行时先判断：

```csharp
if (GameSetting.isResourcesBuildIn || GameSetting.cdnType == CdnType.Local)
    result = LoadEditorHotfixAssemblies();
else if (HybridClrSetting.useDheHybridClr)
    result = await LoadDifferentialHybridAssemblies(...);
```

DHE 判断被放到了资源模式判断之后。DHE App 即使把占位资源内置到 Player，只要 `isResourcesBuildIn` 为 `true` 或 CDN 为 `Local`，就不会调用 `LoadOriginalDifferentialHybridAssembly`，而是只在当前 AppDomain 中查找程序集。对于被剥离、只由原生 DHE 基线承载的程序集，这会导致加载失败或加载语义错误。

**建议**：先根据构建产物的 DHE manifest 决定模式；DHE 模式下无论资源是内置还是远程，都进入 DHE 加载器，`loadPatchAssets` 只决定是否读取更新资源。

### 4. 编辑器实时检测与 Player 持久标记可能分叉

**位置**：

- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildingResource/BuildDllsHelper.cs:43-57, 82-114`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Tasks/BuildGameSettingTask.cs:98-104`
- `Framework/LFramework/Scripts/Runtime/Components/HotfixComponent/HotfixComponent.cs:90-101`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Pipelines/ResourceBuildPipeline.cs:20-35`

DLL 构建和资源复制实时读取 `IsDheAvailable`，而 Player 运行时读取序列化的 `useDheHybridClr`。该字段只在 App pipeline 的 `BuildGameSettingTask` 中更新；ResourceBuildPipeline 不执行这个任务。

因此可能出现：

- App 仍携带 `useDheHybridClr = false`，ResourcesUpdate 却按 DHE 生成资源。
- App 携带 `true`，之后编辑器环境变化或资源包改用标准模式，Player 仍强行调用 DHE native internal call。

**建议**：构建时生成带版本/模式指纹的统一 manifest，App 和 ResourcesUpdate 都从同一份 manifest 校验；Player 只读取该 manifest，不读取容易滞后的编辑器 Setting。

### 5. ResourcesUpdate 可以在未生成 DLL 时发布 DHE 资源

**位置**：

- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs:82-107`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Pipelines/ResourceBuildPipeline.cs:20-35`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Tasks/BuildDllTask.cs:22-29`

`ValidateDheBuildConfiguration()` 只校验 `BuildType.App`。ResourcesUpdate 如果 `isbuildDll == false`，`BuildDllTask` 会被跳过，但资源任务仍可继续执行。DHE 资源可能因此沿用旧 DLL/MetaVersion，形成资源版本与代码版本不匹配的更新包。

**建议**：DHE ResourcesUpdate 必须强制 `isbuildDll == true`，并在资源发布前验证 DLL、CurrentMeta、OriginalMeta 与同一基线快照的版本指纹一致。

## 中风险问题

### 6. 所有 Hotfix 程序集被隐式当作 DHE 程序集

**位置**：

- `Framework/LFramework/Scripts/Runtime/Settings/HybridCLRSetting.cs:61-73`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs:109-123, 143-166`
- `Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildingResource/BuildDllsHelper.cs:523-545`
- `Framework/LFramework/Scripts/Runtime/Components/HotfixComponent/HotfixComponent.cs:224-238`

修改删除了 `differentialHybridAssembliesSort`，改为把 `hotfixAssembliesSort` 全量写入 DHE 设置。原本可以区分标准 Hotfix 集合和 DHE 子集，现在所有 Hotfix 程序集都会参与 DHE 快照和运行时加载。只要其中一个程序集没有按 DHE 方式生成，native API 可能返回 `DHE_NOT_DIFFERENTIAL_HYBRID_ASSEMBLY`。

**建议**：保留独立的 DHE 程序集集合，或者明确规定全部 Hotfix 都必须是 DHE，并在构建前检查程序集清单、依赖顺序和快照 manifest。

### 7. “付费版”检测存在伪阳性

**位置**：`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/HybridCLR/HybridCLRBuildWorkflow.cs:31-36, 194-206`

`GetHasRequiredApis()` 只检查托管类型和方法名/参数。`IsDheAvailable` 还依赖可修改的 `hybridclr_package.json` 版本字符串，以及安装器写入的 `libil2cpp-version.txt`。这些信号不能证明签名授权，也不能验证 Player 最终链接了带 DHE 实现的 native `libil2cpp`。编辑器下 `RuntimeApi` 的 DHE 方法甚至固定返回 `OK`，因此编辑器通过不代表 Player 可用。

**建议**：将名称改为 `IsDheApiAvailable` 或 `IsDheBuildCapable`；如果官方没有 License API，不要把它描述成付费授权判断。至少增加 native 构建产物或安装源的指纹校验。

### 8. 安装失败异常类型错误且阻断普通流程

**位置**：`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildingResource/BuildDllsHelper.cs:221-235`

未安装 HybridCLR 时抛出 `NullReferenceException`，但实际不是空引用；原自动安装逻辑被注释掉。错误类型会误导诊断，且普通构建也被直接阻断。

**建议**：改为 `InvalidOperationException` 或 `BuildFailedException`，明确区分“未安装”“版本不匹配”“DHE native 环境不可用”。

### 9. DHE 产物只检查文件存在，不检查内容和版本

**位置**：`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildingResource/BuildDllsHelper.cs:416-455`

ResourcesUpdate 只用 `File.Exists` 验证 DLL、CurrentMeta、OriginalMeta。空文件、旧文件或与当前 DLL 不对应的 MetaVersion 都会通过检查，随后在 Player 中才失败。

**建议**：检查长度、MetaVersion 解析结果、程序集名称和快照基线指纹；复制前生成校验清单，注册资源时把清单版本一起发布。

## 低风险及维护性问题

### 10. 检测结果未缓存

`GetHasRequiredApis()` 每次访问都会扫描所有已加载程序集并反射查找五个方法。它只发生在编辑器，但可以在一次构建中多次触发。建议在程序集重载后缓存，并在检测失败时记录具体缺失的类型/方法。

### 11. 测试覆盖不足

当前测试只覆盖 `TryUpdateDifferentialHybridAssemblies()` 的成功更新和目录清理，没有覆盖：

- DHE API 缺失时的检测结果；
- 包版本或 native 版本不匹配；
- App 与 ResourcesUpdate 的模式一致性；
- 内置资源/Local CDN 下的 DHE 加载；
- DHE 资源缺 DLL、空 MetaVersion、旧基线的拒绝行为；
- 直接 RuntimeApi 调用的 Player 端结果。

## 推荐修复顺序

1. 先统一模式来源，修复 App/ResourcesUpdate/Player 分叉。
2. 修复 `BuildEnvironmentConfigurator` 的无条件配置覆盖。
3. 恢复 DHE 与标准程序集的明确边界，并验证依赖顺序。
4. 决定是否支持社区版：支持则拆分可选程序集或恢复反射边界；只支持付费版则明确编译约束。
5. 增加 ResourcesUpdate 产物完整性和版本指纹校验。
6. 补充 EditMode 测试，并至少对一个实际 Player 做 DHE 原始加载和补丁加载验证。

## 修复后状态

本次按确认范围完成了以下修复：

1. `HybridCLRBuildWorkflow.IsDheAvailable` 同时检查 DHE API、包版本 `8.11.1`、本地 libil2cpp 版本和 `DifferentialHybridImage.cpp/.h`；检测失败安全返回 `false`，构建入口使用 `EnsureDheAvailable()` 给出明确异常。
2. `BuildEnvironmentConfigurator` 按检测结果同步标准 HybridCLR 或 DHE 配置。标准模式只填充 asmdef 列表并清空 DHE 列表；DHE 模式只填充 `differentialHybridAssemblies`，避免双重枚举和静默过滤缺失 asmdef。
3. DHE 模式优先级已移到运行时资源来源判断之前。Player 中 DHE 即使资源内置或使用 Local CDN，也会调用原生 `LoadOriginalDifferentialHybridAssembly`；`loadPatchAssets` 只控制是否读取补丁资源。`#if UNITY_EDITOR` 分支仍只在 Unity Editor 编译目标中生效。
4. `ResourcesUpdate` 会校验序列化模式与当前环境一致；DHE 资源更新必须同时重建 DLL 和 MetaVersion。DLL 构建、复制和 MetaVersion 路径统一使用 `BuildTarget`，避免活动目标与请求目标不一致。
5. AOT、Hotfix、DHE DLL/MetaVersion 复制前增加非空内容检查；缺失或空产物会立即失败，并清理过期 DHE 资源文件。安装失败改为明确的 `InvalidOperationException`。
6. `CheckSettings` 只在 DHE 配置非空时检查 DHE 专用 deploy/native/InjectRules 环境，标准模式会清理遗留的进程环境变量；字节搜索边界和空输入也已修复。
7. 新增 DHE 能力判断、版本组合、产物内容和目录清理的 EditMode 测试。

## 仍需关注的边界

- 代码仍直接引用 DHE 类型、字段和 `RuntimeApi`。因此不支持“同一份已编译程序集同时兼容缺少这些 API 的社区版包”；社区版会在编译阶段失败，`GetHasRequiredApis()` 无法绕过编译器。这是当前选择直接调用原生 API 的编译契约。
- `IsDheAvailable` 表示“当前编辑器环境具备 DHE 构建能力”，不是官方许可证或付费授权证明。最终 `Unity.IL2CPP.dll` 指纹、Player 链接结果和原生 `InternalCall` 仍须在真实构建中验证。
- 当前策略把 `hotfixAssembliesSort` 全量作为 DHE 集合；如果产品需要标准 Hotfix 与 DHE 子集并存，应恢复独立集合，而不是在运行时猜测。
- 现有 `GetHasRequiredApis()` 未做缓存，也未逐项记录缺失 API；这影响诊断效率，但不改变本次构建结果。

## 验证边界

已完成的验证：

- Unity 生成的 Roslyn 参数对 `LFramework.Runtime`、`LFramework.Editor`、`LFramework.Editor.Tests` 分别编译通过；Runtime 有既有警告，Editor 有既有 API 过时警告，均无编译错误。
- 当前 Unity `Library/ScriptAssemblies` 中三个程序集时间戳为 2026-09-02 10:47，日志对应编译阶段没有新的 `CS` 或 Tundra 错误。
- 静态调用检查确认 `BuildDll`/`CopyDll` 只有新的带 `BuildTarget` 参数调用；旧的 DHE 状态符号不存在；`git diff --check` 没有空白错误（仅有换行格式提示）。

未完成或不具备结论的验证：

- Unity Pipeline TestRunner 的筛选参数未生效，实际启动了 319 个 EditMode 测试并在 60 秒超时，不能视为测试通过。
- 未执行真实 App、ResourcesUpdate、AOT 快照、资源包下载和设备/Player 原生 DHE 加载；这些仍是发布前必须补齐的验证。
