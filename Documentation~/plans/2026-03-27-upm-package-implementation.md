# LFramework UPM Package Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the `Assets/Framework` standalone repository into a UPM package without changing its existing internal directory structure.

**Architecture:** Keep the current repository root as the package root, preserve the `Framework/...` subtree exactly as-is, and make only the minimum code changes needed for package compatibility. The only functional editor code change is to resolve settings asset paths from the script location instead of a fixed `Assets/Framework` path.

**Tech Stack:** Unity 6000.3.0f1, C#, Unity Package Manager, asmdef-based assemblies, NUnit editor tests

---

## Chunk 1: Path Compatibility

### Task 1: Lock the package-path behavior with a failing editor test

**Files:**
- Create: `Framework/LFramework/Tests/Editor/SettingSetupHelperTests.cs`

- [ ] **Step 1: Write the failing test**

Add a test that loads `LFramework.Editor`, finds `LFramework.Editor.Settings.SettingSetupHelper`, and invokes a non-public static method that resolves the settings asset path. Assert that the returned path matches the path derived from the script asset location and that the folder exists.

- [ ] **Step 2: Run test to verify it fails**

Run the Unity EditMode test for `LFramework.Editor.Tests.SettingSetupHelperTests`.
Expected: FAIL because the resolver method does not exist yet.

### Task 2: Implement path resolution without changing the folder structure

**Files:**
- Modify: `Framework/LFramework/Scripts/Editor/Settings/SettingSetupHelper.cs`

- [ ] **Step 1: Add the minimal implementation**

Add a non-public static method that:
- resolves `SettingSetupHelper.cs` through `AssetDatabase.GUIDToAssetPath`
- converts it to a project-relative path
- walks from `Scripts/Editor/Settings` back to `Assets/Settings`
- normalizes separators for Unity asset paths

Update all existing settings asset creation code to use the resolved path instead of the hard-coded constant.

- [ ] **Step 2: Run test to verify it passes**

Run the same Unity EditMode test again.
Expected: PASS

## Chunk 2: Package Metadata

### Task 3: Remove the obsolete generator

**Files:**
- Delete: `Framework/LFramework/Scripts/Editor/BuildPackage/GenerateBuildVersionHelper.cs`

- [ ] **Step 1: Delete the unused file**

Remove the obsolete helper instead of adapting its old fixed-path output logic.

- [ ] **Step 2: Verify there are no remaining references**

Search for `GenerateBuildVersionHelper`.
Expected: no remaining references.

### Task 4: Add package manifest and documentation

**Files:**
- Create: `package.json`
- Create: `README.md`
- Create: `CHANGELOG.md`
- Create: `LICENSE.md`
- Create: `Third Party Notices.md`

- [ ] **Step 1: Add package manifest**

Create `package.json` for `com.lframework.core` with Unity version compatibility and external dependency declarations.

- [ ] **Step 2: Add package docs**

Add concise documentation for installation, preserved folder layout, dependencies, and bundled third-party components.

## Chunk 3: Verification

### Task 5: Verify package conversion changes

**Files:**
- Verify only

- [ ] **Step 1: Run targeted EditMode test**

Run the new `SettingSetupHelperTests`.
Expected: PASS

- [ ] **Step 2: Run Unity compilation check**

Run a Unity compile and inspect the error log.
Expected: no new compile errors introduced by the package conversion changes.

- [ ] **Step 3: Review changed files**

Inspect git diff under `Assets/Framework`.
Expected: only package-conversion files and no accidental edits to the user's existing GameWindow work.
