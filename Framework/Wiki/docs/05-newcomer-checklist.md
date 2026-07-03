# 05. 新人接手清单

## 第一天必须跑通

1. 在 Unity 中打开主工程。
2. 打开 `LFramework/GameSetting` 窗口，确认能看到框架设置、构建入口、Luban、本地 CDN 服务等页面。
3. 找到当前工程的 `ProjectSettingSelector`。
4. 确认 `ResourceComponentSetting.ResourceMode`。
5. 确认入口 Procedure 配置。
6. 进入 Play Mode，观察启动管线日志和热更入口日志。

## 必须知道的代码入口

| 问题 | 起点 |
| --- | --- |
| 启动卡住 | `LaunchPipelineRunner.RunAsync` |
| 资源初始化失败 | `InitResourceTask`、`ResourceComponentSetting` |
| 版本检查失败 | `CheckVersionTask` |
| 资源下载失败 | `DownloadResourceTask` |
| 热更 DLL 没加载 | `LoadAssemblyTask`、`HotfixComponent`、`HybridCLRSetting` |
| 热更入口不执行 | `HotfixEntryTask`、`Entry.HotfixEntryStart` |
| Procedure 不切换 | `ProcedureComponentSetting`、`ProcedureComponent` |
| Provider 没注册 | `LSystemApplication.RegisterProvider.cs` |
| World 没注册 | `LSystemApplication.RegisterWorld.cs` |
| 构建失败 | `BuildOrchestrator`、`BuildPipelineRunner` |

## 改框架前的边界判断

1. 是框架通用能力，改 `Framework/LFramework`。
2. 是业务玩法逻辑，放到业务工程或热更业务层，不要塞进框架。
3. 是配置数据，优先改 Setting 资产和 Setting 类型。
4. 是平台打包差异，改 `IPlatformConfig` 或对应构建任务。
5. 是资源系统差异，先判断 YooAsset/Addressables 后端，不要混用路径假设。

## 提交前检查

1. 只改必要文件，避免吸收主工程 unrelated dirty files。
2. 如果新增 Unity 资产，确认 `.meta` 是否需要一起提交。
3. 配置变更要说明影响的运行时读取路径。
4. 构建逻辑变更要说明走 App 构建还是资源更新构建。
5. 启动逻辑变更要说明影响哪条 Pipeline。
6. DI/Provider/World 变更要说明生命周期和释放路径。

## 常见误区

| 误区 | 正确理解 |
| --- | --- |
| 只看 `git status` 根仓库 | `Assets/Frame` 是嵌套包时，要进入 `Assets/Frame` 看包内状态 |
| 配置资产存在就等于运行时可读 | Player 下需要 `Resources/ProjectSettingSelector` 能被加载 |
| `HasAsset` 只判断 key 是否存在 | Addressables 下还会判断下载大小和本地就绪状态 |
| `[Inject]` 一定是运行时反射 | 当前框架有源码生成器路径，优先保持生成注入一致性 |
| Provider 注册顺序随文件顺序 | 实际按 `BelongToAttribute.Sort` 排序 |

