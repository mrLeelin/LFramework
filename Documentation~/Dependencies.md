# Dependencies

`com.lframework.core` depends on both UPM packages and non-UPM-delivered tooling.

## Declared package dependencies

From `package.json`:

- `com.unity.test-framework`
- `com.unity.ugui`

## Required external dependencies

These are expected by the current package contents and integration patterns:

- `com.code-philosophy.hybridclr`
- `com.cysharp.unitask`
- Odin Inspector / Sirenix runtime and editor assemblies
- `com.tuyoogame.yooasset` via scoped registry

## Embedded third-party source in this package

The package currently preserves source for:

- `UniRx`
- `UniTask`
- `UnityGameFramework`
- `Zenject`

This means consumers should treat the package as shipping with opinions about those integrations rather than as a thin adapter layer.

## Consumer guidance

Before adoption, verify:

1. your Unity version matches package expectations (`6000.3.0f1`)
2. your project can resolve YooAsset through a registry
3. your project has the expected HybridCLR and Sirenix setup
4. your own copies of embedded third-party code will not conflict with this package layout

## Recommended compatibility policy

For team use, maintain an internal compatibility matrix covering:

- Unity version
- HybridCLR version
- YooAsset version
- Odin/Sirenix version
- package version

Keep that matrix in your release notes or package changelog process.
