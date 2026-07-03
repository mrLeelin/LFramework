# WorldHelper 使用指南

`WorldHelper` 是挂在某个 `WorldBase` 下面的玩法子系统。它不应该被当成全局组件使用，也不应该替代 `Provider`。它适合承载“只在当前 World 存活”的逻辑，例如当前玩法的刷怪、相机、结算、关卡对象、局内引导、局内网络同步等。

## 什么时候用 WorldHelper

| 场景 | 推荐位置 | 原因 |
| --- | --- | --- |
| 跨流程、全局常驻服务 | `Component` | 生命周期跟随框架，不依赖某个 Procedure。 |
| 某个流程进入时注册的业务服务 | `Provider` | 由 ProcedureState 绑定，负责流程级服务。 |
| 当前玩法世界的上下文和编排 | `World` | World 绑定当前 Procedure，管理局内生命周期。 |
| World 内部可拆分的子系统 | `WorldHelper` | Helper 跟随 World 创建、初始化、启动、停止和释放。 |

判断标准：如果这个逻辑离开当前 World 就不应该存在，就放进 `WorldHelper`；如果它要跨 World 存活，就不要放进 `WorldHelper`。

## 注册和启动链路

```text
HotfixProcedureBase.OnEnter
  -> LSystemApplication.TryRegisterWorld(ProcedureState)
  -> WorldBase.Initialized()
      -> RegisterWorldHelper()
          -> AutoRegisterWorldHelper.GetRegisterWorldHelper(worldType)
          -> ReferencePool.Acquire(helperType)
          -> helper.SetWorld(this)
          -> LServices.Register(helperBusinessInterface, helper)
          -> LServices.Inject(helper)
      -> InitializeAllHelpers()
      -> OnInitialized()
```

`WorldBase` 只会自动完成 Helper 的创建、依赖注入和 `Initialize()`。真正的局内资源实例化、开局、停局，需要 World 自己在合适的时机调用：

- `InstantiateHelperGame()`：并行调用所有 Helper 的 `InstantiateWorld()`。
- `StartHelperGame()`：调用所有 Helper 的 `StartGame()`。
- `StopHelperGame()`：调用所有 Helper 的 `StopGame()`。

## 生命周期

| 阶段 | 谁调用 | Helper 方法 | 适合做什么 |
| --- | --- | --- | --- |
| 绑定 World | `WorldBase.RegisterWorldHelper()` | `SetWorld(WorldBase)` | 框架内部调用，不要业务侧手动调。 |
| 依赖注入 | `WorldBase.RegisterWorldHelper()` | `LServices.Inject(helper)` | 注入 Component、Provider、业务接口。 |
| 初始化 | `WorldBase.InitializeAllHelpers()` | `Initialize()` | 读取 World、缓存轻量状态、订阅局内事件。 |
| 实例化 | World 主动调用 | `InstantiateWorld()` | 异步创建实体、加载局内资源、生成对象。 |
| 开始游戏 | World 主动调用 | `StartGame()` | 打开运行开关、启动计时、允许输入或同步。 |
| 每帧逻辑 | `WorldBase.Update()` | `IWorldUpdate.OnUpdate()` | 只放局内运行中的逻辑。 |
| LateUpdate | `WorldBase.LateUpdate()` | `IWorldLateUpdate.LateUpdate()` | 相机跟随、表现后处理等。 |
| 停止游戏 | World 主动调用 | `StopGame()` | 关闭运行开关、停止计时、取消未完成行为。 |
| 释放 | `WorldBase.Clear()` | `Clear()` | 解除事件、清空引用、释放局内对象。 |

## 写一个 Helper

先定义业务接口。接口用于给外部系统通过 `LServices` 访问，不建议让外部直接依赖具体 Helper 类型。

```csharp
public interface ICombatSpawnHelper : IWorldHelper
{
    UniTask SpawnPlayerAsync();
}
```

实现 Helper，并用 `AutoWorldHelper` 绑定到目标 World。

```csharp
using Cysharp.Threading.Tasks;
using LFramework.Runtime;

[AutoWorldHelper(typeof(CombatWorld))]
public sealed class CombatSpawnHelper : WorldHelperBase, ICombatSpawnHelper, IWorldUpdate
{
    [Inject] private EntityComponent EntityComponent { get; set; }

    private CombatWorld _world;
    private bool _running;

    public override void Initialize()
    {
        _world = GetWorld<CombatWorld>();
    }

    public override async UniTask InstantiateWorld()
    {
        await SpawnPlayerAsync();
    }

    public override void StartGame()
    {
        _running = true;
    }

    public void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        if (!_running)
        {
            return;
        }

        // 局内刷怪、计时、同步等逻辑。
    }

    public override void StopGame()
    {
        _running = false;
    }

    public override void Clear()
    {
        _running = false;
        _world = null;
    }

    public UniTask SpawnPlayerAsync()
    {
        // 使用 EntityComponent 或资源组件创建玩家。
        return UniTask.CompletedTask;
    }
}
```

## World 里怎么启动 Helper

`WorldBase.Initialized()` 会在 `RegisterWorldHelper()` 和 `InitializeAllHelpers()` 之后调用 `OnInitialized()`，所以 World 可以在 `OnInitialized()` 中启动自己的异步进入流程。

```csharp
using Cysharp.Threading.Tasks;
using LFramework.Runtime;

[BelongTo(ProcedureState.Combat)]
public sealed class CombatWorld : WorldBase
{
    protected override void OnInitialized()
    {
        EnterWorldAsync().Forget();
    }

    private async UniTaskVoid EnterWorldAsync()
    {
        await InstantiateHelperGame();
        StartHelperGame();
    }

    protected override void OnDeInitialized()
    {
        StopHelperGame();
    }
}
```

## 访问 Helper 的正确方式

`WorldBase.GetWorldHelper<T>()` 当前使用 Helper 的具体类型作为字典 key。也就是说，在 World 或另一个 Helper 内部获取 Helper 时，用具体类型最稳：

```csharp
var spawnHelper = GetWorldHelper<CombatSpawnHelper>();
```

如果是 World 外部的系统，不要调用 World 的内部字典，应该通过业务接口走 `LServices`：

```csharp
var spawnHelper = LServices.Get<ICombatSpawnHelper>();
await spawnHelper.SpawnPlayerAsync();
```

注意：`AutoWorldHelper` 自动注册时会把 Helper 的业务接口注册到 `LServices`。这个接口不要只继承框架接口，应该表达业务能力，例如 `ICombatSpawnHelper`、`ICameraFollowHelper`、`ISettlementHelper`。

## 文档里应该怎么体现

在“模块树”或某个 World 页面里，`WorldHelper` 不要只写类名。每个 Helper 至少记录这些信息：

现在可以在 `html/module-tree.html` 中选择 `WorldHelper 模块`，点击 `WorldHelperBase` 或具体 Helper 子类，右侧详情会显示一份 “Wiki 记录模板”。这份模板就是每个 Helper 文档块的标准格式。

| 字段 | 写法 |
| --- | --- |
| 绑定 World | `AutoWorldHelper(typeof(CombatWorld))` |
| 业务职责 | 一句话说明它负责的局内子系统。 |
| 对外接口 | 例如 `ICombatSpawnHelper`，说明谁会通过 `LServices` 访问它。 |
| 生命周期使用 | 是否实现 `InstantiateWorld`、`StartGame`、`StopGame`、`Clear`。 |
| Update 参与 | 是否实现 `IWorldUpdate` 或 `IWorldLateUpdate`，以及运行开关是什么。 |
| 依赖项 | `[Inject]` 注入了哪些 Component、Provider 或业务接口。 |
| 创建/释放资源 | 创建了哪些实体、UI、订阅、计时器，在哪里释放。 |
| 调用关系 | 被哪个 World 启动，被哪些系统调用，对哪些 Helper 有依赖。 |

示例文档块：

```text
CombatSpawnHelper
- 绑定 World：CombatWorld
- 对外接口：ICombatSpawnHelper
- 职责：负责玩家和敌人的局内生成。
- 生命周期：InstantiateWorld 创建首批实体；StartGame 打开运行；StopGame 停止生成；Clear 清空引用和事件。
- Update：实现 IWorldUpdate，只有 _running=true 时执行。
- 依赖：EntityComponent、ResourceComponent、ICombatConfigProvider。
- 调用关系：CombatWorld.EnterWorldAsync -> InstantiateHelperGame/StartHelperGame；CombatRuleHelper 调用 SpawnEnemyAsync。
```

## 常见错误

- 把 Helper 当全局单例：离开 World 后 Helper 会被释放，跨流程状态应放到 Component 或 Provider。
- 在 Helper 字段里保存未清理的运行状态：Helper 来自 `ReferencePool`，`Clear()` 必须清干净。
- 只写 `Initialize()`，忘了在 World 里调用 `InstantiateHelperGame()` 和 `StartHelperGame()`。
- 在 World 内用接口调用 `GetWorldHelper<ICombatSpawnHelper>()`：当前字典按具体类型保存，接口访问应走 `LServices`。
- 实现 `IWorldUpdate` 后没有运行开关：停局后可能继续执行逻辑，建议用 `_running` gate。
