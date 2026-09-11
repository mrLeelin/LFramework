# Changelog

All notable changes to this package are documented in this file.

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
