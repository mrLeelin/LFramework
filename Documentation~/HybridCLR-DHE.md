# HybridCLR DHE Integration

LFramework supports both the standard HybridCLR hot-update loader and the commercial Differential Hybrid Execution (DHE) MetaVersion workflow. This integration is locked to the licensed flagship HybridCLR 8.3.0 package and its matching official `libil2cpp` release matrix. Do not mix it with community packages or other flagship versions.

## Install the commercial package

Install the licensed `com.code-philosophy.hybridclr` 8.3.0 package in the consuming Unity project. A local package reference can be used while validating a package checkout:

```json
{
  "dependencies": {
    "com.code-philosophy.hybridclr": "file:C:/path/to/com.code-philosophy.hybridclr",
    "com.lframework.core": "file:C:/path/to/lframework"
  }
}
```

Run the HybridCLR 8.3.0 installer after replacing the package so the `libil2cpp` branch selected by the 8.3.0 release matrix is installed. The installer must report both the package and installed `libil2cpp` version as `8.3.0` before building.

## Commercial API detection

No project scripting define is required. LFramework detects the flagship 8.3.0 API surface at runtime and in the editor. This is necessary because community and flagship packages can use the same package name and version.

If `DifferentialHybridMetaVersion` is selected while the installed package does not expose the flagship APIs, the build fails before generating artifacts instead of silently switching to standard HybridCLR.

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

Build an App with both DLL and resource generation enabled through the LFramework build pipeline. DHE App builds reject any other combination because a Player assembled from stale resources cannot have a trustworthy baseline. Before the resource build, LFramework writes one-byte placeholder DLL and MetaVersion assets. Existing files are overwritten in place to retain their Unity GUIDs, while artifacts for assemblies removed from the configuration are deleted. These placeholders prevent an old local patch from being embedded into the new Player. After the Player succeeds, the post-process task creates the target-specific AOT snapshot and baseline MetaVersion files under the build backup directory:

```text
<backup>/DHE/<BuildTarget>/AotSnapshot
```

Preserve this backup for every released Player version. Resource updates must use the snapshot created by the exact Player they target.

At runtime, a missing `code_dll` label or a one-byte placeholder causes LFramework to call `LoadOriginalDifferentialHybridAssembly` and run the DHE assemblies embedded in the Player. A resource update may patch only a subset of the configured assemblies; the remaining assemblies continue to use their original Player versions.

## Build a resource update

Use the same platform, development setting, assembly list, and Player backup, then run a ResourcesUpdate build with DLL generation enabled. The pipeline:

1. Compiles the current DHE assemblies.
2. Generates current MetaVersion files against the released Player snapshot.
3. Copies the encrypted DLLs, current MetaVersion files, and original MetaVersion files into the configured hotfix output folder.
4. Registers all three artifact sets with Addressables or YooAsset using their configured labels.

A DLL patch without both matching MetaVersion files is rejected at build time and at runtime. This is intentional; falling back after discovering a partial patch could execute incompatible code and metadata.

## Standard mode

For a project using ordinary HybridCLR:

1. Set `loadMode` to `Standard`.
2. Configure `hotfixAssembliesSort` and `logicMainDllName` as before.

The existing AOT metadata and `Assembly.Load` workflow remains active in this mode.

## Release checks

Before publishing a Player or patch, verify:

- The licensed package version and installed `libil2cpp` version both report `8.3.0`, and the selected native branches match the 8.3.0 Unity release matrix.
- `HybridCLRBuildWorkflow.IsDheAvailable` is true for the installed package.
- `ProjectSettings/HybridCLRSettings.asset` contains the expected differential assemblies.
- The baseline snapshot belongs to the exact released Player and target.
- The resource catalog contains the DLL, current MetaVersion, and original MetaVersion labels for a patch.
- A clean device can start both without a patch and with the intended patch.
