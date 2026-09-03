# 热更修改代码审查

审查日期：2026-09-01  
审查基线：`56de908^1` -> `56de908`（`feature/hybridclr-dhe` 合并）  
审查范围：HybridCLR 8.11.1/DHE 集成、热更加载、DLL/MetaVersion 构建与资源注册；工作区中已有的 `SoundManager.cs` 修改及文档删除/新增未纳入本次差异。

## 结论

当前不建议直接发布 DHE 热更流程。发现 1 个阻断级风险、2 个高优先级风险和 2 个中优先级风险。首要问题是 Unity 6000.3 的 HybridCLR 版本清单选择器必然取不到版本，首次安装时会在克隆仓库前后路径触发空引用；其次包声明允许的 Unity 版本与内置修改版 `Unity.IL2CPP.dll` 的精确支持版本不一致。

## 发现

### [阻断] Unity 6000.3 版本选择器与清单不一致

**位置**：`Framework/HybridCLR/Editor/Installer/InstallerController.cs:49-50,276`；`Framework/HybridCLR/Data~/hybridclr_version.json:34-36`

在 `UNITY_6000_3_OR_NEWER` 下，安装器只查找 `unity_version == "6000.3.x"`，但内置清单记录的是 `unity_version == "6000"`。因此 `_curDefaultVersion` 为 `null`，`PrepareLibil2cppWithHybridclrFromGitRepo()` 访问 `_curDefaultVersion.hybridclr.branch` 时抛出 `NullReferenceException`。`BuildDllsHelper.CheckInstallHybridClr()` 会在未安装 HybridCLR 的项目首次构建时自动进入该路径，导致热更安装和构建无法完成。

**建议**：统一清单键和值的约定（或按 major/minor 版本做显式匹配），并在克隆前对 `_curDefaultVersion` 做空值校验，输出可诊断的“不支持当前 Unity 版本”错误。

### [高] 包声明的 Unity 版本与内置修改版 IL2CPP 支持范围冲突

**位置**：`package.json:5-6`；`Documentation~/Dependencies.md:36-38`；`Framework/HybridCLR/Editor/Installer/InstallerController.cs:385-392`

包元数据和依赖文档把消费方版本写成 `6000.3.0f1`，但 Windows 安装器对 Unity 6000 只接受精确的 `6000.3.6f1`，其他版本会抛错。由于 `CheckInstallHybridClr()` 对标准模式也会安装 HybridCLR，这不仅影响 DHE，也会让使用包声明版本 `6000.3.0f1` 的新项目在首次安装时失败；并且失败发生在远程仓库克隆和本地复制之后，反馈较晚。

**建议**：将 `package.json`/依赖文档的最低或精确版本改为实际支持矩阵，或在安装入口先做 Unity 版本预检并明确区分 Standard 与 DHE 的支持范围。

### [中] 资源更新实现不支持文档承诺的“部分程序集补丁”

**位置**：`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildingResource/BuildDllsHelper.cs:407-435`；规格文档 `Documentation~/HybridCLR-DHE.md:49,53-60`

`CopyDheArtifactsToProject()` 始终遍历全部 `differentialHybridAssembliesSort`，并要求每个程序集同时存在 DLL、current MetaVersion、original MetaVersion；缺少任意一个就整体失败。文档同时规定资源更新可以只修补配置程序集的子集，其余程序集继续使用 Player 原版本。当前实现会拒绝这种合法的增量包，并强制生成全量补丁。

**建议**：由资源更新输入显式提供补丁程序集清单，并只为该子集校验/注册三类文件；或删除文档中的部分补丁承诺并在运行时/构建时统一采用全量策略。

### [中] 配置中的未知程序集被静默删除

**位置**：`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs:105-121,124-146`

`ResolveConfiguredAssemblyNames()` 用 `AssemblyDefinitionExists` 过滤掉找不到 asmdef 的条目，随后 `SyncRuntimeSetting()` 把过滤后的结果写回 `HybridCLRSetting`。拼写错误、程序集移动或导入顺序问题会被当成“同步成功”，从而改变用户配置并可能遗漏 DHE 依赖；DHE 要求依赖顺序和完整程序集集合，静默丢项会在更晚的加载/元版本阶段才暴露。

**建议**：发现配置项不存在时直接阻断构建并列出缺失名称；仅对自动发现的新增项做同步，不要把非法配置静默归一化后持久化。

### [高] ResourcesUpdate 未强制要求重新生成 DLL

**位置**：`Framework/LFramework/Scripts/Editor/BuildPackage/Builder/BuildEnvironmentConfigurator.cs:82-87,96-102`；`Framework/LFramework/Scripts/Editor/BuildPackage/Pipeline/Tasks/BuildDllTask.cs:22-31`；规格文档 `Documentation~/HybridCLR-DHE.md:53-58`

`ValidateDheBuildConfiguration()` 只在 `BuildType.App` 返回后继续校验，ResourcesUpdate 完全跳过 `isbuildDll` 检查。用户可以关闭 DLL 生成直接执行资源更新，此时 `BuildDllTask` 被跳过，项目中的 DHE DLL/MetaVersion 可能是上一次构建留下的旧文件，但资源仍会被发布，形成 DLL 与资源版本不匹配的补丁。

**建议**：DHE 的 ResourcesUpdate 也必须要求 `isbuildDll && isBuildResources`，并在复制/注册前校验产物时间戳或构建标识，避免复用旧文件。

## Standards

未发现本次自定义代码违反仓库中明确的硬性命名或序列化规则。存在一个可维护性方面的判断性问题：程序集列表解析逻辑分别出现在 `HotfixComponent`、`BuildEnvironmentConfigurator` 和 `BuildDllsHelper`，属于潜在的 duplicated logic/shotgun change，后续可能造成运行时加载集合与构建集合漂移。建议在修复上述行为问题时收敛为一个共享解析/校验入口。

## Spec

- DHE 8.11.1 集成、安装器和资源流程的主要要求已实现，但上述版本矩阵冲突使安装路径不可用。
- “资源更新可只包含部分程序集”与当前构建实现不一致（见上方中优先级发现）。
- 文档要求补丁缺少匹配 MetaVersion 时拒绝；当前对“已有 DLL 但缺 MetaVersion”的路径会拒绝，但对整个 code 标签不可用的情况仍会按可选补丁回退到 Player 原程序集，需确认这是否符合产品的离线/网络失败策略。

## 验证记录

- 已检查合并提交的文件差异、调用链和 DHE 运行时 API 签名。
- 已用 JSON 解析确认内置 6000 条目为 `"6000"`，与安装器查找的 `"6000.3.x"` 不同。
- `git diff --check`：本次 LFramework 自定义热更代码范围通过；合并中引入的第三方 HybridCLR 原始文件存在大量供应商格式空白，不作为本次业务问题结论。
- 未运行 Unity Editor 编译、DHE 安装、Player/ResourcesUpdate 构建或真机加载；当前工作区没有可直接用于该包的 `.sln/.csproj`，因此运行时结论仍需在 Unity 6000.3.6f1 项目中补充验证。
