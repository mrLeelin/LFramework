# LFramework UPM Package Design

## Goal

Convert the current `Assets/Framework` standalone repository into a Unity Package Manager package that can be imported directly by other Unity projects, while preserving the existing internal directory structure.

## Confirmed Constraints

- Keep the current directory layout. Do not reorganize into `Runtime/`, `Editor/`, or `Tests/`.
- Ship all bundled framework code together in one package:
  - `Framework/LFramework`
  - `Framework/UniRx`
  - `Framework/UnityGameFramework`
  - `Framework/Zenject`
- Do not update the current demo project to consume the package in this task.
- Treat this as a long-term maintained package, not a one-off export.
- `GenerateBuildVersionHelper.cs` can be removed.

## Packaging Strategy

### Package root

Use the current standalone repository root as the package root:

- `Assets/Framework`

Add UPM metadata files at that root:

- `package.json`
- `README.md`
- `CHANGELOG.md`
- `LICENSE.md`
- `Third Party Notices.md`

### Package identity

Use a single package identity for now:

- Name: `com.lframework.core`
- Display name: `LFramework`

This keeps installation simple for consuming projects and matches the requirement of shipping the full framework as one package.

## Code Changes Required

### 1. Remove the hard-coded asset path

`SettingSetupHelper.cs` currently assumes the framework lives under:

- `Assets/Framework/Framework/LFramework/Assets/Settings`

That breaks after the framework is installed as a package, because the path becomes:

- `Packages/com.lframework.core/Framework/LFramework/Assets/Settings`

The fix is to resolve the settings path relative to the `SettingSetupHelper.cs` script asset path, not relative to `Assets/`.

### 2. Remove obsolete build-version generator

`GenerateBuildVersionHelper.cs` writes to an outdated fixed path and is no longer required for the package conversion. It should be deleted instead of adapted.

### 3. Keep assembly identities stable

Do not rename asmdefs in this task. Keep:

- `LFramework.Runtime`
- `LFramework.Editor`
- `LFramework.Hotfix`
- `GameFramework`
- `UnityGameFramework.Runtime`
- `UnityGameFramework.Editor`
- `UniRx`
- `Zenject`

This minimizes migration risk for consuming projects, serialized references, and HybridCLR-related configuration.

## Dependencies

Declare the package's external UPM dependencies in `package.json`:

- `com.code-philosophy.hybridclr`
- `com.cysharp.unitask`
- `com.tuyoogame.yooasset`
- `com.unity.addressables`
- `com.unity.mobile.notifications`
- `com.unity.ugui`
- `com.unity.test-framework`

Keep versions aligned with the current demo project's manifest where possible.

## Testing Strategy

- Add an editor test that verifies the settings path is resolved from the `SettingSetupHelper.cs` script location instead of a hard-coded `Assets/Framework` path.
- Run the specific editor test first and confirm it fails before implementation.
- Run the same test again after implementation and confirm it passes.
- Run a Unity compilation check after all code changes.

## Non-Goals

- Do not migrate the demo project to package consumption.
- Do not refactor framework folder layout.
- Do not rename assemblies.
- Do not rework unrelated editor window files already modified in the dirty worktree.
