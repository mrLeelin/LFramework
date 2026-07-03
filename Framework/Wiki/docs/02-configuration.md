# 02. 配置整理

## 配置入口

配置统一从 `ProjectSettingSelector` 汇总，再由 `SettingManager` 访问。

关键代码：

- `LFramework/Scripts/Runtime/Settings/ProjectSettingSelector.cs`
- `LFramework/Scripts/Runtime/Settings/SettingManager.cs`
- `LFramework/Scripts/Runtime/Settings/BaseSetting.cs`
- `LFramework/Scripts/Runtime/Settings/ComponentSettings/ComponentSetting.cs`

## 配置资产分类

| 类型 | 例子 | 用途 |
| --- | --- | --- |
| BaseSetting | `GameSetting`, `AndroidSetting`, `iOSSetting`, `HybridCLRSetting` | 项目级配置、平台配置、热更配置 |
| ComponentSetting | `ResourceComponentSetting`, `ProcedureComponentSetting`, `UIComponentSetting` | 绑定具体运行时组件的配置 |
| ProjectSettingSelector | `ProjectSettingSelector` | 当前工程实际启用的配置集合 |

## SettingManager 读取规则

Editor 下：

- `SettingManager.GetProjectSelector()` 用 `AssetDatabase.FindAssets` 查找第一个 `ProjectSettingSelector`。
- 适合在编辑器工具、Inspector、构建工具中使用。

Player 下：

- `SettingManager.GetProjectSelector()` 从 `Resources.Load<ProjectSettingSelector>("ProjectSettingSelector")` 读取。
- 因此正式运行时必须确保工程侧存在可加载的 `ProjectSettingSelector` 资源。

## ProjectSettingSelector 去重规则

`ProjectSettingSelector` 分别维护：

- `selectedSettings`
- `selectedComponentSettings`

设置新配置时，会按下面规则清理重复项：

- 类型相同视为同一配置。
- `SettingId` 非空且相同，也视为同一配置。

这意味着新增配置时，不要只复制资产而不改 `SettingId`，否则可能被认为是同一个配置。

## 资源配置重点

`ResourceComponentSetting` 是资源系统配置核心：

- `ResourceMode` 决定使用 `YooAsset` 或 `Addressable`。
- `Resource Helper Type` 默认指向 `LFramework.Runtime.YooAssetResourceHelper`。
- YooAsset 模式下通过 `Packages` 配置多包。
- `Default Package Id` 为空时会使用第一个有效包。
- `Routing` 控制路由索引、路由包、路由资产地址。
- `ValidateMultiPackageConfiguration` 会检查包 ID、包名、fallback 引用、默认包和路由包。

## 构建配置重点

`BuildSetting` 是编辑器构建入口使用的参数对象：

- `builderTarget`：目标平台。
- `buildType`：`App` 或 `ResourcesUpdate`。
- `isRelease`：Debug/Release 模式。
- `appVersion` 和 `versionCode`：应用版本。
- `isBuildResources`：App 构建时是否顺带构建资源。
- `isResourcesBuildIn`：资源是否内置。
- `resourcesVersion`：资源版本。
- `cdnType`：CDN 类型。
- `isbuildDll`：是否构建热更 DLL。

`BuildOrchestrator` 会读取当前 `ResourceComponentSetting.ResourceMode`，写入 `BuildSetting.ResourceSystem`，再选择 App 或资源更新构建管线。

## 配置变更检查表

1. 修改配置资产后，确认它是否被 `ProjectSettingSelector` 引用。
2. 新增 ComponentSetting 后，确认 `bindTypeName` 指向正确运行时组件。
3. 新增资源包后，执行 `ValidateMultiPackageConfiguration` 或在 Inspector 中确认无错误。
4. Player 运行失败时，优先确认 `ProjectSettingSelector` 是否能从 Resources 读取。
5. 构建参数变更时，同时检查 `BuildSetting`、平台 `IPlatformConfig`、`BuildGameSettingTask`。

