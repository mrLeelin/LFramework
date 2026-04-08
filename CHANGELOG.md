# Changelog

## 0.2.13

- Improved `GameWindow` embedded setting inspector interaction so popup-like fields such as `Bind Component` respond more reliably inside the Odin-hosted editor shell.

## 0.1.0

- Added a Unity Package Manager `package.json` at the repository root.
- Preserved the original `Framework/...` layout for package consumption.
- Updated `SettingSetupHelper` to resolve the settings folder from the script location instead of a fixed `Assets/Framework/...` path.
- Added a package compatibility test for the settings-path resolver.
- Removed `GenerateBuildVersionHelper` because it depended on an obsolete hard-coded project path.
