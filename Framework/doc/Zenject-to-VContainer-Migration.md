# Zenject → VContainer 迁移文档

## 概述

本项目已从 Zenject 依赖注入框架完全迁移到 VContainer (jp.hadashikick.vcontainer 1.17.0)。Zenject 库及所有相关代码已从项目中删除。

本文档记录迁移后的 DI 架构、关键 API 变更、已知限制和后续优化方向。

## 迁移范围

- 删除 `Assets/Framework/Framework/Zenject/` 整个目录
- 删除 `PreLoadZenjectAttribute`
- 迁移约 30 个文件的 `using Zenject` → `using VContainer`
- 重构核心启动流程（`UnitySystemApplicationBehaviour`、`LSystemApplicationBehaviour`）
- 重构 Hotfix 层（`LSystemApplication` 及其 partial class）
- 重构运行时组件（`ViewComponent`、`WorldBase`、`DefaultUIFormHelper`）
- 新建 `MonoBehaviourValidationAttribute`（从 Zenject 命名空间迁移到 `LFramework.Runtime`）
- 新建 `FrameworkGameObjectInjector`（替代 `DiContainerExtensions`）

## DI 架构

### Scope 层级

```
Root Scope（应用生命周期）
├── GameFrameworkComponent 实例 ×28
├── Settings（GameSetting, HybridCLRSetting, ...）
├── ISystemApplication
│
└── Hotfix Scope（热更新生命周期）
    ├── ISystemProviderRegister
    ├── IWorldRegister
    ├── 热更组件实例
    │
    └── Procedure Scope（流程生命周期）
        └── 随流程创建/销毁
```

### 核心类

| 类 | 职责 |
|---|---|
| `FrameworkResolverContext` | 管理 Root/Hotfix/Procedure 三层 resolver，`ActiveResolver` 按优先级返回最内层 |
| `FrameworkInjector` | 封装 `ActiveResolver.Inject()`，统一注入入口 |
| `HotfixScopeRegistry` | 管理 Hotfix Scope 的创建和销毁 |
| `RuntimeProcedureScopeRegistry` | 管理 Procedure Scope 的创建和销毁，支持安装回调 |
| `LFrameworkAspect` | 框架核心单例，持有上述所有实例 |
| `FrameworkGameObjectInjector` | GameObject 注入工具，仅注入标记了 `[MonoBehaviourValidation]` 的 MonoBehaviour |

### 启动流程

```
LSystemApplicationBehaviour.Awake()
  └── StartApplication()
        ├── new FrameworkResolverContext()
        ├── new ContainerBuilder()
        ├── RegisterSetting()          → ScopeBuilder.RegisterInstance(setting).As(type)
        ├── RegisterComponents()       → 创建 28 个 GameFrameworkComponent
        ├── BindComponents()           → ScopeBuilder.RegisterInstance(component).As(type)
        ├── OnConfigureRootScope()     → builder.RegisterInstance<ISystemApplication>(this)
        ├── Build()                    → RootResolver = builder.Build()
        ├── resolverContext.SetRoot()
        ├── new LFrameworkAspect(resolverContext)
        ├── ResolveApplicationDependencies()  → FrameworkInjector.Inject(每个组件)
        ├── StartComponents()          → AwakeComponent() + StartComponent()
        ├── ApplicationStarted()
        └── SetUpComponents()
```

## Zenject → VContainer API 对照

| Zenject | VContainer | 说明 |
|---|---|---|
| `DiContainer.Bind<T>().FromInstance(x).AsSingle()` | `IContainerBuilder.RegisterInstance<T>(x)` | 只能在 scope 构建阶段调用 |
| `DiContainer.Inject(obj)` | `IObjectResolver.Inject(obj)` / `FrameworkInjector.Inject(obj)` | 任意时刻 |
| `DiContainer.Resolve<T>()` | `IObjectResolver.Resolve<T>()` / `LFrameworkAspect.Get<T>()` | scope 存活期间 |
| `DiContainer.Unbind<T>()` | `scope.Dispose()` | VContainer 不支持运行时 Unbind，通过 scope 生命周期管理 |
| `DiContainer.Bind(type).WithId(id)` | `Dictionary<(Type, string), T>` | VContainer 不支持 keyed binding，ViewComponent 用本地字典 |
| `DiContainer.HasBinding<T>()` | `IObjectResolver.TryResolve<T>(out _)` | — |
| `[Inject]`（Zenject 命名空间） | `[Inject]`（VContainer 命名空间） | 语法相同，命名空间不同 |

## [Inject] 属性注入注意事项

VContainer 的 `[Inject]` 属性注入与 Zenject 有一个关键差异：

**VContainer 要求属性必须有 setter。** Zenject 可以通过反射直接写入 getter-only 属性的 backing field，VContainer 不行。

```csharp
// 错误：VContainer 会抛 ArgumentException: Set Method not found
[Inject] private EventComponent EventComponent { get; }

// 正确：private 属性用 { get; set; }
[Inject] private EventComponent EventComponent { get; set; }

// 正确：protected 属性用 { get; private set; }
[Inject] protected EventComponent EventComponent { get; private set; }
```

规则：
- `private` 属性 → `{ get; set; }`（不能加 `private set;`，C# 不允许 accessor 与属性同级）
- `protected` 属性 → `{ get; private set; }`

## ViewModel 管理

ViewComponent 的 ViewModel 管理从 Zenject 的 keyed binding 改为本地字典：

```csharp
// 之前（Zenject）
DiContainer.Bind<IViewModel>().WithId(bindingId).FromInstance(vm);
DiContainer.HasBindingId(bindingId);
DiContainer.UnbindId(bindingId);

// 现在（VContainer）
Dictionary<(Type, string), IViewModel> _viewModelCache;
_viewModelCache[key] = vm;
_viewModelCache.TryGetValue(key, out var existing);
_viewModelCache.Remove(key);
```

## Provider/World 生命周期

之前 Zenject 支持运行时 `Bind/Unbind`，Provider 和 World 可以随时注册/注销。

现在 VContainer 的 scope 是只读的，Provider/World 的生命周期与 Procedure Scope 绑定：
- 进入流程 → `EnterProcedureScope()` 创建 scope
- 注册 Provider/World → `FrameworkInjector.Inject()` 注入，本地字典管理
- 离开流程 → `ExitProcedureScope()` 销毁 scope

## 性能说明

### 迁移带来的改善
- 容器构建阶段分配更少（VContainer 内部用 Span/栈分配）
- 消除了运行时 Bind/Unbind 的中间对象分配
- Zenject 的 TypeAnalyzer 缓存开销消除

### 当前限制
- `[Inject]` 属性注入仍走反射（`PropertyInfo.SetValue`），与 Zenject 相同
- MonoBehaviour 组件无法使用构造函数注入
- 注入开销集中在启动阶段（一次性），运行时不会再触发

### 后续优化方向
- 非 MonoBehaviour 类（`LSystemApplication`、`SystemProviderBase`、`WorldBase` 等）可改为构造函数注入，走 VContainer 代码生成路径，零反射
- MonoBehaviour 组件可考虑在 `AwakeComponent()` 中手动 `LFrameworkAspect.Instance.Get<T>()` 替代 `[Inject]`，避免反射

## 文件变更清单

### 删除的文件
- `Assets/Framework/Framework/Zenject/` 整个目录
- `PreLoadZenjectAttribute.cs`
- `DiContainerExtensions.cs`

### 新建的文件
- `FrameworkGameObjectInjector.cs` — 替代 DiContainerExtensions
- `MonoBehaviourValidationAttribute.cs` — 从 Zenject 命名空间迁移

### 重构的文件（核心）
- `UnitySystemApplicationBehaviour.cs` — 启动流程改用 ContainerBuilder
- `LSystemApplicationBehaviour.cs` — 适配新的 scope 构建
- `LFrameworkAspect.cs` — 移除 DiContainer，仅保留 FrameworkResolverContext
- `LSystemApplication.cs` — 构造函数改用 HotfixScopeRegistry
- `LSystemApplication.RegisterProvider.cs` — 移除 Bind/Unbind
- `LSystemApplication.RegisterWorld.cs` — 移除 Bind/Unbind
- `ViewComponent.cs` — 本地字典替代 keyed binding
- `WorldBase.cs` — 移除 Bind/Unbind
- `HotfixProcedureBase.cs` — 直接接口调用替代反射
- `DefaultUIFormHelper.cs` — 使用 FrameworkGameObjectInjector
- `DefaultEntityHelper.cs` — 使用 FrameworkGameObjectInjector

### 简单替换的文件
- `SystemProviderBase.cs`、`LanguageComponent.cs`、`ResourceDownloadComponent.cs`、`TableComponent.cs`、`HotfixComponent.cs`、`GameNotificationsComponent.cs`、`UIChildEntityLogic.cs` — `using Zenject` → `using VContainer`，getter-only 属性加 setter
- `BaseComponent.cs`、`GameEntry.cs`、`ConfigComponent.cs`、`ISystemApplication.cs`、`TableManager.cs` — 移除 `using Zenject`
- `Entry.cs` — 移除 Zenject 预加载优化
- `Window.cs` — 移除 `[PreLoadZenject]`
- Sample 文件、测试文件、asmdef 文件 — 适配 VContainer

### 其他清理
- `link.xml` — 移除 Zenject 程序集条目
- `HostLinkXmlInstaller.cs` — 移除 Zenject 程序集名称
- `AOTGenericReferences.cs` — 移除 Zenject 泛型引用
- `WorkFlowEngine.cs`、`SequenceLineComponent.cs` — `DiContainer.Inject` → `FrameworkInjector.Inject`
- `StringExtension.cs`、`SequenceLineChunkGroup.cs` — 移除 `using ModestTree`（Zenject 子库）
