# MinimalBootstrap Sample

Goal: boot `LFramework` in the smallest possible consumer setup.

## What this sample demonstrates

- a generated scene: `Scenes/MinimalBootstrap.unity`
- a self-contained `ProjectSettingSelector` reference on the sample application behaviour
- configuring the minimum framework components required for procedure startup
- reaching a first entrance procedure cleanly

## Included scripts

- `Runtime/MinimalBootstrapApplicationBehaviour.cs`
- `Runtime/MinimalBootstrapProcedure.cs`
- `Editor/MinimalBootstrapSampleInstaller.cs`

## How to use

1. Import the sample from Package Manager.
2. Run `LFramework/Samples/Minimal Bootstrap/Install Sample`.
3. Open `Scenes/MinimalBootstrap.unity`.
4. Press Play.

## Success criteria

- Play mode starts without selector/configuration errors
- DI container is created
- configured components are registered
- first procedure is entered

## Recommended follow-up

Import this sample first, then continue with:

- `../ProcedureFlow`
- `../UIBasic`
