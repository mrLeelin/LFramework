# World Runtime Preview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a detailed, read-only World and WorldHelper runtime panel in Game Window.

**Architecture:** Add an Editor-only snapshot API beside `WorldBase`, then consume it from a new auto-discovered `ProfiledBase` page. Resolve active worlds from LServices scopes so concrete and interface aliases are deduplicated.

**Tech Stack:** Unity 6 Editor IMGUI, LFramework runtime world system, NUnit Editor tests.

---

### Task 1: Lock Snapshot Behavior

**Files:**
- Create: `Framework/LFramework/Tests/Editor/World/WorldDiagnosticsSnapshotTests.cs`
- Create: `Framework/LFramework/Scripts/Runtime/World/WorldDiagnosticsSnapshot.cs`
- Modify: `Framework/LFramework/Scripts/Runtime/World/WorldBase.cs`

- [x] Write a test world that registers one helper implementing a custom interface, `IWorldUpdate`, and `IWorldLateUpdate`.
- [x] Assert one unique helper, its binding interface, registration key, implemented interfaces, and callback flags.
- [x] Run the targeted Editor test and confirm it fails because the snapshot API is missing.
- [x] Add the minimal Editor-only snapshot types and `WorldBase.GetDiagnosticsSnapshot()` implementation.
- [x] Run the targeted test and confirm it passes.

### Task 2: Add the Profiled Page

**Files:**
- Create: `Framework/LFramework/Scripts/Editor/Window/Profiled/WorldProfiled.cs`
- Create: `Framework/LFramework/Scripts/Editor/Window/Profiled/WorldProfiledDataSource.cs`
- Create: `Framework/LFramework/Tests/Editor/Window/WorldProfiledDataSourceTests.cs`

- [x] Write a data-source test that proves service aliases deduplicate to one World instance.
- [x] Run it and confirm the missing data source causes the expected failure.
- [x] Implement scope traversal and immutable page models.
- [x] Render summary cards, World details, searchable Helper cards, interface lists, and lifecycle badges.
- [ ] Run the final targeted tests. The final fixtures compile, but the Unity Test Runner queue is occupied by a timed-out synchronous all-mode run.

### Task 3: Verify Unity Integration

**Files:**
- Verify all files above and their Unity `.meta` files.

- [x] Compile the final Runtime, Editor, and Editor Tests assemblies with Unity's generated Roslyn response files.
- [ ] Run the two final targeted Editor test fixtures in Unity Test Runner.
- [x] Verify the final three assembly layers report no compile errors.
- [x] Reopen Chinese-bearing files and scan for `???`, replacement characters, or escaped Unicode regressions.
