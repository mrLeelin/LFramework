# WorldResident：跨 World 的驻留对象

`WorldResidentBase` 用于在指定的多个 World 之间保留数据和业务对象，例如大地图的实体生成、实体回收和地图状态。实例由框架自动创建、注入与释放。

## 声明与使用

将类型放在框架已经加载并扫描的 Hotfix 程序集中。`WorldResidentAttribute` 使用现有 Hotfix 类型索引发现类型；不需要加入组件配置，也不需要手动注册宿主。

```csharp
using LFramework.Runtime;
using UnityGameFramework.Runtime;

public interface IMapEntityService
{
    int VisitCount { get; set; }
}

[WorldResident(typeof(WorldA), typeof(WorldB), typeof(WorldC))]
public sealed partial class MapEntityResident : WorldResidentBase, IMapEntityService
{
    [Inject] private EntityComponent Entities { get; set; }

    public int VisitCount { get; set; }

    protected override void OnInitialize()
    {
        // 依赖已经注入；加载跨 World 保留的地图状态。
    }

    protected override void OnEnterWorld(WorldBase world)
    {
        VisitCount++;
        // World 已初始化；挂接当前 World 的表现、订阅当前 World 的事件。
    }

    protected override void OnExitWorld(WorldBase world)
    {
        // World 和 Provider 尚未释放；取消本次挂接，清除当前 World 的临时引用。
        // 跨 World 保留的地图数据与实体不在这里销毁。
    }

    protected override void OnShutdown()
    {
        // 取消订阅，使用 Entities 回收本对象负责的实体，释放资源。
        // 注入或初始化失败也可能走到这里，需要允许依赖为空、资源仅加载了一部分。
    }
}
```

`WorldA/B/C` 应是项目中的具体 `WorldBase` 类型。含 `[Inject]` 的业务类型遵循现有注入生成器约定，声明为 `partial`。

Resident 的具体类型和业务接口自动注册到 `LServices`。World、WorldHelper、Provider 或流程可以通过现有 `[Inject]` 获取，也可以在确认处于有效范围时调用 `LServices.Get<IMapEntityService>()`。离开范围后不得继续使用之前缓存的引用。

## 生命周期

| 转换 | 行为 |
| --- | --- |
| 首次进入 A、B 或 C | 创建、注入、`OnInitialize()`，World 就绪后 `OnEnterWorld(world)` |
| A → B → C | 每次 `OnExitWorld(旧 World)` → `OnEnterWorld(新 World)`，保持实例及数据 |
| C → 未声明的 D | `OnExitWorld(C)`，随后 `OnShutdown()` 并移除服务注册，不通知进入 D |
| 进入没有 World 的流程 | 退出旧 World 后释放实例，不调用 `OnEnterWorld(null)` |
| D → A | 创建新实例，数据重新初始化 |
| 退出游戏 | 当前流程退出、Resident 退出 World、Provider/World 清理、Resident 释放、基础组件关闭 |

采用**精确类型匹配**，声明父 World 不会自动覆盖派生 World。当前框架为单活动 World 模型；此功能保持连续切换之间的实例，不提供多个同时运行 World 的隔离容器。

Resident 在目标 Provider、World 和流程注入前完成准备，因此这些对象进入时即可获取服务。框架提交目标 World 后，再释放超出目标范围的旧 Resident。旧流程与旧 World 的退出回调仍能访问旧 Resident。

`OnEnterWorld` 在 World 初始化和流程注入成功后、流程业务 `OnEnterProcedure()` 前执行；`OnExitWorld` 在流程业务退出后、Provider 和 World 回收前执行。即使连续进入相同类型的 World，也会重新通知进入；两次回调之间仅保留 Resident 实例，不保留旧 World 的附着。`OnInitialize`／`OnShutdown` 管实例资源，`OnEnterWorld`／`OnExitWorld` 管本次 World 的挂接。

类型在每次 Hotfix 加载会话中发现并缓存。应用启动自动添加 `WorldResidentHost`；自定义启动类需要保留基类 `RegisterComponents()` 调用。普通业务代码不需要调用 Host 或 Transition API。

## 顺序、依赖与帧回调

可使用 `[WorldResident(typeof(WorldA), Sort = 100)]` 指定顺序。数值大的先注入、先初始化、先进入 World、先执行帧回调，退出 World 和释放时倒序；相同值按完整类型名排序。需要某个 Resident 在另一个之前初始化时，显式设置其 `Sort`。

Resident 可以依赖应用组件及同等或更长 World 范围的 Resident。例如声明 A/B 的对象可依赖声明 A/B/C 的对象。所有目标对象先构造，再完成所有注入，最后依次初始化。禁止依赖仅覆盖自身部分 World 范围的 Resident，避免切换后持有已释放的对象。

不要跨 World 保留具体 World、WorldHelper 或流程 Provider。可在 `OnEnterWorld` 使用传入实例建立临时挂接，在 `OnExitWorld` 中解除并清空引用。它们的退出时间与 Resident 不同，不支持将其作为 Resident 的长期注入依赖。

按需实现 `IWorldResidentUpdate.Update(float, float)`、`IWorldResidentLateUpdate.LateUpdate()` 获取帧回调。这些调度接口不会注册为业务服务。准备转换期间暂停调度；初始化、进入 World、退出 World 和释放回调中不允许同步切换流程或关闭应用，需将请求留到回调结束后执行。正常帧回调和流程 `OnEnterProcedure()` 中可以切换流程。

## 实体、异步任务与失败处理

- 框架管理 Resident 本身的生命周期。业务仍需在 `OnShutdown()` 回收自己创建的 Entity、事件订阅和资源句柄。
- Entity 的实际存活还取决于业务是否将它交给 WorldHelper 或其他对象管理。需要跨 World 保留的 Entity 应由 Resident 负责回收，避免同时由旧 World 清理。
- `LifetimeToken` 在关闭前取消，可传给支持取消的异步 API。不能取消的加载回调仍需检查取消状态，并及时回收迟到的资源。
- `OnInitialize()` 是同步入口。不要使用 `async void`；异步任务应单独启动、捕获异常，并遵守 `LifetimeToken`。框架不会等待地图资源加载完成才进入 World。
- 构造、注入或框架进入阶段失败时，清理本次新增的 Resident/Provider/World，并恢复原 Resident 的可恢复注册。已经退出的旧 World 及业务外部副作用不会自动恢复。
- `OnEnterWorld` 抛错时，对本次已经尝试进入的对象倒序调用 `OnExitWorld`，然后回滚新增对象；退出回调必须允许部分进入状态。若补偿退出也失败，保留依赖，后续退出或应用关闭可重试。
- 同一时刻两个 Resident 不能占用相同业务接口，也不能覆盖已有外部服务；目标 Provider/WorldHelper 覆盖 Resident 接口会使本次进入失败并回滚。关闭只移除仍指向自身的注册，保留外部替换。
- 已提交后的旧 Resident 清理异常会被记录，并继续清理其他对象；不会撤销已进入的新 World。`OnShutdown()` 应尽可能完整地释放自身资源。
- `OnExitWorld` 失败时保留 Provider、World 和 Resident，再次退出可重试失败项，成功项不会重复通知。正常流程退出时，Provider 清理失败会保留 World/Resident 并上报异常；再次切换可重试未完成的清理，已经成功的业务退出回调不会重复执行。
- 应用关闭失败后，Hotfix 应用停止自身的帧回调，并保留关闭入口及后续清理所需的依赖。再次调用 `Dispose()` 或 `StopApplication()` 可继续清理；完成前不会提前关闭底层组件。切入 Runtime 流程时，旧 Resident 提交后的清理异常只记录日志，新流程继续注入并进入。

生命周期测试位于 `Framework/LFramework/Tests/Editor/World/WorldResidentHostTests.cs`，流程接入测试位于 `Framework/LFramework/Tests/Editor/Hotfix/WorldResidentIntegrationTests.cs`。
