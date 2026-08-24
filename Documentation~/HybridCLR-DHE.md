# HybridCLR DHE Integration

LFramework supports both the standard HybridCLR hot-update loader and the commercial Differential Hybrid Execution (DHE) MetaVersion workflow. DHE support is opt-in so projects using the community package continue to compile unchanged.

## Install the commercial package

Install the licensed `com.code-philosophy.hybridclr` package in the consuming Unity project. A local package reference can be used while validating a package checkout:

```json
{
  "dependencies": {
    "com.code-philosophy.hybridclr": "file:C:/path/to/com.code-philosophy.hybridclr",
    "com.lframework.core": "file:C:/path/to/lframework"
  }
}
```

Run the HybridCLR installer after replacing the package so its matching `libil2cpp` files are installed. Keep the commercial package version and installed `libil2cpp` version aligned.

## Enable commercial APIs

Add the following scripting define to every assembly that compiles LFramework, for example through the Player scripting defines or a project-level `Assets/csc.rsp`:

```text
-define:LFRAMEWORK_HYBRIDCLR_DHE
```

`HybridCLR_SUPPORT` remains managed by HybridCLR. `LFRAMEWORK_HYBRIDCLR_DHE` explicitly enables commercial-only APIs because community and commercial packages can share the same package name and version.

Do not define `LFRAMEWORK_HYBRIDCLR_DHE` in a project that uses the community package.

## Configure LFramework

In the project's `HybridCLRSetting` asset:

1. Set `loadMode` to `DifferentialHybridMetaVersion`.
2. Fill `differentialHybridAssembliesSort` in dependency order. Dependencies must precede assemblies that reference them.
3. Set `logicMainDllName` to the assembly containing the hotfix entry point.
4. Keep the default labels unless the resource pipeline requires different names:
   - code DLL: `code_dll`
   - current MetaVersion: `dhe_current_meta`
   - baseline MetaVersion: `dhe_original_meta`

Before a build, `BuildEnvironmentConfigurator` synchronizes this list into HybridCLR's `differentialHybridAssemblies` setting. Standard mode instead synchronizes the normal hot-update assembly definitions.

## Build the initial Player

Build an App with DLL generation enabled through the LFramework build pipeline. After the Player succeeds, the post-process task creates the target-specific AOT snapshot and baseline MetaVersion files under the build backup directory:

```text
<backup>/DHE/<BuildTarget>/AotSnapshot
```

Preserve this backup for every released Player version. Resource updates must use the snapshot created by the exact Player they target.

At runtime, when no `code_dll` patch label exists, LFramework calls `LoadOriginalDifferentialHybridAssembly` and runs the DHE assemblies embedded in the Player.

## Build a resource update

Use the same platform, development setting, assembly list, and Player backup, then run a ResourcesUpdate build with DLL generation enabled. The pipeline:

1. Compiles the current DHE assemblies.
2. Generates current MetaVersion files against the released Player snapshot.
3. Copies the encrypted DLLs, current MetaVersion files, and original MetaVersion files into the configured hotfix output folder.
4. Registers all three artifact sets with Addressables or YooAsset using their configured labels.

A DLL patch without both matching MetaVersion files is rejected at build time and at runtime. This is intentional; falling back after discovering a partial patch could execute incompatible code and metadata.

## Standard mode

For a project using ordinary HybridCLR:

1. Do not define `LFRAMEWORK_HYBRIDCLR_DHE`.
2. Set `loadMode` to `Standard`.
3. Configure `hotfixAssembliesSort` and `logicMainDllName` as before.

The existing AOT metadata and `Assembly.Load` workflow remains active in this mode.

## Release checks

Before publishing a Player or patch, verify:

- Unity, HybridCLR package, and installed `libil2cpp` versions match the release matrix.
- The DHE scripting define is present in both runtime and editor assemblies.
- `ProjectSettings/HybridCLRSettings.asset` contains the expected differential assemblies.
- The baseline snapshot belongs to the exact released Player and target.
- The resource catalog contains the DLL, current MetaVersion, and original MetaVersion labels for a patch.
- A clean device can start both without a patch and with the intended patch.
