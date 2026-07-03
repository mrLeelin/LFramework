# 04. 资源与构建

## 资源系统

资源模式由 `ResourceComponentSetting.ResourceMode` 决定：

- `YooAsset`
- `Addressable`

运行时资源入口会读取：

- `GameSetting`
- `ResourceComponentSetting`
- `SettingComponent`

## YooAsset 多包

关键文件：

- `LFramework/Scripts/Runtime/Settings/ComponentSettings/ResourceComponentSetting.cs`
- `LFramework/Scripts/Runtime/Components/ResourceComponent/YooAsset/MultiPackage/YooAssetMultiPackageUtility.cs`
- `LFramework/Scripts/Runtime/Components/ResourceComponent/YooAsset/MultiPackage/PackageRegistry.cs`
- `LFramework/Scripts/Runtime/Components/ResourceComponent/YooAsset/MultiPackage/PackageResolver.cs`

核心规则：

- `PackageDefinition.packageId` 是逻辑包 ID。
- `PackageDefinition.yooPackageName` 是实际 YooAsset 包名。
- 默认包通过 `DefaultPackageId` 解析，未配置时取第一个有效包。
- 路由索引包通过 `YooAssetRouting.routeIndexPackageId` 或默认包解析。
- 构建包、启动更新包、下载计划都通过 `YooAssetMultiPackageUtility` 收集。

## Addressables 资源存在性

`AddressableResourceHelper.HasAsset(assetName)` 是两阶段判断：

1. 先查 `Addressables.ResourceLocators` 中是否存在匹配 PrimaryKey 的 location。
2. 再用 `Addressables.GetDownloadSizeAsync(assetName).WaitForCompletion()` 判断资源是否已经本地可用。

返回语义：

- `Exist`：catalog 有 key，且下载大小为 0。
- `NotReady`：catalog 未初始化、检查失败、或仍需下载。
- `NotExist`：catalog 已就绪但找不到 key。

高频 UI 列表或批量预检不要反复同步调用 `HasAsset`，应做异步预热和缓存。

## 构建入口

构建主入口：

- `LFramework/Scripts/Editor/BuildPackage/Builder/BuildPackageWindow.cs`
- `LFramework/Scripts/Editor/BuildPackage/Builder/BuildOrchestrator.cs`
- `LFramework/Scripts/Editor/BuildPackage/Builder/BuildSetting.cs`

`BuildOrchestrator` 做三件事：

1. 创建平台配置：`PlatformConfigFactory.CreateConfig(...)`
2. 创建构建上下文：`BuildPipelineContext`
3. 根据 `BuildSetting.buildType` 选择构建管线

## 构建管线

### AppBuildPipeline

用于完整 App 构建：

1. `CreateDirectoryTask`
2. `PreprocessBuildTask`
3. `SetScriptingDefineSymbolsTask`
4. `BuildBeforeTask`
5. `BuildDllTask`
6. `BuildResourcesTask`
7. `BuildGameSettingTask`
8. `BuildPlayerTask`
9. `PostprocessBuildTask`

### ResourceBuildPipeline

用于资源更新构建：

1. `BuildDllTask`
2. `BuildResourcesTask`

## 平台配置

平台配置走 `IPlatformConfig`：

- Android：`AndroidPlatformConfig`
- iOS：`iOSPlatformConfig`
- Windows：`WindowsPlatformConfig`

平台配置负责写 PlayerSettings、输出路径、BuildPlayerOptions 等构建期差异。

## 构建问题定位顺序

1. 构建入口是否走了正确 `BuildSetting.buildType`。
2. `ResourceComponentSetting.ResourceMode` 是否和当前资源系统一致。
3. `isbuildDll` 是否决定了热更 DLL 构建。
4. `isBuildResources` 是否决定了 App 构建时是否顺带资源。
5. 平台配置是否写入了对应 PlayerSettings。
6. YooAsset/Addressables 构建输出路径是否匹配 CDN 和运行时加载路径。

