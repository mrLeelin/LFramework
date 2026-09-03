# HybridCLR DHE Integration

This file is the compatibility entry point for the current HybridCLR integration. The
implementation uses automatic source detection; it does not expose a manual DHE switch,
does not write a runtime mode flag, and does not use reflection to call DHE APIs.

## Edition selection

When Unity finds `HybridCLR.Editor` source containing
`Editor/DHE/MetaVersionWorkflow.cs`, the editor automatically adds the internal
`LFRAMEWORK_HYBRIDCLR_DHE` define to the supported build targets. The build and Player
runtime then compile the flagship DHE path directly. When that source is absent, the
define is removed and the standard community HybridCLR path is compiled instead.

The source detector is intentionally strict: the assembly name and the DHE source path
must both match. It also preserves unrelated scripting defines and persists changes to
`ProjectSettings/ProjectSettings.asset`. Replacing or removing HybridCLR source files
triggers another synchronization after import.

## Configuration

Keep the existing `HybridCLRSetting.hotfixAssembliesSort` and
`logicMainDllName` configuration. The DHE-only labels are visible automatically in the
Inspector when the flagship source is present:

- `dheCurrentMetaLabel`
- `dheOriginalMetaLabel`

Do not add a `runtimeUseDheHybridClr` field or manually select a DHE mode. The build
pipeline synchronizes the configured assembly list into
`HybridCLRSettings.differentialHybridAssemblies` only in the flagship compile path.

## Build flow

For the flagship edition, an App build must build both DLLs and resources. It writes
one-byte DHE placeholders into the Player resource folders, then creates the target-
specific AOT snapshot and MetaVersion baseline after the Player build. A ResourcesUpdate
must also rebuild DLLs and resources using the exact backup snapshot belonging to the
released Player. DLL, current MetaVersion, and original MetaVersion artifacts are all
registered with the configured Addressables or YooAsset registrar.

For the community edition, the existing `PrebuildCommand.GenerateAll`, AOT metadata,
generic-reference, and ordinary hot-update DLL flow remains active. DHE types and
`differentialHybridAssemblies` are excluded at compile time, so the community build does
not depend on flagship-only source files.

Both editions require the normal HybridCLR package setup. A detected flagship source
tree additionally requires HybridCLR and local `libil2cpp` version `8.11.1`, together
with the installed DHE native files. A mismatch fails the build with an explicit error;
it never silently falls back to the community path.

## Runtime flow

In the Unity Editor, hot-fix assemblies are loaded from the current AppDomain. In a
Player compiled with the flagship define, `HotfixComponent` uses DHE APIs directly:

- built-in or local resources load the original Player DHE images;
- remote resources may provide encrypted differential DLLs;
- a patch DLL is accepted only when both current and original MetaVersion assets exist;
- missing or invalid artifacts return a hot-fix error rather than executing an incomplete
  patch.

A community Player loads AOT metadata first and then ordinary hot-update DLLs for remote
resources. Built-in or local resources retain the existing Editor assembly loading path.

## Detailed reference

See [`Documentation~/ai/HYBRIDCLR_DHE_AUTO_EDITION_2026-09-02.md`](ai/HYBRIDCLR_DHE_AUTO_EDITION_2026-09-02.md)
for the implementation map, verification evidence, and release checklist.
