# 03. 模块详解：Component / Provider / HotfixComponent / World / WorldHelper

这页按框架抽象解释每类对象的边界、创建方式、生命周期和适用场景。

## 1. Component

### 定位

Component 是框架级长期服务，类型通常继承：

```csharp
UnityGameFramework.Runtime.GameFrameworkComponent
```

它不是普通 `MonoBehaviour`。当前 `GameFrameworkComponent` 的 MonoBehaviour 继承已经被移除，组件实例通常由配置或热更扫描创建，然后由框架显式调用生命周期。

### 生命周期

`GameFrameworkComponent` 提供：

```text
AwakeComponent
StartComponent
SetUpComponent
UpdateComponent
LateUpdate
ShutDown
RuntimeOnApplicationFocus
RuntimeOnApplicationPause
RuntimeOnApplicationQuit
```

热更组件注册时的顺序固定为：

```text
create all components
  -> register services
  -> inject all components
  -> AwakeComponent all
  -> StartComponent all
  -> SetUpComponent all
```

### 配置绑定

工程配置中的 `ComponentSetting` 有一个关键字段：

```csharp
public string bindTypeName;
```

`ComponentSettingExtension.CovertToComponent()` 会：

1. 用 `bindTypeName` 找到组件类型。
2. 校验类型必须继承 `GameFrameworkComponent`。
3. 反射创建组件。
4. 将 Setting 字段按同名字段拷贝到组件实例。

所以 Component 的配置字段命名不是随意的：Setting 字段名要能匹配组件里的非公开实例字段。

### 使用场景

适合做：

- 资源系统：`ResourceComponent`
- 下载系统：`ResourceDownloadComponent`
- 热更系统：`HotfixComponent`
- 表格系统：`TableComponent`
- UI 系统：`UIComponent`
- 实体系统：`EntityComponent`
- 语言、声音、通知、计时器等基础服务

不适合做：

- 某个玩法流程的临时业务状态
- 某个关卡/房间/战斗上下文
- 需要随 Procedure 自动释放的逻辑

这些应该放 Provider 或 World。

## 2. HotfixComponent

### 定位

`HotfixComponent` 是热更系统组件，负责把热更 DLL 和热更类型交给框架。

它自身是一个长期 Component，同时依赖：

- `ProcedureComponent`
- `GameSetting`
- `HybridCLRSetting`
- `ResourceDownloadComponent`
- `ResourceComponent`

### 加载热更程序集

`LoadHotfixAssemblies(Action<HotfixCodeResult>)` 根据运行环境分支：

Editor 下：

```text
LoadEditorHotfixAssemblies
  -> 从 AppDomain.CurrentDomain.GetAssemblies() 找主逻辑程序集
  -> 按 HybridCLRSetting.hotfixAssembliesSort 收集热更程序集
```

非 Editor 且启用 HybridCLR：

```text
LoadAotAssemblies
  -> ResourceComponent.LoadAssetsByTagHandle<TextAsset>(defaultAotDllLabel)
  -> HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly

LoadHotfixAssembliesInternal
  -> ResourceComponent.LoadAssetsByTagHandle<TextAsset>(defaultCodeDllLabel)
  -> 按 HybridCLRSetting.hotfixAssembliesSort 排序
  -> Assembly.Load
  -> 找 logicMainDllName 对应主逻辑程序集
```

### 类型索引

加载成功后会执行 `ParseHotfixAssembly()`：

1. 收集所有热更程序集的类型到 `_allTypes`。
2. 扫描所有继承 `GameAttribute` 的 Attribute。
3. 建立 `_allAttributeTypes`，用于按 Attribute 类型反查业务类型。

这几个接口是后续启动的关键：

```csharp
GetHotfixAssemblyAllTypes()
GetHotfixAssemblyType(string typeName)
GetTypesFromAttribute<T>() where T : GameAttribute
```

### 进入热更入口

`EnterHotfixAssembly()` 通过 `StaticMethod` 调用：

```text
LFramework.Hotfix.Entry.HotfixEntryStart
```

所以如果热更主程序集没找到，或 `logicMainDllName` 配错，后续 `LSystemApplication` 根本不会创建。

## 3. Provider

### 定位

Provider 是随 Procedure 生命周期管理的系统服务，继承：

```csharp
SystemProviderBase
```

Provider 适合承载某个流程中的业务系统，例如战斗数据、玩法管理、网络同步、局内状态等。

### 绑定方式

Provider 用：

```csharp
[BelongTo(procedureState, lifeCycle, sort)]
```

绑定流程。

字段含义：

- `procedureState`：绑定哪个 ProcedureState。
- `lifeCycle`：当前流程释放，还是 Forever。
- `sort`：初始化排序，数值越大越先初始化。

### 生命周期

注册时由 `LSystemApplication.TryRegisterProvider` 驱动：

```text
ReferencePool.Acquire
  -> RegisterCommonDataSync
  -> LServices.Register(interfaceType)
  -> LServices.Register(providerType)
  -> sort desc
  -> Injection.Inject
  -> AwakeComponent
  -> SubscribeEvent
  -> SetUp
```

每帧：

```text
LSystemApplication.Update
  -> provider.UpdateComponent
```

释放：

```text
UnSubscribeEvent
  -> UnRegisterCommonDataSync
  -> LServices.Unregister
  -> Injection.ClearReflectionCache
  -> ReferencePool.Release
```

### SystemProviderBase 提供的能力

- `[Inject] protected EventComponent EventComponent`
- `AwakeComponent`
- `SubscribeEvent`
- `SetUp`
- `UpdateComponent`
- `UnSubscribeEvent`
- `OnStop`
- `Fire`
- `FireNow`

Provider 自己只需要重写虚方法，不要手动绕过注册/释放流程。

## 4. World

### 定位

World 是当前 Procedure 的上下文容器，继承：

```csharp
WorldBase
```

一个 Procedure 进入时最多注册一个当前 World，保存在：

```csharp
LSystemApplication._worldBase
```

World 适合承载“当前世界/当前玩法场景”的组合状态，比如一场战斗、一个大地图、一个房间。

### 注册方式

World 也用：

```csharp
[BelongTo(procedureState)]
```

注册时由 `LSystemApplication.TryRegisterWorld` 扫描。

### 初始化顺序

```text
ReferencePool.Acquire(worldType)
  -> Injection.Inject(world)
  -> LServices.Register(interfaceType)
  -> LServices.Register(worldType)
  -> world.Initialized()
```

`WorldBase.Initialized()` 内部顺序：

```text
ResolveFrameworkDependencies
Subscribe(EventComponent)
RegisterWorldHelper
InitializeAllHelpers
OnInitialized
```

### World 与 Procedure 的关系

`HotfixProcedureBase.OnEnter` 会在 World 创建后执行：

```csharp
_linkWorld = world;
_linkWorld.LinkProcedure(this);
```

因此：

- Procedure 可通过 `GetLinkWorld<TWorld>()` 获取当前 World。
- World 可通过 `GetLinkProcedure<TProcedure>()` 获取当前 Procedure。

这是 Procedure 与 World 的双向关联。

### 更新与释放

每帧：

```text
LSystemApplication.Update -> WorldBase.Update
LSystemApplication.LateUpdate -> WorldBase.LateUpdate
```

释放时：

```text
TryUnRegisterWorld
  -> LServices.Unregister(worldType/interfaceType)
  -> Injection.ClearReflectionCache
  -> ReferencePool.Release(world)
```

`WorldBase.Clear()` 会清理事件、Helper、更新列表和业务反初始化。

## 5. WorldHelper

### 定位

WorldHelper 是 World 内部的子系统，继承：

```csharp
WorldHelperBase
```

它用于把 World 拆成多个可组合模块，例如：

- 地图加载 Helper
- 玩家出生 Helper
- 相机 Helper
- 怪物刷新 Helper
- 战斗结算 Helper
- 局内 UI Helper

### 自动绑定

Helper 用：

```csharp
[AutoWorldHelper(typeof(SomeWorld))]
```

绑定到一个或多个 World 类型。

`AutoRegisterWorldHelper.GetRegisterWorldHelper(worldType)` 会：

1. 从 `HotfixComponent.GetTypesFromAttribute<AutoWorldHelperAttribute>()` 获取所有 Helper 类型。
2. 判断 `BindWorldType` 是否包含当前 World 类型。
3. 要求类型继承 `WorldHelperBase`。
4. 用 `ReferencePool.Acquire` 创建 Helper。

### 注册到 World

`WorldBase.RegisterWorldHelper()` 会：

1. `BuildWorldHelpers(helper)`。
2. 如果 Helper 实现 `IWorldUpdate`，加入 `_worldUpdates`。
3. 如果 Helper 实现 `IWorldLateUpdate`，加入 `_worldLateUpdates`。
4. `helper.SetWorld(this)`。
5. 用 Helper 的业务接口注册到 `LServices`。
6. 对所有 Helper 执行 `LServices.Inject(helper)`。
7. `InitializeAllHelpers()` 调用 `helper.Initialize()`。

### Helper 生命周期

`WorldHelperBase` 要实现：

```text
Initialize
StartGame
InstantiateWorld
StopGame
Clear
```

World 提供批量调用：

```text
InstantiateHelperGame -> UniTask.WhenAll(helper.InstantiateWorld)
StartHelperGame -> helper.StartGame
StopHelperGame -> helper.StopGame
```

如果 Helper 需要每帧更新，实现：

- `IWorldUpdate`
- `IWorldLateUpdate`

## 6. 选择哪个模块

| 需求 | 放哪里 | 原因 |
| --- | --- | --- |
| 全局基础服务 | Component | 生命周期跨流程，配置驱动 |
| 热更 DLL 加载/类型扫描 | HotfixComponent | 它是热更类型索引源 |
| 当前流程业务系统 | Provider | 绑定 ProcedureState，可排序和自动释放 |
| 当前玩法上下文 | World | 一次流程一个上下文，持有 Helper |
| World 内部子系统 | WorldHelper | 拆分世界逻辑，可自动注册和更新 |
| 单个 UI 窗口逻辑 | UIBehaviour / Window | 应受 UIComponent 生命周期控制 |
| 单个实体逻辑 | EntityLogic | 应受 EntityComponent 生命周期控制 |

## 7. 模块边界警告

- 不要把业务玩法状态塞进长期 Component，否则切 Procedure 后容易残留。
- 不要让 WorldHelper 绕过 World 自己注册全局状态，Helper 应从属 World。
- 不要让 Provider 手动 new 依赖，使用 `[Inject]` 或 `LServices`。
- 不要给 Provider/World 绑定错误接口，`GetDerivedInterfaces` 会影响注册到 `LServices` 的业务接口。
- 不要把 `IInjectable` 当业务接口，它只是生成注入标记。

