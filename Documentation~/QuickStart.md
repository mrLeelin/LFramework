# Quick Start

This guide gets `com.lframework.core` into a new Unity project with the minimum moving parts needed to boot the framework.

## 1. Prerequisites

Install these dependencies before importing the package:

- `com.code-philosophy.hybridclr`
- `com.cysharp.unitask`
- Odin Inspector / Sirenix runtime and editor assemblies
- Scoped registry support for `com.tuyoogame.yooasset`

See `Dependencies.md` for details.

## 2. Install the package

Add the package from a local path, embedded package, or Git URL.

Example:

```json
{
  "dependencies": {
    "com.lframework.core": "https://your.git.url/LFramework.git"
  }
}
```

## 3. Create the bootstrap scene

Create an empty scene and add a root GameObject such as `App`.

Attach:

- `LFramework.Runtime.LSystemApplicationBehaviour`

This is the standard startup MonoBehaviour for runtime projects.

## 4. Initialize project settings

Open:

- `LFramework/GameSetting`

Then initialize the framework project settings so a `ProjectSettingSelector` asset is created. The runtime boot sequence depends on this asset.

## 5. Configure components

`LSystemApplicationBehaviour` uses its serialized `allComponentTypes` list to instantiate framework components.

At minimum, verify your scene/project includes the components required by your game flow. Typical starter sets include:

- Event
- Procedure
- Resource
- UI

Pick the actual component type names from your framework setup rather than hard-coding guesses.

## 6. Add an entrance procedure

Create a procedure deriving from `RuntimeBaseProcedure` and make sure your procedure system can enter it as the initial flow.

Use `Samples~/ProcedureFlow` as the reference for recommended responsibilities.

## 7. Press Play

Expected result:

- `LSystemApplicationBehaviour` starts
- settings are bound into Zenject
- configured `GameFrameworkComponent` instances are created and injected
- startup completes without missing selector / missing component errors

## First things to read next

1. `Architecture.md`
2. `PublicAPI.md`
3. `ExtensionPoints.md`
4. `Samples~/MinimalBootstrap/README.md`
