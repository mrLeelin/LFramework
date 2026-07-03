# 00. 框架交接索引

这套 wiki 面向接手 `Assets/Frame/Framework` 的开发者，目标是解释框架真实运行路径，而不是只介绍目录。

## 推荐阅读顺序

1. `docs/01-startup-flow.md`：从 `LSystemApplication` 开始，追到入口 Procedure、Provider、World、WorldHelper，说明哪里算真正进入游戏。
2. `docs/03-runtime-systems.md`：详细解释 Component、HotfixComponent、Provider、World、WorldHelper 的职责边界。
3. `docs/06-worldhelper-guide.md`：按真实源码链路说明 WorldHelper 怎么写、怎么启动、怎么在文档里记录。
3. `docs/02-configuration.md`：整理 `ProjectSettingSelector`、`SettingManager`、`ComponentSetting`、`BuildSetting`。
4. `docs/04-resource-and-build.md`：解释 YooAsset/Addressables 后端和 App/资源构建管线。
5. `docs/05-newcomer-checklist.md`：作为接手排查清单。

## 核心主线

```text
HotfixEntryTask
  -> HotfixComponent.EnterHotfixAssembly
  -> Entry.HotfixEntryStart
  -> LSystemApplication
  -> RegisterHotfixComponents
  -> AddHotfixProcedure
  -> ProcedureComponent.ForceChangedProcedure
  -> HotfixProcedureBase.OnEnter
  -> TryRegisterProvider
  -> TryRegisterWorld
  -> World.Initialized
  -> RegisterWorldHelper
  -> OnEnterProcedure / World.OnInitialized
```

## 目录分工

| 路径 | 责任 |
| --- | --- |
| `LFramework/Scripts/Runtime` | 运行时框架、启动管线、组件封装、配置读取、资源适配、World/Helper 基类 |
| `LFramework/Scripts/Hotfix` | 热更入口、`LSystemApplication`、热更 Procedure、Provider/World 注册 |
| `LFramework/Scripts/Editor` | GameWindow、设置同步、构建管线、资源构建工具 |
| `UnityGameFramework/Scripts/Runtime` | Game Framework 基础组件和底层管理器 |
| `UniTask` | async/await 异步任务基础 |
| `UniRx` | 响应式事件/数据流基础 |

## 框架抽象分层

| 抽象 | 生命周期 | 主要入口 |
| --- | --- | --- |
| Component | 跨流程长期服务 | `GameFrameworkComponent`、`ComponentSetting`、`[HotfixComponent]` |
| HotfixComponent | 跨流程长期服务 | `LoadHotfixAssemblies`、`ParseHotfixAssembly`、`EnterHotfixAssembly` |
| Procedure | 流程状态机节点 | `HotfixProcedureBase.OnEnter/OnLeave` |
| Provider | 绑定 Procedure，可按生命周期释放 | `SystemProviderBase`、`[BelongTo]` |
| World | 当前 Procedure 的上下文 | `WorldBase`、`TryRegisterWorld` |
| WorldHelper | World 内部子系统 | `WorldHelperBase`、`[AutoWorldHelper]` |
