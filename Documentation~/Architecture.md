# Architecture

`LFramework` is organized as a layered runtime framework on top of Game Framework and UnityGameFramework, with Zenject used for dependency injection and optional hotfix flow support through HybridCLR.

## Layering

Recommended conceptual layers:

1. **Foundation**
   - `GameFramework`
   - `UnityGameFramework`
   - `Zenject`
   - `UniTask`
   - `UniRx`
2. **LFramework Runtime**
   - application bootstrap
   - component registration
   - procedure integration
   - provider/world orchestration
   - settings
3. **Game/App Layer**
   - game-specific procedures
   - UI screens
   - providers
   - worlds
   - content logic
4. **Hotfix Layer**
   - dynamically loaded hotfix components
   - hotfix procedures
   - hotfix-specific providers and worlds

## Startup flow

`LSystemApplicationBehaviour.Awake()` is the main entry point.

High-level order:

1. Create Zenject `DiContainer`
2. Register project settings
3. Reflect and create configured `GameFrameworkComponent` instances
4. Bind components into DI
5. Inject application and components
6. Run component lifecycle (`AwakeComponent -> StartComponent -> SetUpComponent`)
7. Bind `ISystemApplication`
8. Connect awaitable/event support

## Runtime roles

### Application Behaviour

`LSystemApplicationBehaviour`

- Unity-facing bootstrap object
- owns startup ordering
- reads configured component type names
- bridges scene setup and framework runtime

### Components

`GameFrameworkComponent` derived types

- long-lived systems registered during app startup
- usually own subsystem-wide responsibilities
- can be configured through `ComponentSetting`

Examples in this package:

- Resource
- UI
- Table
- Sound
- Language
- Workflow

### Procedures

`RuntimeBaseProcedure` / `HotfixProcedureBase`

- model application flow states
- should focus on flow transitions and orchestration
- receive DI injection automatically on enter

### Providers

`SystemProviderBase`

- procedure-scoped or persistent support systems
- subscribe to events
- perform per-state services
- are activated by procedure state through `[BelongTo]`

### Worlds

`WorldBase`

- own stateful runtime domains linked to a procedure
- manage `IWorldHelper` collaborators
- provide update / late update forwarding

## Dependency guidance

Recommended dependency direction:

- App/Hotfix code -> public LFramework runtime abstractions
- Procedures -> providers/worlds through interfaces or DI
- Providers/worlds -> framework services/components
- Avoid app code depending on editor utilities or internal helper details

## Design intent

The package is strongest when consumers treat it as:

- a bootstrapping framework
- a flow/runtime composition framework
- a set of extension points

and not as a bag of internal helper classes.
