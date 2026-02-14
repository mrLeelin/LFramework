# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **LFramework**, a Unity game framework built on top of [Game Framework](https://gameframework.cn/) (19 built-in modules), integrated with **Zenject** (DI), **UniRx** (reactive extensions), and **LuBan** (data configuration). It supports hot-fix code loading via HybridCLR.

Unity version: 6000.3.0f1

## Architecture

The framework uses a layered architecture: **GameFramework** (core C# library) → **UnityGameFramework** (Unity integration, 19 modules) → **LFramework** (DI, hotfix, providers, worlds).

### Initialization Flow

`LSystemApplicationBehaviour.Awake()` drives startup:

```
Awake() → DiContainer = new DiContainer()
  → StartApplication()
    → SingletonManager.AddSingleton(new LFrameworkAspect(DiContainer))
    → RegisterSetting()          // Bind GameSetting to DI
    → RegisterComponents()       // Reflect + instantiate GameFrameworkComponents from serialized type list
    → BindComponents()           // Bind each component to DiContainer
    → ResolveApplicationDependencies()  // Inject all components + self
    → StartComponents()          // AwakeComponent() → StartComponent() for each
    → ApplicationStarted()       // Bind ISystemApplication, setup awaitable events
    → SetUpComponents()          // SetUpComponent() for each
```

### Hotfix Entry

When hot-fix assembly loads, `Entry.HotfixEntryStart()` runs:
1. Creates `LSystemApplication` singleton (manages hotfix lifecycle)
2. Discovers types via `[HotfixComponentAttribute]` and `[HotfixProcedureAttribute]`
3. Registers hotfix components into DI, runs their lifecycle
4. Force-changes to the hotfix entrance procedure

### Key Patterns

**Dependency Injection (Zenject)**
- Global container: `LFrameworkAspect.Instance.DiContainer`
- All components auto-bound at startup; use `[Inject]` for field/property injection
- `RuntimeBaseProcedure.OnEnter()` and `HotfixProcedureBase.OnEnter()` auto-inject `this`

**Component System**
- Base class: `GameFrameworkComponent` (non-MonoBehaviour)
- Lifecycle: `AwakeComponent()` → `StartComponent()` → `SetUpComponent()` → `UpdateComponent()` → `ShutDown()`
- Components created via reflection from serialized type names in `LSystemApplicationBehaviour`
- Each component gets a `ComponentSetting` ScriptableObject for configuration

**Procedure (Game Flow State Machine)**
- Runtime procedures extend `RuntimeBaseProcedure` (auto DI injection on enter)
- Hotfix procedures extend `HotfixProcedureBase` (auto registers providers + world on enter, unregisters on leave)
- Each hotfix procedure declares `ProcedureState` (int) to associate with providers/worlds

**System Providers**
- Extend `SystemProviderBase`, mark with `[BelongTo(procedureState, lifecycle, sort)]`
- Lifecycle: `AwakeComponent()` → `SubscribeEvent()` → `SetUp()` → `UpdateComponent()` → `UnSubscribeEvent()` → `OnStop()`
- `ProviderLifeCycle.CurState` = disposed when procedure leaves; `ProviderLifeCycle.Forever` = persists
- Auto-bound to DI by derived interface; auto-unbound on disposal

**World System**
- Extend `WorldBase`, mark with `[BelongTo(...)]`
- Contains `IWorldHelper` instances (auto-registered via `[AutoWorldHelper]` attribute)
- Helpers that implement `IWorldUpdate`/`IWorldLateUpdate` get frame callbacks
- One world per procedure; linked to its procedure via `LinkProcedure()`

**Event System**
- Define events by extending `GameEventArgs<T>` (auto-generates `EventID` from type hash)
- Create events via `GameEventArgs<T>.CreateEmpty()` or `.Create(callback)` (uses `ReferencePool`)
- Fire: `EventComponent.Fire(sender, args)` (next frame) or `.FireNow(sender, args)` (immediate)
- Subscribe/unsubscribe in `SystemProviderBase.Subscribe()`/`UnSubscribe()` overrides

**Singleton Management**
- `Singleton<T>` for plain C# singletons; `SingletonMonoBehaviour<T>` for MonoBehaviour singletons
- `SingletonManager` handles lifecycle, Update/LateUpdate dispatch via `ISingletonUpdate`/`ISingletonLateUpdate`

### Additional LFramework Components

Beyond the 19 GameFramework modules, LFramework adds:
- **TableComponent** - LuBan data table integration
- **SpriteCollectionComponent** - Sprite atlas management
- **ResourceDownloadComponent** - CDN resource download with version management
- **SequenceLineComponent** - Sequence/timeline system
- **WorkFlowComponent** - Node-based workflow engine
- **ViewComponent** - MVVM view binding system
- **GameNotificationsComponent** - Push notification management

## Assembly Structure

```
LFramework.Runtime        → Core runtime (depends on GameFramework, UnityGameFramework.Runtime, Zenject, UniRx)
LFramework.Hotfix         → Hot-fix system (depends on LFramework.Runtime, autoReferenced: false)
LFramework.Editor         → Editor tools
UnityGameFramework.Runtime → 19 built-in modules
GameFramework             → Pure C# core library (no Unity dependency)
Zenject                   → DI container
UniRx                     → Reactive extensions
Luban.Tool.Editor         → Data config tool (editor only)
```

Note: `LFramework.Hotfix.asmdef` has `autoReferenced: false` — it's loaded dynamically at runtime.

## Key Source Locations

- **App bootstrap**: `LFramework/Scripts/Runtime/Base/UnitySystemApplicationBehaviour.cs`, `LSystemApplicationBehaviour.cs`
- **DI facade**: `LFramework/Scripts/Runtime/Base/LFrameworkAspect.cs`
- **Component base**: `UnityGameFramework/Scripts/Runtime/Base/GameFrameworkComponent.cs`
- **Procedure base**: `LFramework/Scripts/Runtime/Procedure/RuntimeBaseProcedure.cs`
- **Hotfix entry**: `LFramework/Scripts/Hotfix/Entry.cs`
- **Hotfix procedure**: `LFramework/Scripts/Hotfix/Procedure/HotfixProcedureBase.cs`
- **Provider base**: `LFramework/Scripts/Runtime/Providers/SystemProviderBase.cs`
- **World base**: `LFramework/Scripts/Runtime/World/WorldBase.cs`
- **Event base**: `LFramework/Scripts/Runtime/Components/EventComponent/GameEventArgs.cs`
- **Singleton**: `LFramework/Scripts/Runtime/Singleton/Singleton.cs`, `SingletonManager.cs`
- **Settings**: `LFramework/Scripts/Runtime/Settings/`

## Extending the Framework

**Add a new component**: Create a class extending `GameFrameworkComponent`, add its full type name to the serialized `allComponentTypes` array on the `LSystemApplicationBehaviour` in the scene.

**Add a hotfix component**: Mark with `[HotfixComponent]` in the Hotfix assembly. It will be auto-discovered and registered.

**Add a hotfix procedure**: Mark with `[HotfixProcedure]`, extend `HotfixProcedureBase`, implement `ProcedureState`.

**Add a system provider**: Extend `SystemProviderBase`, mark with `[BelongTo(procedureState)]`. Override `Subscribe()`/`UnSubscribe()` for events.

**Add a world**: Extend `WorldBase`, mark with `[BelongTo(procedureState)]`. Register helpers in `RegisterWorldHelper()` or use `[AutoWorldHelper]`.
