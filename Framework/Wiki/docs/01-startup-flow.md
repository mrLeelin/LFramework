# 01. 从 LSystemApplication 到进入游戏

这页从 `LSystemApplication.cs` 开始讲运行时如何接管热更世界，并一路走到业务真正进入游戏。

相关源码：

- `LFramework/Scripts/Hotfix/Base/SystemApplication/LSystemApplication.cs`
- `LFramework/Scripts/Hotfix/Base/SystemApplication/LSystemApplication.RegisterProvider.cs`
- `LFramework/Scripts/Hotfix/Base/SystemApplication/LSystemApplication.RegisterWorld.cs`
- `LFramework/Scripts/Hotfix/Entry.cs`
- `LFramework/Scripts/Hotfix/Procedure/HotfixProcedureBase.cs`
- `LFramework/Scripts/Runtime/World/WorldBase.cs`
- `LFramework/Scripts/Runtime/World/WorldHelperBase.cs`
- `LFramework/Scripts/Runtime/Helper/AutoRegisterWorldHelper.cs`

## 0. 前置链路：启动管线进入热更入口

`LSystemApplication` 不是 Unity 场景里直接拖出来的对象，它是在热更入口里创建的。

常见前置链路是：

```text
LaunchPipelineRunner
  -> InitResourceTask
  -> LoadAssemblyTask
  -> HotfixEntryTask
  -> HotfixComponent.EnterHotfixAssembly()
  -> LFramework.Hotfix.Entry.HotfixEntryStart()
```

`HotfixComponent.EnterHotfixAssembly()` 通过反射调用热更主程序集里的：

```csharp
LFramework.Hotfix.Entry.HotfixEntryStart
```

这一步之后，流程才进入 `LSystemApplication`。

## 1. Entry.HotfixEntryStart

`Entry.HotfixEntryStart()` 做五件关键事：

1. `SingletonManager.AddSingleton<LSystemApplication>()`
2. `LSystemApplication.ClearReflectionCache()`
3. 从 `LFrameworkAspect` 获取 `ProcedureComponent`
4. 从 `LFrameworkAspect` 获取 `HotfixComponent`
5. 注册热更组件、热更 Procedure，然后强制切到入口 Procedure

精简流程：

```text
Entry.HotfixEntryStart
  -> create LSystemApplication
  -> get ProcedureComponent
  -> get HotfixComponent
  -> application.RegisterHotfixComponents(hotfixComponent)
  -> AddHotfixProcedure(procedureComponent, hotfixComponent)
  -> procedureComponent.ForceChangedProcedure(entranceProcedureType)
```

入口 Procedure 类型名来自：

```text
ProcedureComponent.EntranceHotfixProcedureTypeName
```

如果这个类型名不在 `HotfixComponent.GetHotfixAssemblyAllTypes()` 字典里，热更入口会报错，游戏不会进入业务流程。

## 2. LSystemApplication 构造阶段

`LSystemApplication` 是热更运行时的总拥有者。它实现了：

- `ISystemProviderRegister`
- `IWorldRegister`
- `ISingletonUpdate`
- `ISingletonLateUpdate`

构造函数只做三件事：

```text
LServices.Inject(this)
LServices.Register<ISystemProviderRegister>(this)
LServices.Register<IWorldRegister>(this)
```

含义：

- `[Inject] private HotfixComponent HotfixComponent { get; set; }` 会被注入。
- 后续 `HotfixProcedureBase` 不直接引用 `LSystemApplication`，而是通过 `LServices` 获取两个注册接口。
- `LSystemApplication` 成为 Provider 与 World 注册/释放的唯一入口。

## 3. 注册热更组件

`RegisterHotfixComponents(HotfixComponent hotfixComponent)` 会扫描热更程序集里所有带：

```csharp
[HotfixComponentAttribute(...)]
```

的类型。

注册规则：

1. 跳过 `null`、抽象类、接口。
2. 类型必须继承 `GameFrameworkComponent`。
3. 防止同类型重复注册。
4. `Activator.CreateInstance(type)` 创建实例。
5. 将 `instance.Parent` 设置为 `hotfixComponent.Parent`。
6. 如果 `HotfixComponentAttribute.BindType` 不为空，用它注册到 `LServices`。
7. 如果 `BindType` 为空，则用 `GetDerivedInterfaces()` 找派生接口注册。
8. 全部实例创建完后，统一注入。
9. 统一调用生命周期：`AwakeComponent` -> `StartComponent` -> `SetUpComponent`。

重要点：

- 热更组件是框架级服务，不是某个 Procedure 的临时对象。
- 热更组件挂到 `LSystemApplication._hotfixComponents`，每帧由 `LSystemApplication.Update` 调用 `UpdateComponent`。
- 组件释放发生在 `LSystemApplication.Dispose`，调用 `ShutDown`。

## 4. 注册热更 Procedure

`Entry.AddHotfixProcedure` 会扫描带：

```csharp
[HotfixProcedureAttribute]
```

的类型。

规则：

1. 从 `HotfixComponent.GetTypesFromAttribute<HotfixProcedureAttribute>()` 获取类型。
2. 跳过抽象类。
3. `Activator.CreateInstance(type)` 创建 Procedure 实例。
4. `ProcedureComponent.AddHotfixProcedure(objs)` 加入 `ProcedureComponent`。
5. `ProcedureComponent.ForceChangedProcedure(entranceProcedureType)` 切到入口流程。

到这里为止，只是把流程对象交给 `ProcedureComponent`。真正进入游戏发生在入口 Procedure 的 `OnEnter`。

## 5. HotfixProcedureBase.OnEnter

所有热更业务流程应该继承 `HotfixProcedureBase`，它封住了流程进入/退出的框架顺序。

进入流程时：

```text
HotfixProcedureBase.OnEnter
  -> LServices.TryGet<ISystemProviderRegister>
  -> LServices.TryGet<IWorldRegister>
  -> providerRegister.TryRegisterProvider(ProcedureState)
  -> worldRegister.TryRegisterWorld(ProcedureState)
  -> world.LinkProcedure(this)
  -> Injection.Inject(this)
  -> OnEnterProcedure(procedureOwner)
```

这里的 `ProcedureState` 是业务流程的状态 ID。Provider 和 World 都通过它与当前流程绑定。

关键结论：

- Provider 先于 World 之后的业务 `OnEnterProcedure` 注册。
- World 创建成功后会反向持有当前 Procedure。
- Procedure 自己最后才执行注入，然后进入业务重写的 `OnEnterProcedure`。

## 6. Provider 注册

`TryRegisterProvider(int procedureState)` 扫描所有带：

```csharp
[BelongTo(procedureState, lifeCycle, sort)]
```

的类型。

注册条件：

1. 类型必须继承 `SystemProviderBase`。
2. `BelongToAttribute.ProcedureState` 必须等于当前 `ProcedureState`。
3. 同一个 Provider 类型已经注册过则跳过。

注册流程：

```text
ReferencePool.Acquire(providerType)
  -> RegisterCommonDataSync(systemProvider)
  -> LServices.Register(interfaceType, systemProvider)
  -> LServices.Register(providerType, systemProvider)
  -> collect sort
  -> sort desc
  -> Injection.Inject(provider)
  -> AwakeComponent()
  -> SubscribeEvent()
  -> SetUp()
```

排序规则：

- `BelongToAttribute.Sort` 越大越先初始化。

生命周期规则：

- `ProviderLifeCycle.CurState`：离开当前 Procedure 时释放。
- `ProviderLifeCycle.Forever`：切流程时不释放，直到应用整体释放或主动清理。

## 7. World 注册

`TryRegisterWorld(int procedureState)` 同样扫描 `BelongToAttribute`，但只接受继承 `WorldBase` 的类型。

注册流程：

```text
ReferencePool.Acquire(worldType)
  -> Injection.Inject(world)
  -> LServices.Register(interfaceType, world)
  -> LServices.Register(worldType, world)
  -> world.Initialized()
  -> _worldBase = world
  -> return world
```

`world.Initialized()` 内部是进入游戏前的关键点：

```text
ResolveFrameworkDependencies()
  -> Subscribe(EventComponent)
  -> RegisterWorldHelper()
  -> InitializeAllHelpers()
  -> OnInitialized()
```

其中：

- `ResolveFrameworkDependencies` 直接拿 `EventComponent` 和 `HotfixComponent`。
- `Subscribe` 给 World 自己订阅全局事件。
- `RegisterWorldHelper` 自动装配 WorldHelper。
- `InitializeAllHelpers` 初始化所有 Helper。
- `OnInitialized` 是业务 World 自己的初始化入口。

## 8. WorldHelper 自动注册

WorldHelper 通过：

```csharp
[AutoWorldHelper(typeof(SomeWorld))]
```

绑定到指定 World。

注册路径：

```text
WorldBase.RegisterWorldHelper
  -> AutoRegisterWorldHelper.GetRegisterWorldHelper(worldType)
  -> HotfixComponent.GetTypesFromAttribute<AutoWorldHelperAttribute>()
  -> filter attribute.BindWorldType contains worldType
  -> filter subclass of WorldHelperBase
  -> ReferencePool.Acquire(helperType)
  -> BuildWorldHelpers(helper)
  -> LServices.Register(helper business interface)
  -> LServices.Inject(helper)
```

Helper 进入 World 后会：

- `SetWorld(this)`
- 如果实现 `IWorldUpdate`，加入 `_worldUpdates`
- 如果实现 `IWorldLateUpdate`，加入 `_worldLateUpdates`
- 在 `InitializeAllHelpers` 中调用 `Initialize`

业务 World 可以在合适时机调用：

- `InstantiateHelperGame()`：并行执行所有 Helper 的 `InstantiateWorld`
- `StartHelperGame()`：通知所有 Helper 开始游戏
- `StopHelperGame()`：通知所有 Helper 停止游戏

因此“进入游戏”的业务落点通常在 World 的 `OnInitialized` 或入口 Procedure 的 `OnEnterProcedure` 中，触发 WorldHelper 的实例化和开始。

## 9. 每帧更新

`LSystemApplication.Update` 每帧按顺序更新：

```text
hotfix components UpdateComponent
system providers UpdateComponent
world Update
```

`LSystemApplication.LateUpdate` 只转发：

```text
world LateUpdate
```

`WorldBase.Update` 会遍历所有实现 `IWorldUpdate` 的 Helper。`WorldBase.LateUpdate` 会遍历所有实现 `IWorldLateUpdate` 的 Helper。

## 10. 离开流程与释放

`HotfixProcedureBase.OnLeave` 顺序：

```text
OnLeaveProcedure
base.OnLeave
providerRegister.TryUnRegisterProvider(ProcedureState)
worldRegister.TryUnRegisterWorld()
_linkWorld = null
```

Provider 释放：

```text
UnSubscribeEvent
UnRegisterCommonDataSync
LServices.Unregister(providerType)
LServices.Unregister(interfaceType)
Injection.ClearReflectionCache(type)
ReferencePool.Release(provider)
```

World 释放：

```text
LServices.Unregister(worldType)
LServices.Unregister(interfaceType)
Injection.ClearReflectionCache(worldType)
ReferencePool.Release(world)
```

`WorldBase.Clear` 会取消事件订阅、注销 Helper 服务、释放 Helper、清空更新列表，最后调用 `OnDeInitialized`。

## 11. 完整流程图

```text
HotfixEntryTask
  -> HotfixComponent.EnterHotfixAssembly
    -> Entry.HotfixEntryStart
      -> new LSystemApplication
        -> Inject HotfixComponent
        -> Register ISystemProviderRegister
        -> Register IWorldRegister
      -> RegisterHotfixComponents
        -> scan [HotfixComponent]
        -> create GameFrameworkComponent
        -> Register service
        -> Inject
        -> Awake/Start/SetUp
      -> AddHotfixProcedure
        -> scan [HotfixProcedure]
        -> AddHotfixProcedure
      -> ForceChangedProcedure
        -> HotfixProcedureBase.OnEnter
          -> TryRegisterProvider
          -> TryRegisterWorld
            -> World.Initialized
              -> RegisterWorldHelper
              -> InitializeAllHelpers
              -> OnInitialized
          -> Injection.Inject(procedure)
          -> OnEnterProcedure
            -> business starts game
```

## 排查顺序

| 现象 | 先看 |
| --- | --- |
| 没进入热更入口 | `HotfixEntryTask`、`HotfixComponent.EnterHotfixAssembly` |
| 找不到入口 Procedure | `ProcedureComponent.EntranceHotfixProcedureTypeName`、热更程序集类型字典 |
| Provider 没创建 | `BelongToAttribute.ProcedureState`、是否继承 `SystemProviderBase` |
| Provider 初始化顺序不对 | `BelongToAttribute.Sort` |
| World 没创建 | `BelongToAttribute.ProcedureState`、是否继承 `WorldBase` |
| WorldHelper 没注册 | `AutoWorldHelperAttribute.BindWorldType`、是否继承 `WorldHelperBase` |
| Helper 没更新 | 是否实现 `IWorldUpdate` 或 `IWorldLateUpdate` |
| 切流程后状态残留 | Provider 生命周期是否误设为 `Forever` |

