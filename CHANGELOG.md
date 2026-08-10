# Changelog

All notable changes to this package are documented in this file.

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
