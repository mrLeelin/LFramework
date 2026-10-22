# Changelog

All notable changes to this package are documented in this file.

## [0.3.18]

### Fixed

- 修复循环计时器不触发、暂停后恢复提前到期、清空后 ID 复用以及回调重入导致重复派发的问题；一次更新最多同步处理两轮，防止循环回调反复缩短到期时间时卡住主线程，并保护暂停恢复时的时间戳上限。
- 资源句柄提前释放时立即取消等待，但延迟到底层操作完成及回调分发结束后再归还对象池，避免旧操作完成时污染复用后的句柄；补齐 Addressables 与 YooAsset 加载失败的释放路径。
- 修复 YooAsset 按标签批量创建句柄时，中途抛异常造成已创建句柄泄漏的问题，失败时释放此前取得的全部句柄。
- Entity 异步等待覆盖加载取消、组件关闭及晚到回调；在 `OnInit`/`OnShow` 中取消时，等待显示回调退出并清理旧实体后才完成取消，允许 continuation 安全复用同一实体 ID。
- 修复 UI 对象池复用时的组状态、打开失败回滚和关闭异常清理；子窗口禁用后仍完成关闭、取消订阅和资源释放。
- 修复 UI/Entity 子模块部分订阅成功后抛异常导致的事件残留；订阅失败自动撤销，清理失败保留异常并支持重试，重入取消延迟到当前订阅结束。
- 单例注册失败时回滚，阻止重复启动清空现有服务，并拦截 `Register()` 内同类型同步重入，失败后允许重新注册。
- Provider 与 World 清理保留失败项以便重试；World Helper 注册前检查接口冲突；同步字段缓存同时区分 DTO 类型与 Provider 类型。
- 网络队列跟踪延期重试并在清空时撤销计时器、释放请求消息；资源下载失败分类使用清理前保存的失败信息。

### Added

- 增加资源句柄、Entity 取消、UI 生命周期、计时器、网络重试、启动、Provider/World 清理及下载失败回归测试。
- 增加循环计时器回调重入、同 ID Entity 取消后重新加载、部分订阅失败与清理重试、单例注册重入以及批量句柄创建异常的专项回归测试。

### Upgrade Notes

- 更新 Runtime、GameFramework 及相关 Hotfix 调用程序集后重新编译；保留原有不带取消令牌的 Entity 异步扩展方法签名。
- 计时器回调中递归请求的更新最多在当前更新内追加一轮，其余工作由后续更新处理；Entity 显示回调内发起的取消在当前显示回调返回并完成清理后通知等待方。

## [0.3.17]

### Fixed

- 修复组件设置转换时，仅供辅助器（Helper）直接读取、不参与组件字段映射的派生设置字段被误判为缺失并反复输出 Editor 错误的问题：`ResourceComponentSetting` 的 6 个 YooAsset 多包/路由/Hotfix 字段与 `LocalizationComponentSetting` 的 5 个本地化字段现在通过 `[ComponentSettingMappingIgnore]` 显式声明跳过映射，不再产生错误日志。
- 组件设置字段映射诊断改为 `UnityEngine.Debug.LogError`（仅 Editor 编译），避免在未定义 `ENABLE_LOG`/`ENABLE_ERROR_AND_ABOVE_LOG` 等日志宏的工程中被整体编译掉，导致真实映射错误静默消失；诊断信息现在提示可用 `[ComponentSettingMappingIgnore]` 显式忽略。
- 组件字段查找扩展为沿完整继承链查找（含 public 字段与基类私有字段），修复 public 字段与继承字段无法映射的问题。
- 修复 `ResourceComponentSetting.m_CustomResourceHelper` 与 `ResourceComponent.m_CustomResourceHelper` 字段类型不一致（`SettingHelperBase` 与 `ResourceHelperBase`）导致该字段被静默跳过的问题，自定义 Resource Helper 现在可以通过设置资产生效。

### Added

- 新增组件设置字段映射回归测试，覆盖显式忽略字段、未标记缺失字段的 Editor 诊断、继承链与 public 字段映射、真实模板资产转换，以及设置字段与组件字段的类型对称性。

### Upgrade Notes

- 仅供辅助器读取、不参与组件字段映射的设置字段需标注 `[ComponentSettingMappingIgnore]`；未标注且组件上确实不存在的字段仍会在 Editor 输出错误日志。
- `ResourceComponentSetting.m_CustomResourceHelper` 的序列化类型由 `SettingHelperBase` 调整为 `ResourceHelperBase`；已有设置资产中该字段为空引用（`fileID: 0`），无需迁移。

## [0.3.16]

### Fixed

- 修复组件设置转换时将 `ComponentSetting` 基类的 `settingId` 等元数据误当作组件参数，导致 Editor 启动时输出字段缺失错误的问题。
- 在参数映射前统一排除基类元数据，保留派生设置类的组件参数赋值、字段存在性和类型检查。

### Added

- 增加组件设置转换回归测试，覆盖空标识、自定义标识以及真实组件参数的正常赋值。

## [0.3.15]

### Added

- 为 UI Groups 新增“不使用对象池”开关（`DisableInstancePool`），默认关闭；开启后，组内界面每次打开均创建新实例，关闭后执行回收生命周期并直接释放资源和销毁实例。
- 为 `UIComponent.AddUIGroup` 和 `IUIManager.AddUIGroup` 增加带对象池开关的重载，并通过 `IUIGroup.DisableInstancePool` 提供只读查询。
- 增加 UI 实例对象池回归测试，覆盖配置序列化、重新打开、加载取消、打开失败、跨组隔离、重复关闭、退出异常及重入清理。

### Fixed

- 修复加载中关闭界面时，加载完成的实例未正确传入释放辅助器的问题，并确保加载请求记录在成功、失败和取消路径中释放。
- 退出时清理待回收和未入池实例；单个界面的关闭或回收回调抛出异常时，仍继续清理其他实例，并在完成清理后汇总异常。
- 对象池实例回收回调失败后直接释放，避免后续打开复用未完成清理的实例。
- 默认 UI 释放辅助器保留原资源辅助器的释放入口，以支持资源组件关闭后晚到的实例释放。

### Upgrade Notes

- 已有 UI Groups 配置和原 `AddUIGroup` 调用继续使用对象池；组级开关在创建组时确定，不支持运行中切换。
- 自定义 `IUIGroup` 实现需补充 `DisableInstancePool` 只读属性；自定义 `IUIManager` 实现需补充新增的 `AddUIGroup` 重载。升级后需重新编译相关调用程序集，包括 Hotfix 程序集。

## [0.3.14]

### Added

- 新增 World Resident 生命周期，支持跨指定 World 保留业务对象、服务注入、更新回调和安全释放。
- 新增应用关闭接口与关闭重试路径，确保 Hotfix 业务清理先于底层 Runtime 组件关闭。
- 增加 World Resident、Hotfix 流程切换、Provider 清理和应用关闭的 EditMode 回归测试。

### Fixed

- 修复 Provider 关闭时未完整取消事件订阅的问题。
- 修复 World Resident 转换在验证或进入阶段失败后残留未完成事务的问题。
- 修复 Runtime 组件关闭失败后无法保留依赖并重试清理的问题。
## [0.3.13]

### Changed

- 为 `Debug`、`Info`、`Warning`、`Error` 和 `Fatal` 的泛型格式化日志重载补充调用位置：在保留原有方法签名与复合格式化行为的前提下，日志会附带调用方法、文件路径和行号。
- 日志辅助器未注册时，格式化日志入口会直接返回，避免为不会输出的日志创建调用栈与格式化缓存。

### Added

- `GameFrameworkLog.HasLogHelper` 用于查询当前是否已注册日志辅助器，供日志入口在写入前快速判断。

## [0.3.12]

### Added

- 为 Debug、Info、Warning、Error、Fatal 增加对应的 Tag 日志入口，支持字符串标签、泛型类型名和实例类型名，并自动记录调用方法、文件路径及行号。

### Changed

- 普通 string/object 日志入口使用 CallerMemberName、CallerFilePath、CallerLineNumber 自动附加调用位置；Assert 显式透传业务调用位置。
- 日志入口增加 DebuggerHidden 和 DebuggerStepThrough，单步调试时跳过日志实现。
- 保留默认无 Tag 调用、日志级别条件编译和原有格式化重载。带标签使用 DebugTag、InfoTag、WarningTag、ErrorTag、FatalTag，避免与格式化参数发生重载冲突。

### Upgrade Notes

- 现有格式化重载保持原有行为，不自动追加调用位置；需要 Tag 和调用位置时，可将插值字符串传入对应的 Tag 入口。
- 普通日志与 Assert 的方法签名新增可选参数；升级框架后需要重新编译调用程序集，包括 Hotfix 程序集。

## [0.3.11]

### Added

- 新增 11 项 Provider 排序与生命周期 EditMode 回归测试，覆盖同值稳定排序、跨数据类型同步、跨流程注册、重复进入、注销、回滚及释放失败重试。

### Changed

- 统一 Provider 初始化各阶段、每帧 `UpdateComponent`、`ISyncServerData` 同步及正常注销、释放的排序规则：`BelongTo.Sort` 越大越先执行，相同优先级按注册先后稳定执行。
- 更新和同步共用全局有序 Provider 注册表；同步不再按数据类型分组，单对象和列表接收者可以按优先级交错执行，同类型数据在单次同步中仍只读取一次。
- 跨流程保留的 `Forever` Provider 与新注册 Provider 使用同一全局执行顺序；初始化仍只处理本批新增 Provider，不重复初始化已有实例。

### Fixed

- 正常清理成功后立即同步移除 Provider 查询索引和有序执行记录，避免后续对象清理失败导致已释放实例残留。
- 正常清理抛异常时保留失败 Provider 的重试入口；注册失败仍逆序回滚，并在清理前移除本批执行记录，避免残留更新或同步回调。

### Upgrade Notes

- 无需修改现有 Provider 接口或同步回调签名，但已有 `BelongTo.Sort` 现在也会影响更新、同步和正常清理顺序。有先后依赖的 Provider 应配置不同优先级，不要依赖同值时的发现顺序或旧的数据类型分组顺序。
- 本次不改变网络登录事件时序、登录标记透传、空数据跳过规则或账号状态重置逻辑。

## [0.3.10]

- Mac 使用 DHE 打包热更测试成功。

## [0.3.7]

### Added

- Embedded the licensed HybridCLR 8.11.1 Editor, Runtime, plugin, and installer data inside `com.lframework.core`.
- Added the Unity 6000.3.6f1 modified `Unity.IL2CPP.dll` and automatic SHA-256-verified installation for DHE builds.

### Changed

- Consumers now install HybridCLR through `com.lframework.core` without a separate `com.code-philosophy.hybridclr` package reference.

## [0.3.6]

### Changed

- Updated the commercial HybridCLR DHE integration contract and validation baseline from flagship 8.3.0 to flagship 8.11.1.

## [0.3.5]
- 增加 EntityComponent 和 UiComponent  的语法糖更容易获取到 EntityLogic 或者 Window

## [0.3.4]

### Added

- Added live Editor diagnostics for registered providers, worlds, world helpers, and service scopes.
- Added EditMode coverage for diagnostic snapshots, profiled data sources, and duplicate world-helper registrations.

### Changed

- Bounded profiled-window repaint frequency and made runtime preview panels tolerate framework bootstrap and shutdown states.
- Exposed Entity component initialization state so diagnostics do not inspect it before its framework module is ready.

## [0.3.3]

### Added

- Added EditMode regression coverage for provider and world registration rollback, partial subscription cleanup, retryable unsubscribe failures, and deterministic event IDs.

### Changed

- Added an awaitable Hotfix assembly-loading API and updated the launch pipeline to await it directly while preserving the original callback entry point.
- Changed generic game-event IDs to use a deterministic type-name hash.

### Fixed

- Rolled back provider services, subscriptions, injection caches, and pooled instances when any registration lifecycle stage fails.
- Rolled back world services, injection caches, and pooled instances when world initialization fails.
- Made Addressables dependency downloads awaitable, retry failed operations safely, and report terminal download failure instead of leaving the launch flow pending.
- Guaranteed successful-download event objects and auto-release handlers are cleaned up when a subscriber throws.

## [0.2.58]

### Fixed

- Skipped object-pool unspawn when an Entity handle was already destroyed during shutdown teardown.
- Changed scene ResourceHandle disposal to use the Addressables scene unload flow, preventing duplicate handle release when exiting PlayMode.

## [0.2.57]

### Added

- Forwarded Unity application focus, pause, and quit callbacks to Hotfix-layer `GameFrameworkComponent` instances through a runtime lifecycle contract.
- Added EditMode regression coverage for Runtime-to-Hotfix lifecycle dispatch and lifecycle-service cleanup.

## [0.2.56]

### Changed

- Refactored version-check launch configuration behind `ICheckVersionConfigProvider` so framework tasks no longer hardcode whitelist, version URL, JSON parsing, comparison, or remote-setting application rules.
- Split version data contracts into `IGameVersionConfig` and `IGameVersionEndpointConfig` so runtime endpoint fields are optional capabilities instead of required version fields.

## [0.2.55]

### Fixed

- Applied the configured app version to Android and iOS player bundle versions during package builds.

## [0.2.54]

### Added

- Added lightweight `DownloadAssets` handlers for Addressables and YooAsset so asset predownload no longer reuses the hot-update download pipeline.
- Added a `DownloadAssets(keys, packageId)` overload for YooAsset multi-package resource predownload.

### Fixed

- Fixed Addressables resource-helper compatibility with the current `ResourceLocators` and `IResourceLocation` APIs.
- Ensured YooAsset asset predownload initializes the target package before starting the lightweight download flow.

## [0.2.53]

### Added

- Added `SequenceLineComponent` dynamic chunk insertion by target serial id, with before/after placement support.
- Added EditMode coverage for SequenceLine relative insertion, explicit group insertion serial ids, and failed insertion id stability.

### Fixed

- Kept SequenceLine serial id assignment consistent across normal insertion, explicit group insertion, and relative insertion.
- Prevented failed relative insertion from consuming a SequenceLine serial id.
- Removed completed SequenceLine chunks by their actual linked-list node so dynamic insertion does not cause the wrong chunk to be removed.

## [0.2.52]

### Fixed

- Hardened Addressables asset-handle release against duplicate or invalid-handle disposal during Editor PlayMode shutdown.
- Guarded UI and entity teardown paths against destroyed Unity objects during manual Stop PlayMode.
- Skipped nonessential UI pause, cover, refresh, and close-complete callbacks while the UI manager is shutting down.

## [0.2.51]

### Added

- Added separate Android Debug and Release keystore settings and applied the matching keystore during APK/AAB builds.
- Added EditMode coverage for Android keystore selection and build-mode validation.

### Changed

- Android Project export no longer requires Unity keystore validation because signing is handled outside Unity for exported Gradle projects.

## [0.2.50]

### Added

- Added a `Log Load Urls` resource setting that logs resolved remote load URLs for both Addressables and YooAsset, including dependency and package-internal URL requests.
- Added URL log details for backend, asset identity, final URL, and compact stack traces to help locate unexpected remote resource dependencies.

## [0.2.40]

### Changed

- Trim iOS string settings when read so signing, provisioning, export, and upload values ignore accidental leading or trailing whitespace.

## [0.2.36]

### Added

- Added configurable iOS signing, provisioning profile, capability, privacy, ExportOptions, and IPA export settings.
- Added EditMode coverage for iOS signing validation, Xcode export options generation, archive script generation, and Info.plist privacy output.

### Changed

- Hardened iOS Xcode post-processing to apply manual signing, arm64-only builds, Swift embedding settings, bitcode disabling, and UnityFramework linker flags.
- Made iOS build validation fail early when signing settings are missing or still use placeholder values.
- Made CocoaPods and IPA export execution macOS-aware so Windows project generation does not try to run Xcode tooling.

## [0.2.31]

### Added

- Added project-owned platform config registry providers so consumer projects can take over the full `BuilderTarget -> IPlatformConfig` mapping with pure code while keeping a framework default provider as fallback.
- Added EditMode coverage for default provider fallback, custom provider priority selection, provider conflicts, unsupported targets, and null-return failures.

### Changed

- Refactored `PlatformConfigFactory` to discover active registry providers by priority, reject provider conflicts explicitly, and report unsupported targets or null config results with clearer diagnostics.
- Updated `HostLinkXmlInstaller` to maintain the host `Assets/link.xml` automatically on editor load and before player builds instead of relying on a manual menu action.

## [0.2.14]

### Added

- Added `ProjectSettingSelector` quick actions in `GameWindow` to collect all project-owned settings automatically and run selector validation without manual dragging.

## [0.2.13]

### Changed

- Improved `GameWindow` embedded setting inspector interaction so popup-like fields such as `Bind Component` respond more reliably inside the Odin-hosted editor shell.

## [0.1.0]

### Added

- Added a Unity Package Manager `package.json` at the repository root.
- Added a package compatibility test for the settings-path resolver.

### Changed

- Preserved the original `Framework/...` layout for package consumption.
- Updated `SettingSetupHelper` to resolve the settings folder from the script location instead of a fixed `Assets/Framework/...` path.

### Removed

- Removed `GenerateBuildVersionHelper` because it depended on an obsolete hard-coded project path.
