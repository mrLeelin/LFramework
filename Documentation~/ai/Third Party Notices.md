# Third Party Notices

This package bundles or depends on third-party components.

## Bundled in this repository

### Zenject

- Location: `Framework/Zenject`
- Bundled license file: `Framework/Zenject/LICENSE.txt`

### UnityGameFramework / Game Framework

- Location: `Framework/UnityGameFramework`
- Bundled license file: `Framework/UnityGameFramework/LICENSE.md`

### UniRx

- Location: `Framework/UniRx`
- Note: this repository snapshot does not currently include a local UniRx license file. Confirm upstream licensing before external redistribution.

### HybridCLR

- Location: `Framework/HybridCLR`
- Version: licensed flagship 8.11.1 with DHE support
- Bundled license file: `Framework/HybridCLR/LICENSE`

## External prerequisites not fully declarable from this package manifest

### UniTask

- Current source project dependency: `com.cysharp.unitask`
- Current source project install mode: Git dependency in the consuming project's `manifest.json`

### Odin Inspector / Sirenix

- This package uses Sirenix runtime and editor APIs in both runtime and editor code.
- Install the required Odin assemblies in the consuming project before importing this package.
