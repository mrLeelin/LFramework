# LFramework

`LFramework` is a Unity Package Manager package version of the current framework repository. This package intentionally preserves the original internal layout under `Framework/...` instead of reorganizing the code into `Runtime/`, `Editor/`, or `Tests/`.

## Package guide

- Quick start: `Documentation~/QuickStart.md`
- Architecture overview: `Documentation~/Architecture.md`
- Public API guidance: `Documentation~/PublicAPI.md`
- Extension guide: `Documentation~/ExtensionPoints.md`
- Dependency notes: `Documentation~/Dependencies.md`
- Upgrade notes: `Documentation~/UpgradeGuide.md`
- FAQ: `Documentation~/FAQ.md`

## Included source

- `Framework/LFramework`
- `Framework/UniRx`
- `Framework/UnityGameFramework`
- `Framework/Zenject`

## Included samples

- `Samples~/MinimalBootstrap`
- `Samples~/ProcedureFlow`
- `Samples~/UIBasic`
- `Samples~/HotfixMinimal`

## Installation

Add the package from a Git URL or an embedded/local package path.

Example Git entry in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.lframework.core": "https://your.git.url/LFramework.git"
  }
}
```

## Required prerequisites

Some dependencies cannot be declared fully from a package manifest because they are currently consumed from Git URLs or from non-UPM distribution channels in the source project.

Install these before importing `com.lframework.core`:

- `com.code-philosophy.hybridclr`
- `com.cysharp.unitask`
- Odin Inspector / Sirenix runtime and editor assemblies

## Registry requirement for YooAsset

This package declares `com.tuyoogame.yooasset` as a dependency. The consuming project must have a scoped registry that can resolve that package, for example:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.tuyoogame.yooasset"
      ]
    }
  ]
}
```

## Notes

- The package keeps the existing `Framework/...` directory layout on purpose.
- `SettingSetupHelper` now resolves its settings folder relative to its script location instead of assuming `Assets/Framework/...`.
- `GenerateBuildVersionHelper` was removed because it wrote to an obsolete fixed project path.
- If you need to edit package-contained assets, prefer using an embedded or local package workflow.
- This package currently follows a documentation-first sample strategy: each sample includes runnable integration steps and starter code notes that can be imported into a consumer project.
