# Stable Sound Playback Design

## Goal

Make repeated short sound requests reliable without changing the synchronous
`PlaySound(...): int` caller API. Ten accepted calls must produce ten distinct
playback starts unless the caller explicitly cancels them, the sound system shuts
down, or the shared asset load fails.

The framework must solve both causes of lost cadence:

1. Repeated calls for a cold asset currently start independent asynchronous loads.
2. A loaded sound is currently ignored or replaces another sound when its group has
   no idle agent.

## Scope

This change is contained in the GameFramework sound manager and its
UnityGameFramework integration. It does not add a business-layer preload API and
does not depend on Hotfix types or project data tables.

The implementation also repairs lifecycle defects found on the same playback path:

- paused `AudioSource` instances are currently mistaken for completed playback;
- `StopAllLoadingSounds` leaves stale loading state;
- load failure and load cancellation do not release every pooled request object;
- shutdown does not explicitly reset every sound agent and release every retained
  asset reference.

## Non-Goals

- Do not add AudioSources or a second sound-agent pool.
- Do not add caller-side `await`, preload leases, or explicit acquire/release calls.
- Do not preload every configured sound.
- Do not build a generic resource cache outside the sound module.
- Do not promise playback after explicit `StopSound`, stop-all calls, shutdown, or
  resource load failure.

## Public Contract

All existing `SoundComponent.PlaySound` and `ISoundManager.PlaySound` overloads stay
synchronous and keep returning the real serial id immediately.

Each call creates one internal request. A successful request fires
`PlaySoundSuccess` when an agent actually starts that request, matching the existing
event timing. The request then releases its parameters and user-data reference; the
agent becomes the playback owner. A shared load failure still produces one failure
event per serial id, so every accepted request reaches one terminal decision.

`StopSound(serialId)` has request-aware behavior:

- `WaitingAsset` or `WaitingAgent`: cancel the request before playback;
- `Playing`: stop the owning agent with the existing fade behavior;
- terminal or unknown id: return `false`.

`GetAllLoadingSoundSerialIds` and `IsLoadingSound` report only `WaitingAsset`.
`StopAllLoadingSounds` cancels `WaitingAsset`. `StopAllLoadedSounds` cancels
`WaitingAgent` and stops `Playing` requests.

Pause and resume continue to apply only to requests that have started playing.

## Sound Group Overflow Policy

Add a serialized group-level policy:

```csharp
public enum SoundOverflowPolicy
{
    ReplaceByPriority = 0,
    QueueNonLooping = 1,
}
```

`ReplaceByPriority` preserves the current idle-agent, lower-priority replacement,
same-priority replacement, and `IgnoredDueToLowPriority` behavior.

`QueueNonLooping` has these invariants:

- only an idle agent may start a request;
- an active request is never replaced by another request in the same group;
- a request waits when all agents are busy;
- no hard queue limit may silently discard an accepted request;
- `Loop == true` fails with a dedicated error code because a looping request can
  permanently occupy an agent and starve the queue.

The enum value `0` is the legacy behavior, so existing serialized groups remain
backward compatible. The framework template explicitly configures only `UISound`
as `QueueNonLooping`; `Music` and `Sound` remain `ReplaceByPriority`. Other groups
may opt in only when they contain short non-looping sounds.

## Sound Manager Architecture

`SoundManager` becomes the single owner of three internal concepts while the public
interface remains request-oriented.

### SoundPlayRequest

One pooled request exists per returned serial id until it starts, fails, or is
cancelled. It contains:

- serial id, asset path, group, resource-load priority, and call time;
- the existing `PlaySoundParams` and opaque user data;
- request state and whether the call waited for the current asset load;
- its playback lane membership.

The current `PlaySoundParams` ownership rule is preserved. A pooled object created
with `PlaySoundParams.Create()` is owned by the manager after the call and is
released exactly once after the success/failure/cancellation callback returns.
Non-pooled instances are not released. Callers must not mutate parameters after
passing them to PlaySound, which is already required by the existing asynchronous
implementation.

After a successful start, the request clears its user-data reference before it is
returned to the reference pool. This is required because `SoundComponent` releases
its pooled runtime `PlaySoundInfo` wrapper while handling the success event. The
playing `SoundAgent` retains only playback data, the serial id, and its asset-entry
borrow.

### SoundAssetEntry

One entry exists per ordinal `soundAssetName` string and contains:

- `Loading`, `Ready`, or `Failed` state;
- the single loaded asset object and initial load duration;
- waiting-request count and playing-agent borrow count;
- idle-since time and release-on-load/shutdown flags;
- the requests waiting for the current load callback.

The manager starts one untyped
`IResourceManager.LoadAsset(soundAssetName, null, ...)` operation for a missing
entry. Concurrent requests join that entry. The first request supplies load
priority because the existing resource interface cannot raise an in-flight load's
priority.

The manager owns exactly one resource reference. Sound agents borrow the entry and
must not call `ISoundHelper.ReleaseSoundAsset` independently. The manager releases
the asset once after both request and agent counts reach zero and the idle policy
allows eviction.

This deliberately uses the existing untyped load/release seam. A separate
`LoadAssetHandle<AudioClip>` preload is rejected because Addressables may create a
different operation identity for typed and untyped requests even when both return
the same `AudioClip` object.

### SoundPlaybackLane

A lane is keyed by `(SoundGroup, soundAssetName)`. It owns a FIFO of requests that
are ready for an agent and preserves the intervals recorded at call time.

When a cold asset becomes ready, the first waiting request is due immediately. Each
next due time is derived from the previous request's actual start plus the original
call-time interval. If agent saturation delays a start, all later requests in that
lane shift by the same delay instead of catching up in one frame.

Examples:

- calls at `0.00, 0.21, 0.42` with a load completing at `0.50` start at
  approximately `0.50, 0.71, 0.92`;
- ten calls in the same frame have zero original interval and may start together
  when ten agents are idle;
- if fewer agents are idle, the remaining requests wait and start as agents reset.

The manager accumulates `realElapseSeconds` in `Update` as its deterministic clock.
It does not use wall-clock `DateTime` for queue cadence or replacement ordering.

### Scheduler

Only each lane head participates in scheduling. Eligible heads are ordered by
playback priority and then serial id for stable FIFO behavior. A successful start
removes the request from the lane and registers the entry borrow on the selected
agent.

`SoundGroup` retains the replacement selector for `ReplaceByPriority` and gains an
idle-only selector for `QueueNonLooping`. Agent attachment is atomic: the helper
must accept the asset before the entry borrow is incremented.

## State Machines

Request state:

```text
Created -> WaitingAsset -> WaitingAgent -> Started
             |                |
             +-> Cancelled <--+
             +-> Failed <-----+
```

`Started`, `Cancelled`, and `Failed` are request terminal states. Playback then has
an independent agent lifecycle:

```text
Playing -> Paused -> Playing
    |
    +-> Reset -> asset borrow released
```

Asset state:

```text
Missing -> Loading -> Ready -> Idle -> Released
              |         ^        |
              +-> Failed          +-> Ready on a new request before eviction
              |
              +-> Released after a late callback when release-on-load is set
```

All terminal helpers are idempotent. Dictionary removal, pooled-object release,
parameter release, user-data clearing, resource release, and agent borrow decrement
must each happen at most once.

## Resource Retention

Ready entries become idle when they have no waiting requests and no playing agent
borrows. The proposed defaults are:

- idle retention: 2 real-time seconds;
- maximum idle entries: 64;
- eviction: expired entries first, then the oldest idle entry;
- loading, waiting, and playing entries are never evicted.

These are SoundComponent settings rather than caller APIs. An empty cache may grow
temporarily beyond 64 when entries are active because no-drop playback takes
precedence over the idle-cache bound.

There is no hard request-queue cap. A configurable diagnostic threshold emits one
warning when a group crosses the threshold and resets only after the queue falls
below a lower hysteresis threshold. This exposes pathological producers without
turning overload into silent data loss or log spam.

## Events, Failures, and Cancellation

One failed shared load fans out `PlaySoundFailure` to every waiting serial id. The
core failure args identify one primary failure in the batch. `SoundComponent` logs
only that primary failure but still forwards every runtime failure event.

Pending cancellation is not treated as a user-visible playback failure. Add an
internal core cancellation event so `SoundComponent` can release its pooled runtime
`PlaySoundInfo` wrapper without logging or firing a runtime failure event.

Load progress and dependency callbacks continue to fan out to all requests waiting
for the shared load, preserving per-serial event semantics when those events are
enabled.

After load failure, the asset entry is removed. A later PlaySound call creates a
new entry and retries the load.

## Agent, Pause, and Shutdown Lifecycle

`DefaultSoundAgentHelper` tracks an explicit paused flag. Its completion detector
does not reset an agent while paused. Play, stop, resume, and reset update the flag
consistently. The `Time` setter is restored with clip-aware clamping.

Agent reset releases only its manager-owned entry borrow. It no longer releases the
asset resource directly.

`SoundComponent.ShutDown` cancels waiting requests while event subscriptions still
exist, then stops/reset agents. `SoundManager.Shutdown` is an idempotent fallback:
it cancels all remaining requests, explicitly resets every agent, releases ready
entries once, and marks in-flight entries for one release when late callbacks
arrive.

## Planned File Boundaries

Core GameFramework changes:

- `Sound/ISoundManager.cs`: additive overflow-policy group registration and internal
  cancellation notification contract.
- `Sound/SoundOverflowPolicy.cs`: public policy enum.
- `Sound/SoundManager.cs`: request registry, shared load callbacks, scheduling,
  cancellation, eviction, and shutdown coordination.
- `Sound/SoundManager.PlaySoundInfo.cs`: replace the one-load-per-call payload with
  the request object or remove the obsolete type.
- `Sound/SoundManager.SoundAssetEntry.cs`: per-path resource ownership.
- `Sound/SoundManager.SoundPlaybackLane.cs`: per-group/path cadence queue.
- `Sound/SoundManager.SoundGroup.cs`: overflow policy and idle-only selection.
- `Sound/SoundManager.SoundAgent.cs`: borrowed-entry ownership and deterministic
  start ordering.
- sound event/error files: cancellation cleanup, loop-policy failure, and primary
  shared-failure metadata.

Unity integration changes:

- `Sound/SoundComponent.cs`: configure policies/cache settings, subscribe to
  cancellation cleanup, suppress duplicate batch logs, and perform ordered
  shutdown.
- `Sound/SoundComponent.SoundGroup.cs`: serialize overflow policy.
- `Sound/DefaultSoundAgentHelper.cs`: correct pause/completion/time behavior.
- `LFramework/.../SoundComponentSetting.cs`: expose global cache and queue-warning
  settings through the existing component-setting path.
- `LFramework/Assets/Settings/ComponentSettings/SoundComponentSetting.asset`:
  configure the package template, with only `UISound` opting into reliable queueing.

The existing untracked `SoundComponent.Preload.cs` and its meta file are removed,
and the associated shutdown call is removed from `SoundComponent.cs`.

## Verification Strategy

Add Editor tests around `ISoundManager` with fake resource, sound, group, and agent
helpers. Drive scheduling through `GameFrameworkEntry.Update` and isolate every test
with `GameFrameworkEntry.Shutdown`.

Required regression cases:

1. Ten cold calls to one path cause one resource load and ten unique playback starts.
2. Calls spaced by 0.21 seconds retain that cadence after the cold load completes.
3. More requests than agents wait and eventually start exactly once.
4. `ReplaceByPriority` retains legacy replacement and ignored-priority behavior.
5. A loop request in `QueueNonLooping` fails once and does not enter the queue.
6. Cancelling one shared waiter does not affect other requests for the same asset.
7. Cancelling all waiters before load completion releases the late asset exactly once.
8. Shared load failure fails every request once, logs once, removes the entry, and
   allows a later retry.
9. Repeated ready-asset calls share one reference; idle expiry unloads once.
10. The same path in different groups shares the asset but uses independent lanes.
11. Pause survives repeated updates; resume continues without resetting the agent.
12. Stop-all clears loading and queued state without leaking pooled objects.
13. Shutdown with loading, queued, and playing requests releases every owner once.
14. The package's existing sound tests and Unity compilation remain clean.

Runtime verification uses a short UI clip and records ten success events and ten
agent starts for ten immediate calls. A second probe calls at 0.21-second intervals
on a cold asset and confirms no same-frame completion burst. Addressables and
YooAsset modes each receive a load/release smoke check because the design depends on
their common `IResourceManager` contract.

## Risks and Mitigations

- Strict no-drop queues can grow under producer abuse. Use threshold diagnostics
  and profiling counts, not a silent cap.
- Long non-looping clips can create long waits. Only opt dedicated short-sound
  groups into `QueueNonLooping`.
- One shared resource owner makes any remaining per-agent unload a critical defect.
  Tests assert one load and one final release across multiple agents.
- Later higher-priority callers cannot raise an already-started shared load's
  resource priority. Playback ordering still uses each request's playback priority.
- Existing projects do not automatically become no-drop because enum zero preserves
  legacy behavior. The package template opts in only `UISound`, and other projects
  must opt in deliberately.

## Acceptance Criteria

- Existing callers compile without API changes or `await`.
- Ten accepted non-looping calls in a queue-enabled group produce ten starts.
- Cold-load requests retain their original relative cadence.
- Same-path concurrent requests issue one backend load.
- No agent replacement occurs in a queue-enabled group.
- Every request, pooled object, agent borrow, dictionary entry, and resource handle
  has an exact-once terminal cleanup path.
- Legacy groups retain their current replacement behavior by default.

## Rejected Alternatives

- Explicit preload lease: makes callers coordinate lifetime and still loses requests
  when all agents are busy.
- Preload inside PlaySound without a request scheduler: removes duplicate loading
  but still ignores or replaces playback at agent saturation.
- Dynamic AudioSource growth: violates the fixed-agent constraint and turns bursts
  into unbounded playback-object growth.
- Permanent static AudioClip cache: hides lifetime ownership and leaks across
  gameplay/window boundaries.
