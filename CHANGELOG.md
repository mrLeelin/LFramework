# Changelog

All notable changes to this package are documented in this file.

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
