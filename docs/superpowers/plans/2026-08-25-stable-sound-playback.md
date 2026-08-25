# Stable Sound Playback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve synchronous `PlaySound(...): int` calls while ensuring every accepted short non-looping request in a queue-enabled sound group starts exactly once, shares one in-flight asset load per path, and is cleaned up exactly once.

**Architecture:** Move resource ownership and pending-request scheduling into `GameFramework.Sound.SoundManager`. Use one `SoundAssetEntry` per asset path, one FIFO cadence lane per `(SoundGroup, path)`, and fixed existing sound agents. Keep legacy replacement behavior as enum value zero and opt only the package `UISound` template into reliable non-looping queueing.

**Tech Stack:** Unity 6000.3.0f1, C#, GameFramework module/event/reference-pool APIs, UnityGameFramework sound helpers, NUnit Editor tests, Unity Pipeline CLI.

**Approved spec:** `docs/superpowers/specs/2026-08-25-stable-sound-playback-design.md`

---

## File Map

Core contracts and policy:

- Create `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundOverflowPolicy.cs` for the public group policy.
- Modify `ISoundManager.cs` for additive configuration/group overloads and core cancellation notification.
- Modify `ISoundGroup.cs` to expose the configured overflow policy.
- Modify `PlaySoundErrorCode.cs` for queue-group loop rejection.
- Modify `PlaySoundFailureEventArgs.cs` to mark one primary failure in a shared-load failure batch.
- Create `PlaySoundCanceledEventArgs.cs` for silent pending-request cleanup.

Core manager internals:

- Modify `SoundManager.cs` for registries, shared callbacks, scheduling, cancellation, cache eviction, and shutdown.
- Rename `SoundManager.PlaySoundInfo.cs` to `SoundManager.SoundPlayRequest.cs` and preserve its `.meta` GUID.
- Create `SoundManager.SoundAssetEntry.cs` for one backend-owned resource reference per path.
- Create `SoundManager.SoundPlaybackLane.cs` for cadence-preserving FIFO scheduling.
- Modify `SoundManager.SoundGroup.cs` for idle-only versus replacement agent selection.
- Modify `SoundManager.SoundAgent.cs` so an agent borrows an entry and never unloads the asset directly.

Unity integration:

- Modify `SoundComponent.cs` for settings, group policy forwarding, cancellation cleanup, failure-log deduplication, and ordered shutdown.
- Modify `SoundComponent.SoundGroup.cs` to serialize the policy.
- Modify `DefaultSoundAgentHelper.cs` to distinguish pause from completion and restore the time setter.
- Modify `SoundComponentSetting.cs` and `SoundComponentSetting.asset` for cache defaults and the `UISound` policy.
- Delete the rejected untracked `SoundComponent.Preload.cs` and `.meta`, and remove its shutdown call without disturbing other sound changes.

Tests:

- Create `Framework/LFramework/Tests/Editor/Sound/SoundManagerTestDoubles.cs`.
- Create `SoundManagerSchedulingTests.cs` for shared loading, scheduling, cancellation, release, and shutdown.
- Create `DefaultSoundAgentHelperTests.cs` for pause/time behavior.
- Create `SoundComponentSettingsTests.cs` for reflection-based settings transfer and package-template defaults.

Use these fixed GUIDs for new Unity metadata; preserve `a470707910f5dd349bb5a73764c24580`
when renaming `SoundManager.PlaySoundInfo.cs.meta`:

| Asset | GUID |
| --- | --- |
| `SoundOverflowPolicy.cs` | `734ae039c2b64ce7949a467916703237` |
| `PlaySoundCanceledEventArgs.cs` | `1e53ee0da9c24c26bb48412621f5db66` |
| `SoundManager.SoundAssetEntry.cs` | `cb8dc159899c4763becfe69750403933` |
| `SoundManager.SoundPlaybackLane.cs` | `5249546e1f2b4dafa4e15e0511ec490d` |
| `Tests/Editor/Sound` folder | `c769bc33db534eb7a579d293ddfb404f` |
| `SoundManagerTestDoubles.cs` | `1d0c9ec017f34a74bd2a13bac91869b5` |
| `SoundManagerSchedulingTests.cs` | `b51584b3c5f6449d8196a0fa084c2255` |
| `DefaultSoundAgentHelperTests.cs` | `eae042eb91ba4d138996c16b10be7768` |
| `SoundComponentSettingsTests.cs` | `ed9fb6c2aeaa4319bb8a794d0c098bda` |

---

### Task 1: Lock the Public Policy and Build the Test Harness

**Files:**

- Create: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundOverflowPolicy.cs`
- Create: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundOverflowPolicy.cs.meta`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/ISoundGroup.cs`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/ISoundManager.cs`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/PlaySoundErrorCode.cs`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.cs`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.SoundGroup.cs`
- Create: `Framework/LFramework/Tests/Editor/Sound.meta`
- Create: `Framework/LFramework/Tests/Editor/Sound/SoundManagerTestDoubles.cs`
- Create: `Framework/LFramework/Tests/Editor/Sound/SoundManagerTestDoubles.cs.meta`
- Create: `Framework/LFramework/Tests/Editor/Sound/SoundManagerSchedulingTests.cs`
- Create: `Framework/LFramework/Tests/Editor/Sound/SoundManagerSchedulingTests.cs.meta`
- Delete: `Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.Preload.cs`
- Delete: `Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.Preload.cs.meta`
- Modify: `Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.cs`

- [ ] **Step 1: Remove only the rejected preload draft**

Delete the two untracked preload files and remove only this line from
`SoundComponent.ShutDown()`:

```csharp
ReleaseAllSoundPreloads();
```

Run `git diff -- SoundComponent.cs` immediately afterward. Expected: the preload
shutdown line is gone and no unrelated line in the dirty file changed.

- [ ] **Step 2: Write failing policy tests through the public interfaces**

Create a fixture that always isolates the static module registry:

```csharp
namespace LFramework.Editor.Tests.Sound
{
[SetUp]
public void SetUp()
{
    GameFrameworkEntry.Shutdown();
    _manager = GameFrameworkEntry.GetModule<ISoundManager>();
    _resourceManager = new FakeResourceManager();
    _soundHelper = new FakeSoundHelper();
    _manager.SetResourceManager(_resourceManager);
    _manager.SetSoundHelper(_soundHelper);
}

[TearDown]
public void TearDown()
{
    GameFrameworkEntry.Shutdown();
}

[Test]
public void LegacyAddSoundGroup_UsesReplacementPolicy()
{
    Assert.That(_manager.AddSoundGroup("Legacy", new FakeSoundGroupHelper()), Is.True);
    Assert.That(_manager.GetSoundGroup("Legacy").OverflowPolicy,
        Is.EqualTo(SoundOverflowPolicy.ReplaceByPriority));
}

[Test]
public void ExplicitAddSoundGroup_StoresQueuePolicy()
{
    Assert.That(_manager.AddSoundGroup(
        "Reliable", false, false, 1f,
        SoundOverflowPolicy.QueueNonLooping,
        new FakeSoundGroupHelper()), Is.True);
    Assert.That(_manager.GetSoundGroup("Reliable").OverflowPolicy,
        Is.EqualTo(SoundOverflowPolicy.QueueNonLooping));
}
}
```

The fake resource manager must record the type passed to normal sound loads and
allow deterministic completion:

```csharp
internal sealed class FakeResourceManager : IResourceManager
{
    internal sealed class PendingLoad
    {
        internal string AssetName;
        internal Type AssetType;
        internal int Priority;
        internal LoadAssetCallbacks Callbacks;
        internal object UserData;
    }

    internal readonly List<PendingLoad> Loads = new();
    internal readonly List<object> UnloadedAssets = new();

    public string ReadOnlyPath => string.Empty;
    public string ReadWritePath => string.Empty;
    public ResourceMode ResourceMode => ResourceMode.Unspecified;

    public void LoadAsset(string assetName, Type assetType, int priority,
        LoadAssetCallbacks callbacks, object userData)
    {
        Loads.Add(new PendingLoad
        {
            AssetName = assetName,
            AssetType = assetType,
            Priority = priority,
            Callbacks = callbacks,
            UserData = userData
        });
    }

    internal void CompleteSuccess(int index, object asset, float duration = 0.1f)
    {
        PendingLoad load = Loads[index];
        load.Callbacks.LoadAssetSuccessCallback(
            load.AssetName, asset, duration, load.UserData);
    }

    internal void CompleteFailure(int index, string message)
    {
        PendingLoad load = Loads[index];
        load.Callbacks.LoadAssetFailureCallback(
            load.AssetName, LoadResourceStatus.AssetError, message, load.UserData);
    }

    public void UnloadAsset(object asset) => UnloadedAssets.Add(asset);
    public void SetReadOnlyPath(string value) => throw new NotSupportedException();
    public void SetReadWritePath(string value) => throw new NotSupportedException();
    public void SetResourceMode(ResourceMode value) => throw new NotSupportedException();
    public void SetResourceHelper(IResourceHelper value) => throw new NotSupportedException();
    public void InitResources(InitResourcesCompleteCallback value) => throw new NotSupportedException();
    public HasAssetResult HasAsset(string value) => throw new NotSupportedException();
    public void LoadAsset(string name, Type type, string packageId, int priority,
        LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
    public void LoadScene(string name, int priority, LoadSceneCallbacks callbacks,
        object userData) => throw new NotSupportedException();
    public void LoadScene(string name, string packageId, int priority,
        LoadSceneCallbacks callbacks, object userData) => throw new NotSupportedException();
    public void UnloadScene(string name, UnloadSceneCallbacks callbacks,
        object userData) => throw new NotSupportedException();
    public void LoadBinary(string name, LoadBinaryCallbacks callbacks,
        object userData) => throw new NotSupportedException();
    public void LoadBinary(string name, string packageId, LoadBinaryCallbacks callbacks,
        object userData) => throw new NotSupportedException();
    public void InstantiateAsset(string name, LoadAssetCallbacks callbacks,
        object userData) => throw new NotSupportedException();
    public void InstantiateAsset(string name, string packageId,
        LoadAssetCallbacks callbacks, object userData) => throw new NotSupportedException();
}
```

Add these helper doubles in the same namespace:

```csharp
internal sealed class FakeSoundHelper : ISoundHelper
{
    internal readonly List<object> ReleasedAssets = new();
    public void ReleaseSoundAsset(object soundAsset) => ReleasedAssets.Add(soundAsset);
}

internal sealed class FakeSoundGroupHelper : ISoundGroupHelper
{
}

internal sealed class FakeSoundAgentHelper : ISoundAgentHelper
{
    public bool IsPlaying { get; private set; }
    public float Length { get; set; } = 1f;
    public float Time { get; set; }
    public bool Mute { get; set; }
    public bool Loop { get; set; }
    public int Priority { get; set; }
    public float Volume { get; set; }
    public float Pitch { get; set; }
    public float PanStereo { get; set; }
    public float SpatialBlend { get; set; }
    public float MaxDistance { get; set; }
    public float DopplerLevel { get; set; }
    public int PlayCount { get; private set; }
    public int StopCount { get; private set; }
    public object SoundAsset { get; private set; }

    public event EventHandler<ResetSoundAgentEventArgs> ResetSoundAgent;

    public void Play(float fadeInSeconds)
    {
        PlayCount++;
        IsPlaying = true;
    }

    public void Stop(float fadeOutSeconds)
    {
        StopCount++;
        Complete();
    }

    public void Pause(float fadeOutSeconds) => IsPlaying = false;
    public void Resume(float fadeInSeconds) => IsPlaying = true;

    public void Reset()
    {
        IsPlaying = false;
        SoundAsset = null;
    }

    public bool SetSoundAsset(object soundAsset)
    {
        SoundAsset = soundAsset;
        return soundAsset != null;
    }

    internal void Complete()
    {
        IsPlaying = false;
        ResetSoundAgentEventArgs args = ResetSoundAgentEventArgs.Create();
        try
        {
            ResetSoundAgent?.Invoke(this, args);
        }
        finally
        {
            ReferencePool.Release(args);
        }
    }
}
```

The fixture's `AddGroup` helper creates the group, creates exactly the requested
number of fake agents, registers each helper, and stores them in `_agentHelpers`:

```csharp
private void AddGroup(string name, SoundOverflowPolicy policy, int agentCount)
{
    Assert.That(_manager.AddSoundGroup(
        name, false, false, 1f, policy, new FakeSoundGroupHelper()), Is.True);
    for (int i = 0; i < agentCount; i++)
    {
        var helper = new FakeSoundAgentHelper();
        _agentHelpers.Add(helper);
        _manager.AddSoundAgentHelper(name, helper);
    }
}
```

- [ ] **Step 3: Run the tests and prove the policy API is missing**

Run:

```powershell
unity command run_tests --project-path "E:\Project\Demo\monsterhunter" --mode editor --filter "SoundManagerSchedulingTests" --filter_type testName --async_tests true
unity command test_status --project-path "E:\Project\Demo\monsterhunter"
```

Expected: compilation/test discovery fails because `SoundOverflowPolicy`, the new
overload, and `ISoundGroup.OverflowPolicy` do not exist.

- [ ] **Step 4: Add the backward-compatible policy contract**

Create:

```csharp
namespace GameFramework.Sound
{
    public enum SoundOverflowPolicy : byte
    {
        ReplaceByPriority = 0,
        QueueNonLooping = 1
    }
}
```

Add this property to `ISoundGroup` and `SoundGroup`:

```csharp
SoundOverflowPolicy OverflowPolicy { get; set; }
```

Add an `ISoundManager.AddSoundGroup` overload with this exact argument order:

```csharp
bool AddSoundGroup(
    string soundGroupName,
    bool soundGroupAvoidBeingReplacedBySamePriority,
    bool soundGroupMute,
    float soundGroupVolume,
    SoundOverflowPolicy overflowPolicy,
    ISoundGroupHelper soundGroupHelper);
```

Make both old overloads delegate to it with
`SoundOverflowPolicy.ReplaceByPriority`. Add
`LoopingSoundNotSupportedByQueue` to the end of `PlaySoundErrorCode` so existing
numeric values remain stable.

Add and satisfy this boundary test in the same task:

```csharp
[Test]
public void QueueGroup_RejectsLoopBeforeLoading()
{
    AddGroup("Reliable", SoundOverflowPolicy.QueueNonLooping, 1);
    PlaySoundErrorCode? error = null;
    _manager.PlaySoundFailure += (_, args) => error = args.ErrorCode;
    PlaySoundParams parameters = PlaySoundParams.Create();
    parameters.Loop = true;

    _manager.PlaySound("ui/invalid-loop", "Reliable", parameters);

    Assert.That(error, Is.EqualTo(PlaySoundErrorCode.LoopingSoundNotSupportedByQueue));
    Assert.That(_resourceManager.Loads, Is.Empty);
}
```

- [ ] **Step 5: Run the policy tests**

Expected: legacy default, explicit queue policy, and queue-loop rejection tests pass;
no scheduling behavior is asserted yet.

- [ ] **Step 6: Commit the policy seam and test harness**

```powershell
git add -- Framework/UnityGameFramework/Libraries/GameFramework/Sound Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.cs Framework/LFramework/Tests/Editor/Sound.meta Framework/LFramework/Tests/Editor/Sound
git commit -m "Define reliable sound group overflow semantics" -m "Constraint: Existing groups must retain replacement behavior through enum value zero.`nRejected: Caller-side preload leases | They do not solve agent saturation.`nConfidence: high`nScope-risk: moderate`nDirective: QueueNonLooping groups may accept only non-looping requests.`nTested: SoundManager policy Editor tests.`nNot-tested: Shared loading and cadence scheduling are implemented in later tasks."
```

---

### Task 2: Coalesce Same-Path Loads Into One Asset Entry

**Files:**

- Rename: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.PlaySoundInfo.cs` to `SoundManager.SoundPlayRequest.cs`
- Rename: corresponding `.meta` without changing its GUID
- Create: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.SoundAssetEntry.cs`
- Create: corresponding `.meta`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.cs`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.SoundGroup.cs`
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.SoundAgent.cs`
- Modify: `Framework/LFramework/Tests/Editor/Sound/SoundManagerSchedulingTests.cs`

- [ ] **Step 1: Write the cold-load coalescing test**

```csharp
[Test]
public void TenColdRequests_UseOneUntypedLoad_AndStartTenAgents()
{
    AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, agentCount: 16);
    var started = new List<int>();
    _manager.PlaySoundSuccess += (_, args) => started.Add(args.SerialId);

    int[] ids = Enumerable.Range(0, 10)
        .Select(_ => _manager.PlaySound("ui/tick", "UISound"))
        .ToArray();

    Assert.That(ids.Distinct().Count(), Is.EqualTo(10));
    Assert.That(_resourceManager.Loads, Has.Count.EqualTo(1));
    Assert.That(_resourceManager.Loads[0].AssetType, Is.Null);
    Assert.That(started, Is.Empty);

    object clip = new object();
    _resourceManager.CompleteSuccess(0, clip);

    Assert.That(started, Is.EquivalentTo(ids));
    Assert.That(_agentHelpers.Sum(agent => agent.PlayCount), Is.EqualTo(10));
}
```

- [ ] **Step 2: Run the test and confirm the current manager loads ten times**

Expected: the assertion for one load fails with an actual count of 10.

- [ ] **Step 3: Replace per-call load payloads with request and entry records**

Use a pooled request with explicit state:

```csharp
private enum SoundPlayRequestState : byte
{
    Created,
    WaitingAsset,
    WaitingAgent,
    Started,
    Cancelled,
    Failed
}

private sealed class SoundPlayRequest : IReference
{
    internal int SerialId;
    internal string SoundAssetName;
    internal SoundGroup SoundGroup;
    internal int ResourcePriority;
    internal PlaySoundParams PlaySoundParams;
    internal object UserData;
    internal double RequestedAt;
    internal bool WaitedForLoad;
    internal SoundPlayRequestState State;

    public void Clear()
    {
        SerialId = 0;
        SoundAssetName = null;
        SoundGroup = null;
        ResourcePriority = 0;
        PlaySoundParams = null;
        UserData = null;
        RequestedAt = 0d;
        WaitedForLoad = false;
        State = SoundPlayRequestState.Created;
    }
}
```

Use one non-pooled entry as the callback token:

```csharp
private sealed class SoundAssetEntry
{
    internal readonly string SoundAssetName;
    internal readonly List<SoundPlayRequest> LoadingRequests = new();
    internal object Asset;
    internal float LoadDuration;
    internal int AgentBorrowCount;
    internal double IdleSince;
    internal bool IsLoading;
    internal bool ReleaseOnLoad;
    internal bool Released;

    internal SoundAssetEntry(string soundAssetName)
    {
        SoundAssetName = soundAssetName;
    }
}
```

Replace `m_SoundsBeingLoaded` and `m_SoundsToReleaseOnLoad` with:

```csharp
private readonly Dictionary<int, SoundPlayRequest> m_PendingRequests = new();
private readonly Dictionary<string, SoundAssetEntry> m_SoundAssets =
    new(StringComparer.Ordinal);
```

`PlaySound` must validate first, reject loops for queue groups, acquire one request,
register it by serial, then join or create the path entry. Pass the entry itself as
the backend load `userData`:

```csharp
m_ResourceManager.LoadAsset(
    soundAssetName,
    null,
    request.ResourcePriority,
    m_LoadAssetCallbacks,
    entry);
```

On load success, transition all still-live `LoadingRequests` to `WaitingAgent` in
request order. For this task, start them immediately through the group; Task 3
replaces that immediate loop with lanes. Release request parameters and the request
only after its success/failure callback returns. A request that waited on the load
reports the entry's load duration; a later hot-cache request reports duration zero.

- [ ] **Step 4: Make agents borrow the entry rather than unload the asset**

Change the internal attachment seam to:

```csharp
internal bool SetSoundAsset(SoundAssetEntry entry, double startedAt)
{
    Reset();
    if (!m_SoundAgentHelper.SetSoundAsset(entry.Asset))
    {
        return false;
    }

    m_SoundAssetEntry = entry;
    m_SetSoundAssetTime = startedAt;
    m_SoundManager.RetainAgentBorrow(entry);
    return true;
}
```

`Reset()` calls `m_SoundManager.ReleaseAgentBorrow(m_SoundAssetEntry)` once and never
calls `ISoundHelper.ReleaseSoundAsset` directly. Pass the owning manager into each
`SoundGroup` and `SoundAgent` constructor. Change `SetSoundAssetTime` from `DateTime`
to `double`; replacement ordering compares manager real time and then serial id when
two starts share the same time.

- [ ] **Step 5: Run the coalescing test**

Expected: one untyped load, ten distinct success events, ten agent starts, and zero
unloads while agents still borrow the entry.

- [ ] **Step 6: Commit shared loading and manager-owned resource references**

```powershell
git add -- Framework/UnityGameFramework/Libraries/GameFramework/Sound Framework/LFramework/Tests/Editor/Sound
git commit -m "Share one sound asset load across repeated requests" -m "Constraint: Resource ownership must remain backend-neutral through IResourceManager.`nRejected: Typed AudioClip preload handles | Typed and untyped backend operations can have different identities.`nConfidence: high`nScope-risk: broad`nDirective: Sound agents borrow SoundAssetEntry and must never unload shared assets directly.`nTested: Cold ten-request coalescing and ten-start Editor test.`nNot-tested: Busy-agent cadence and cancellation arrive in later tasks."
```

---

### Task 3: Queue Non-Looping Requests and Preserve Cold-Load Cadence

**Files:**

- Create: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/SoundManager.SoundPlaybackLane.cs`
- Create: corresponding `.meta`
- Modify: `SoundManager.cs`
- Modify: `SoundManager.SoundGroup.cs`
- Modify: `SoundManager.SoundAgent.cs`
- Modify: `SoundManagerSchedulingTests.cs`

- [ ] **Step 1: Write cadence and saturation tests**

```csharp
[Test]
public void ColdRequests_PreserveOriginalIntervalsAfterLoad()
{
    AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, agentCount: 3);
    var starts = new List<double>();
    _manager.PlaySoundSuccess += (_, _) => starts.Add(_clock);

    _manager.PlaySound("ui/tick", "UISound");
    Advance(0.21f);
    _manager.PlaySound("ui/tick", "UISound");
    Advance(0.21f);
    _manager.PlaySound("ui/tick", "UISound");
    Advance(0.08f);
    _resourceManager.CompleteSuccess(0, new object());

    Advance(0f);
    Advance(0.21f);
    Advance(0.21f);

    Assert.That(starts, Has.Count.EqualTo(3));
    Assert.That(starts[1] - starts[0], Is.EqualTo(0.21d).Within(0.001d));
    Assert.That(starts[2] - starts[1], Is.EqualTo(0.21d).Within(0.001d));
}

[Test]
public void BusyQueueAgent_DelaysWithoutDroppingOrBurstingLaterRequests()
{
    AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, agentCount: 1);
    int[] ids = Enumerable.Range(0, 3)
        .Select(_ => _manager.PlaySound("ui/tick", "UISound"))
        .ToArray();
    _resourceManager.CompleteSuccess(0, new object());

    Assert.That(_startedIds, Is.EqualTo(new[] { ids[0] }));
    _agentHelpers[0].Complete();
    Advance(0f);
    Assert.That(_startedIds, Is.EqualTo(new[] { ids[0], ids[1] }));
    _agentHelpers[0].Complete();
    Advance(0f);
    Assert.That(_startedIds, Is.EqualTo(ids));
}
```

`Advance(seconds)` increments the fixture clock and calls
`GameFrameworkEntry.Update(seconds, seconds)`.

- [ ] **Step 2: Run both tests and capture their failures**

Expected: the immediate callback implementation starts too early or replaces active
agents; cadence and saturation assertions fail.

- [ ] **Step 3: Add per-group/path playback lanes**

Use this lane contract:

```csharp
private sealed class SoundPlaybackLane
{
    internal readonly SoundGroup SoundGroup;
    internal readonly SoundAssetEntry AssetEntry;
    internal readonly LinkedList<SoundPlayRequest> Requests = new();
    internal double ScheduleOffset;
    internal bool HasScheduleOffset;

    internal double GetDueTime(SoundPlayRequest request, double now)
    {
        if (!HasScheduleOffset)
        {
            ScheduleOffset = request.WaitedForLoad ? now - request.RequestedAt : 0d;
            HasScheduleOffset = true;
        }

        return request.RequestedAt + ScheduleOffset;
    }

    internal void ShiftAfterLateStart(double dueTime, double actualStart)
    {
        if (actualStart > dueTime)
        {
            ScheduleOffset += actualStart - dueTime;
        }
    }
}
```

Key lanes by a small value key containing the `SoundGroup` reference and ordinal
asset path. Remove empty lanes so a later independent burst starts with a fresh
offset. Add `LinkedListNode<SoundPlayRequest> LaneNode` to the request; cancellation
removes that node in O(1) without rebuilding or reordering the FIFO.

- [ ] **Step 4: Implement the deterministic scheduler**

Accumulate the manager clock only from `realElapseSeconds`:

```csharp
internal override void Update(float elapseSeconds, float realElapseSeconds)
{
    m_RealTime += realElapseSeconds;
    ScheduleReadyRequests();
    ReleaseExpiredIdleAssets();
}
```

For each group, inspect only each non-empty lane head whose due time is not greater
than `m_RealTime`. Choose the head with highest `PlaySoundParams.Priority`, then
lowest serial id. In a queue group, `SoundGroup.TryPlaySound` must select only an
agent whose helper is not playing. If no idle agent exists, leave the head queued.

After a start, fire success, clear request ownership, shift the lane offset by any
agent delay, dequeue, and continue while another head is due and another agent is
idle. Calls against an already-ready asset invoke the same scheduler before
returning so hot-cache behavior remains immediate.

- [ ] **Step 5: Add the same-frame ten-call assertion**

Use 16 fake agents, issue ten calls without advancing time, and assert ten starts in
the same manager tick. This distinguishes legitimate zero-interval playback from a
cold-load cadence burst.

Also lock legacy replacement behavior:

```csharp
[Test]
public void ReplacementGroup_KeepsSamePriorityReplacementBehavior()
{
    AddGroup("Music", SoundOverflowPolicy.ReplaceByPriority, 1);
    _manager.PlaySound("music/a", "Music");
    _resourceManager.CompleteSuccess(0, new object());
    _manager.PlaySound("music/b", "Music");
    _resourceManager.CompleteSuccess(1, new object());

    Assert.That(_agentHelpers[0].PlayCount, Is.EqualTo(2));
    Assert.That(_failedIds, Is.Empty);
}
```

- [ ] **Step 6: Run all scheduling tests and commit**

Expected: cadence, one-agent saturation, same-frame, and legacy replacement tests
all pass.

```powershell
git add -- Framework/UnityGameFramework/Libraries/GameFramework/Sound Framework/LFramework/Tests/Editor/Sound/SoundManagerSchedulingTests.cs
git commit -m "Preserve short-sound cadence through cold loads" -m "Constraint: Queue-enabled groups must use the existing fixed agent count without dropping.`nRejected: Immediate catch-up playback | It collapses delayed rhythm into one frame.`nConfidence: high`nScope-risk: broad`nDirective: Schedule with accumulated realElapseSeconds and remove empty lanes to reset burst cadence.`nTested: Cold cadence, busy-agent FIFO, same-frame ten-call, and legacy replacement Editor tests.`nNot-tested: Cache eviction and Unity AudioSource pause behavior arrive later."
```

---

### Task 4: Make Cancellation and Shared Failure Exact-Once

**Files:**

- Create: `Framework/UnityGameFramework/Libraries/GameFramework/Sound/PlaySoundCanceledEventArgs.cs`
- Create: corresponding `.meta`
- Modify: `ISoundManager.cs`
- Modify: `PlaySoundFailureEventArgs.cs`
- Modify: `SoundManager.cs`
- Modify: `SoundManager.SoundAssetEntry.cs`
- Modify: `SoundManager.SoundPlaybackLane.cs`
- Modify: `UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.cs`
- Modify: `SoundManagerSchedulingTests.cs`

- [ ] **Step 1: Write cancellation, stop-all, failure, and retry tests**

Cover these exact assertions:

```csharp
[Test]
public void CancellingOneSharedWaiter_DoesNotCancelTheOther()
{
    AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
    int cancelledId = _manager.PlaySound("ui/tick", "UISound");
    int survivingId = _manager.PlaySound("ui/tick", "UISound");

    Assert.That(_manager.StopSound(cancelledId), Is.True);
    _resourceManager.CompleteSuccess(0, new object());

    Assert.That(_cancelledIds, Is.EqualTo(new[] { cancelledId }));
    Assert.That(_startedIds, Is.EqualTo(new[] { survivingId }));
}

[Test]
public void SharedLoadFailure_FailsEveryWaiterOnce_ThenRetries()
{
    AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
    int first = _manager.PlaySound("ui/missing", "UISound");
    int second = _manager.PlaySound("ui/missing", "UISound");
    _resourceManager.CompleteFailure(0, "missing");

    Assert.That(_failedIds, Is.EquivalentTo(new[] { first, second }));
    Assert.That(_primaryFailureFlags.Count(value => value), Is.EqualTo(1));

    _manager.PlaySound("ui/missing", "UISound");
    Assert.That(_resourceManager.Loads, Has.Count.EqualTo(2));
}
```

Also assert:

- `GetAllLoadingSoundSerialIds` contains only requests in `WaitingAsset`;
- `StopAllLoadingSounds` clears that set and fires one cancellation per request;
- `StopAllLoadedSounds` cancels `WaitingAgent` and stops playing helpers;
- all waiters cancelled before a late success cause one resource release and no
  playback start;
- load progress and dependency callbacks fan out to every live `WaitingAsset`
  request.

- [ ] **Step 2: Add cancellation and primary-failure event data**

Add to `ISoundManager`:

```csharp
event EventHandler<PlaySoundCanceledEventArgs> PlaySoundCanceled;
```

The cancellation args contain `SerialId`, `SoundAssetName`, `SoundGroupName`, and
`UserData`; they deliberately do not contain an error code. Add an
`IsPrimaryFailure` property and boolean argument to core
`PlaySoundFailureEventArgs.Create`.

- [ ] **Step 3: Centralize terminal request cleanup**

Implement only three terminal helpers:

```csharp
private void CompleteStartedRequest(SoundPlayRequest request, ISoundAgent agent);
private void FailRequest(SoundPlayRequest request, PlaySoundErrorCode code,
    string message, bool isPrimaryFailure);
private void CancelRequest(SoundPlayRequest request);
```

Each helper removes the serial registry entry, removes the request from its current
entry/lane exactly once, invokes the relevant event, then releases referenced
parameters and the request in a `finally` block. It clears `UserData` through
`ReferencePool.Release(request)`. The manager must never return a request to the
pool before its callback completes.

Preserve existing no-listener behavior without interrupting batch cleanup. A
synchronous validation failure cleans its request/parameters and then throws. An
asynchronous shared failure first fails and cleans every live waiter, then throws one
`GameFrameworkException` only after the batch is empty when no failure handler is
registered. Cancellation never throws because it is expected control flow.

When the last loading request cancels, keep the entry as the callback token and set
`ReleaseOnLoad = true`. A new request arriving before the callback clears that flag
and joins the existing operation rather than starting a duplicate load.

- [ ] **Step 4: Release Runtime PlaySoundInfo on silent cancellation**

Subscribe in `SoundComponent.AwakeComponent` and add:

```csharp
private void OnPlaySoundCanceled(
    object sender,
    GameFramework.Sound.PlaySoundCanceledEventArgs e)
{
    if (e.UserData is PlaySoundInfo playSoundInfo)
    {
        ReferencePool.Release(playSoundInfo);
    }
}
```

Do not log and do not forward a runtime failure event. On shared load failure,
`OnPlaySoundFailure` logs only when `e.IsPrimaryFailure` is true, but still fires a
runtime failure event for every serial.

- [ ] **Step 5: Run cancellation/failure tests and inspect reference-pool counts**

At each test end, assert `ReferencePool.GetAllReferencePoolInfos()` reports zero
`UsingReferenceCount` for `SoundPlayRequest`, `PlaySoundParams`, and the runtime
`PlaySoundInfo` type after all callbacks complete. The first and third types are
internal, so locate their `ReferencePoolInfo` records by `info.Type.Name` instead of
referencing those types directly from the Editor test assembly.

- [ ] **Step 6: Commit exact-once terminal cleanup**

```powershell
git add -- Framework/UnityGameFramework/Libraries/GameFramework/Sound Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.cs Framework/LFramework/Tests/Editor/Sound
git commit -m "Terminate pending sound requests exactly once" -m "Constraint: Every returned serial must resolve through start, failure, or explicit cancellation.`nRejected: Reporting cancellation as failure | Caller stops are expected control flow and must stay log-silent.`nConfidence: high`nScope-risk: broad`nDirective: Fire cancellation cleanup before clearing runtime user data and never forward it as PlaySoundFailure.`nTested: Shared waiter cancellation, stop-all, late success, failure fan-out, retry, and reference-pool Editor tests.`nNot-tested: Idle retention and shutdown eviction arrive next."
```

---

### Task 5: Add Idle Retention, Eviction, and Shutdown Release

**Files:**

- Modify: `ISoundManager.cs`
- Modify: `SoundManager.cs`
- Modify: `SoundManager.SoundAssetEntry.cs`
- Modify: `SoundManager.SoundGroup.cs`
- Modify: `SoundManager.SoundAgent.cs`
- Modify: `SoundManagerSchedulingTests.cs`

- [ ] **Step 1: Write resource-lifetime tests**

Add tests for:

```csharp
[Test]
public void SharedAsset_ReleasesOnceAfterLastAgentAndIdleTimeout()
{
    _manager.SetSoundCacheSettings(2f, 64, 64, 32);
    AddGroup("UISound", SoundOverflowPolicy.QueueNonLooping, 2);
    _manager.PlaySound("ui/tick", "UISound");
    _manager.PlaySound("ui/tick", "UISound");
    object asset = new object();
    _resourceManager.CompleteSuccess(0, asset);

    _agentHelpers[0].Complete();
    _agentHelpers[1].Complete();
    Advance(1.99f);
    Assert.That(_soundHelper.ReleasedAssets, Is.Empty);
    Advance(0.01f);
    Assert.That(_soundHelper.ReleasedAssets, Is.EqualTo(new[] { asset }));
}
```

Also cover:

- a new request before TTL cancels idle eviction and does not reload;
- more than `maxIdleEntryCount` evicts only the oldest idle entry;
- active and loading entries may exceed the idle maximum and are never evicted;
- the same path in two groups causes one load but independent lanes;
- shutdown resets every active agent and releases each ready entry once;
- a success callback arriving after shutdown releases its asset once;
- a failure callback arriving after shutdown does not emit events or leave entries.

Install a `FakeLogHelper : GameFrameworkLog.ILogHelper` and add a hysteresis test:
crossing the high threshold produces one warning, adding more queued requests
produces no additional warning, draining to the reset threshold re-arms logging,
and crossing the high threshold again produces exactly one second warning. Assert
that every queued serial still starts after agents are completed.

- [ ] **Step 2: Add one cache-settings method**

Add this exact public manager method:

```csharp
void SetSoundCacheSettings(
    float idleReleaseSeconds,
    int maxIdleEntryCount,
    int queueWarningThreshold,
    int queueWarningResetThreshold);
```

Validate `idleReleaseSeconds >= 0`, `maxIdleEntryCount >= 0`, warning threshold
greater than zero, and reset threshold between zero and warning threshold. Invalid
arguments throw `GameFrameworkException` before mutating existing settings.

Initialize the manager itself to `2f`, `64`, `64`, and `32` so direct
`ISoundManager` users receive the documented defaults even without a
`SoundComponent`.

- [ ] **Step 3: Implement idle and borrow accounting**

`ReleaseAgentBorrow` decrements once and sets `IdleSince = m_RealTime` only when the
entry has no loading requests, no lane requests, and no agent borrows. Eviction
collects candidates first, orders by `IdleSince`, and releases through one helper:

```csharp
private void ReleaseAssetEntry(SoundAssetEntry entry)
{
    if (entry.Released)
    {
        return;
    }

    entry.Released = true;
    m_SoundAssets.Remove(entry.SoundAssetName);
    object asset = entry.Asset;
    entry.Asset = null;
    if (asset != null)
    {
        m_SoundHelper.ReleaseSoundAsset(asset);
    }
}
```

- [ ] **Step 4: Add warning hysteresis without dropping**

Track queued request count per group. Log once through `GameFrameworkLog.Warning`
when a group crosses the high threshold. Re-arm only after it falls to or below the
reset threshold. The warning includes group name, queued count, agent count, and
policy. It must not cancel, fail, or reorder any request.

- [ ] **Step 5: Make shutdown explicit and late-callback safe**

Set `m_IsShuttingDown` first, cancel pending requests, call
`SoundGroup.ResetAllSoundAgents()`, release ready entries, and mark loading entries
for release. Clear normal registries only after their owners have terminated. Late
callbacks check `m_IsShuttingDown`, release a returned asset once, and do not fire
playback events.

- [ ] **Step 6: Run lifetime tests and commit**

Expected: load count, release count, entry eviction order, agent reset count, and
late callback tests all pass.

```powershell
git add -- Framework/UnityGameFramework/Libraries/GameFramework/Sound Framework/LFramework/Tests/Editor/Sound/SoundManagerSchedulingTests.cs
git commit -m "Bound idle sound assets without dropping requests" -m "Constraint: Cache limits may evict only idle entries and queue limits may never discard accepted requests.`nRejected: Hard queue cap | It violates the no-drop contract.`nConfidence: high`nScope-risk: broad`nDirective: Release assets only through ReleaseAssetEntry and keep late shutdown callbacks idempotent.`nTested: TTL, max-idle, multi-group sharing, warning hysteresis, shutdown, and late-callback Editor tests.`nNot-tested: Real Addressables and YooAsset playback requires an owning project audio asset."
```

---

### Task 6: Fix Pause Completion and Playback Time

**Files:**

- Modify: `Framework/UnityGameFramework/Scripts/Runtime/Sound/DefaultSoundAgentHelper.cs`
- Create: `Framework/LFramework/Tests/Editor/Sound/DefaultSoundAgentHelperTests.cs`
- Create: corresponding `.meta`

- [ ] **Step 1: Write the paused-agent regression test**

```csharp
[Test]
public void Update_DoesNotResetPausedAudioSource()
{
    var go = new GameObject("Paused Sound Agent");
    try
    {
        var helper = go.AddComponent<DefaultSoundAgentHelper>();
        var clip = AudioClip.Create("pause-test", 44100, 1, 44100, false);
        int resetCount = 0;
        helper.ResetSoundAgent += (_, _) => resetCount++;

        Assert.That(helper.SetSoundAsset(clip), Is.True);
        helper.Play(0f);
        helper.Pause(0f);
        InvokePrivateUpdate(helper);

        Assert.That(resetCount, Is.Zero);
    }
    finally
    {
        UnityEngine.Object.DestroyImmediate(go);
    }
}
```

Add a second test that sets `helper.Time = 0.25f` on a one-second clip and asserts
the getter is within 0.02 seconds.

- [ ] **Step 2: Run and confirm current behavior fails**

Expected: paused update fires reset, and time remains at its previous value because
the setter body is commented out.

- [ ] **Step 3: Track pause explicitly**

Add `m_IsPaused`. Set it before immediate or coroutine pause, clear it on Play,
Resume, Stop, and Reset. Change completion detection to:

```csharp
if (!m_ApplicationPauseFlag && !m_IsPaused && !IsPlaying &&
    m_AudioSource.clip != null && m_ResetSoundAgentEventHandler != null)
```

For faded pause, set the flag before starting the coroutine so intermediate frames
cannot reset the agent.

- [ ] **Step 4: Restore a clip-aware time setter**

```csharp
set
{
    if (m_AudioSource.clip == null)
    {
        return;
    }

    m_AudioSource.time = Mathf.Clamp(value, 0f, m_AudioSource.clip.length);
}
```

- [ ] **Step 5: Run helper tests and commit**

Expected: pause survives repeated private `Update` calls, resume clears the paused
flag, reset clears the clip and flag, and the time setter moves playback position.

```powershell
git add -- Framework/UnityGameFramework/Scripts/Runtime/Sound/DefaultSoundAgentHelper.cs Framework/LFramework/Tests/Editor/Sound/DefaultSoundAgentHelperTests.cs Framework/LFramework/Tests/Editor/Sound/DefaultSoundAgentHelperTests.cs.meta
git commit -m "Keep paused sound agents alive until resume" -m "Constraint: AudioSource.isPlaying is false while paused and cannot identify completion alone.`nRejected: Treating every non-playing source as complete | It resets paused clips.`nConfidence: high`nScope-risk: narrow`nDirective: Maintain the explicit paused flag across immediate and faded pause paths.`nTested: Pause update, resume, reset, and playback-time Editor tests.`nNot-tested: Audible pause continuity is deferred to project PlayMode verification."
```

---

### Task 7: Wire SoundComponent Settings and Runtime Cleanup

**Files:**

- Modify: `Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.cs`
- Modify: `Framework/UnityGameFramework/Scripts/Runtime/Sound/SoundComponent.SoundGroup.cs`
- Modify: `Framework/LFramework/Scripts/Runtime/Settings/ComponentSettings/SoundComponentSetting.cs`
- Modify: `Framework/LFramework/Assets/Settings/ComponentSettings/SoundComponentSetting.asset`
- Create: `Framework/LFramework/Tests/Editor/Sound/SoundComponentSettingsTests.cs`
- Create: corresponding `.meta`

- [ ] **Step 1: Write settings-transfer and template tests**

Create a `SoundComponentSetting`, assign non-default cache values, invoke the
existing non-public `ComponentSettingExtension.CovertToComponent` by reflection,
and assert the private fields on the returned `SoundComponent` match exactly.

Load the package template with `AssetDatabase.LoadAssetAtPath<SoundComponentSetting>`
and assert:

```csharp
Assert.That(GetGroup(setting, "Music").OverflowPolicy,
    Is.EqualTo(SoundOverflowPolicy.ReplaceByPriority));
Assert.That(GetGroup(setting, "Sound").OverflowPolicy,
    Is.EqualTo(SoundOverflowPolicy.ReplaceByPriority));
Assert.That(GetGroup(setting, "UISound").OverflowPolicy,
    Is.EqualTo(SoundOverflowPolicy.QueueNonLooping));
```

- [ ] **Step 2: Run and confirm fields and policies are missing**

Expected: reflection and policy assertions fail before integration fields exist.

- [ ] **Step 3: Add mirrored serialized fields**

Add the same names and types to both `SoundComponent` and `SoundComponentSetting`:

```csharp
[SerializeField]
private float m_SoundAssetIdleReleaseSeconds = 2f;
[SerializeField]
private int m_MaxIdleSoundAssetCount = 64;
[SerializeField]
private int m_SoundQueueWarningThreshold = 64;
[SerializeField]
private int m_SoundQueueWarningResetThreshold = 32;
```

Use the same names as public fields in `SoundComponentSetting`, without
`SerializeField`, because the existing component-setting classes use public mirrored
fields. Configure the manager in `SoundComponent.AwakeComponent` after retrieving
it and before accepting playback.

- [ ] **Step 4: Serialize and forward group policy**

Add `m_OverflowPolicy = SoundOverflowPolicy.ReplaceByPriority` and an
`OverflowPolicy` getter to `SoundComponent.SoundGroup`. Preserve the old
`SoundComponent.AddSoundGroup` overload and add a policy overload. Startup passes
the serialized policy to the new manager overload.

Update YAML with cache defaults and explicit values:

```yaml
m_SoundAssetIdleReleaseSeconds: 2
m_MaxIdleSoundAssetCount: 64
m_SoundQueueWarningThreshold: 64
m_SoundQueueWarningResetThreshold: 32
```

Each group receives `m_OverflowPolicy`; `Music` and `Sound` use `0`, `UISound` uses
`1`.

- [ ] **Step 5: Make component shutdown correct in either component order**

`SoundComponent.ShutDown` first calls both manager stop-all methods while subscribed,
then unsubscribes success, failure, cancellation, and optional progress/dependency
events. This handles the case where SoundComponent shuts down before BaseComponent.
`SoundManager.Shutdown` also fires cancellation before clearing state, which handles
the common registration order where BaseComponent shuts down the module first.
Both paths are idempotent.

- [ ] **Step 6: Run settings and full sound tests, then commit**

Expected: reflection transfer passes, the asset template has only UISound queued,
and all earlier scheduling/lifecycle tests remain green.

```powershell
git add -- Framework/UnityGameFramework/Scripts/Runtime/Sound Framework/LFramework/Scripts/Runtime/Settings/ComponentSettings/SoundComponentSetting.cs Framework/LFramework/Assets/Settings/ComponentSettings/SoundComponentSetting.asset Framework/LFramework/Tests/Editor/Sound
git commit -m "Enable reliable queueing for the UISound template" -m "Constraint: Existing serialized groups must default to replacement while only the package UISound template opts in.`nRejected: Enabling no-drop globally | Long or looping Sound groups can starve a reliable queue.`nConfidence: high`nScope-risk: moderate`nDirective: Keep SoundComponent and SoundComponentSetting field names and types exactly mirrored.`nTested: Settings reflection, template policy, cancellation cleanup, and full sound Editor fixtures.`nNot-tested: Owning-project audible playback remains a runtime verification boundary."
```

---

### Task 8: Final Compile, Test, and Backend-Boundary Verification

**Files:**

- Verify all files above and every new or renamed `.meta` file.
- Update: `docs/superpowers/plans/2026-08-25-stable-sound-playback.md` checkbox state only after each command produces evidence.

- [ ] **Step 1: Run a fresh Unity recompile**

```powershell
unity command recompile --project-path "E:\Project\Demo\monsterhunter" --focus false
unity command recompile_status --project-path "E:\Project\Demo\monsterhunter"
```

Poll until the triggered compilation reaches `completed`. `up_to_date` without a
new trigger is not fresh evidence.

- [ ] **Step 2: Run the targeted Editor tests asynchronously**

```powershell
unity command run_tests --project-path "E:\Project\Demo\monsterhunter" --mode editor --filter "LFramework.Editor.Tests.Sound" --filter_type testName --async_tests true --timeout 300
unity command test_status --project-path "E:\Project\Demo\monsterhunter"
```

Poll until complete. Expected: all sound fixtures pass with zero failures.

- [ ] **Step 3: Run affected resource/settings regression fixtures**

Run `AddressableResourceHelperTests`, `ResourceComponentMultiPackageTests`, and
`NetworkComponentSettingTests` separately with the same async test command. These
prove the untyped resource seam and reflection settings path still compile and
behave as before.

- [ ] **Step 4: Read only fresh Console errors**

Capture a Console cursor before recompile, then request `level=error` with `since`
that cursor after tests. Report new errors only; do not attribute historical Console
entries to this change.

- [ ] **Step 5: Run static ownership checks**

```powershell
rg -n "ReleaseSoundAsset" Framework/UnityGameFramework/Libraries/GameFramework/Sound
rg -n "LoadAsset\(soundAssetName, null" Framework/UnityGameFramework/Libraries/GameFramework/Sound
rg -n "LoadAssetHandle<AudioClip>|AcquireSoundAsset|SoundPreloadLease" Framework
git diff --check
git status --short
```

Expected:

- only `SoundManager` owns final shared-asset release;
- normal sound loading remains untyped;
- no preload-lease API remains;
- no whitespace errors;
- unrelated user changes remain present and untouched.

- [ ] **Step 6: Record the real-backend verification boundary honestly**

This framework checkout currently contains no `.wav`, `.mp3`, `.ogg`, `.aif`, or
`.aiff` asset, so it cannot independently prove audible playback or actual
Addressables/YooAsset load identity. Do not synthesize a persistent test asset or
claim runtime audio proof. Record Editor tests and compile as framework evidence,
then run a ten-call PlayMode smoke test only when an owning Unity project supplies a
configured short sound address:

```csharp
for (int i = 0; i < 10; i++)
{
    soundComponent.PlaySound(configuredShortSoundPath, "UISound");
}
```

Runtime acceptance is ten success events, ten agent starts, one backend load for a
cold path, no `IgnoredDueToLowPriority`, and one final unload after agents complete
and the idle TTL expires.

- [ ] **Step 7: Final review and Lore commit**

Review the complete diff for public API compatibility, enum serialization defaults,
resource exact-once ownership, cancellation event ordering, and preservation of
unrelated changes. Commit only the sound architecture, tests, settings asset, and
plan state. The final Lore message must list targeted test fixtures and explicitly
state the missing real-audio runtime proof in `Not-tested:`.

---

## Plan Review Checklist

- Every approved spec requirement maps to a task and a named test.
- Legacy replacement behavior is covered before UISound opts into queueing.
- Queue cadence, capacity saturation, cancellation, failure, retry, TTL, max-idle,
  pause, stop-all, and shutdown have independent assertions.
- Runtime wrapper release happens in success, failure, and cancellation paths.
- Agent borrow and manager resource ownership cannot both unload the same object.
- No step adds a caller preload API, a new AudioSource, or a hard dropping cap.
- Real backend/audio proof is identified as unavailable in this framework-only
  checkout rather than represented as completed verification.
