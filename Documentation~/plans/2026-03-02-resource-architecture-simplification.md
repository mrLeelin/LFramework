# Resource 架构简化设计文档

**日期：** 2026-03-02
**状态：** 已批准
**作者：** Claude Code

## 概述

简化 Resource 功能架构，移除冗余的 Agent 层，将三层架构（Manager → Agent → Helper）简化为两层（Manager → Helper）。

## 背景

当前的 Resource 系统使用了 Agent 模式来管理资源加载的并发控制：

```
ResourceManager
    ↓ 管理多个 Agent + 任务队列
ILoadResourceAgentHelper (并发控制层) - 多实例
    ↓
IResourceHelper (平台适配层)
    ↓
YooAsset / Addressable (底层 API)
```

**问题：**
1. YooAsset 和 Addressable 内部已经实现了队列机制和并发控制
2. Agent 层的 `IsBusy` 标志只是简单的布尔值，不提供真正的并发控制
3. ResourceManager 的任务队列是多余的，因为底层 API 已经支持异步并发
4. 增加了不必要的代码复杂度（~500 行）和维护成本

## 目标架构

简化为两层架构：

```
ResourceManager (管理器)
    ↓ 直接调用
IResourceHelper (平台适配层)
    ↓
YooAsset / Addressable (底层 API)
```

## 详细设计

### 1. 移除的组件

**接口和基类：**
- `ILoadResourceAgentHelper` 接口
- `LoadResourceAgentHelperBase` 基类

**实现类：**
- `YooAssetLoadResourceAgentHelper`
- `AddressableLoadResourceAgentHelper`

**ResourceManager 内部：**
- `List<ILoadResourceAgentHelper> _agentHelpers` 字段
- `LinkedList<LoadResourceTaskBase> _pendingTasks` 字段
- `AddLoadResourceAgentHelper()` 方法
- `GetAvailableAgent()` 方法
- `Update()` 方法中的任务队列处理逻辑
- 所有 `LoadResourceTaskBase` 及其子类（`LoadAssetTask`、`LoadSceneTask`、`LoadBinaryTask`、`InstantiateAssetTask`）

### 2. IResourceHelper 接口扩展

新增以下方法（从 Agent 层移入）：

```csharp
public interface IResourceHelper
{
    // 现有方法
    void InitializeResources(ResourceInitCallBack callback);
    HasAssetResult HasAsset(string assetName);
    void Release(object asset);
    void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData);

    // 新增方法
    void LoadAsset(string assetName, Type assetType,
                   LoadAssetCallbacks callbacks, object userData);

    void LoadScene(string sceneAssetName,
                   LoadSceneCallbacks callbacks, object userData);

    void LoadBinary(string binaryAssetName,
                    LoadBinaryCallbacks callbacks, object userData);

    void InstantiateAsset(string assetName,
                          LoadAssetCallbacks callbacks, object userData);
}
```

### 3. ResourceManager 简化

**简化后的 LoadAsset 实现：**

```csharp
public void LoadAsset(string assetName, Type assetType, int priority,
                      LoadAssetCallbacks callbacks, object userData)
{
    if (string.IsNullOrEmpty(assetName))
        throw new GameFrameworkException("Asset name is invalid.");
    if (callbacks == null)
        throw new GameFrameworkException("Load asset callbacks is invalid.");
    if (_resourceHelper == null)
        throw new GameFrameworkException("Resource helper is not set.");

    // 直接调用 Helper，不再使用 Agent 和任务队列
    _resourceHelper.LoadAsset(assetName, assetType, callbacks, userData);
}
```

**注意：** `priority` 参数保留在接口中（向后兼容），但不再传递给 Helper。

### 4. 数据流

**简化前：**
```
用户调用 → ResourceManager → 检查可用 Agent →
    如果有空闲: Agent.LoadAsset() → YooAsset API
    否则: 加入队列 → Update() 中处理 → Agent.LoadAsset()
```

**简化后：**
```
用户调用 → ResourceManager → 参数验证 →
    ResourceHelper.LoadAsset() → YooAsset API (内部队列管理)
```

### 5. YooAssetResourceHelper 实现

```csharp
public override void LoadAsset(string assetName, Type assetType,
    LoadAssetCallbacks callbacks, object userData)
{
    var package = YooAssets.GetPackage(PackageName);
    var handle = package.LoadAssetAsync(assetName, assetType);

    handle.Completed += (op) =>
    {
        if (op.Status == EOperationStatus.Succeed)
        {
            callbacks.LoadAssetSuccessCallback?.Invoke(
                assetName, op.AssetObject, 0f, userData);
        }
        else
        {
            callbacks.LoadAssetFailureCallback?.Invoke(
                assetName, LoadResourceStatus.NotExist,
                op.LastError ?? "Load failed.", userData);
        }
    };
}
```

## 错误处理

**ResourceManager 层：**
- 参数验证（空字符串、null 回调等）
- Helper 未设置检查
- 抛出 `GameFrameworkException`

**ResourceHelper 层：**
- YooAsset/Addressable 操作失败时通过回调返回错误
- 错误信息从底层 API 传递到 GameFramework 回调

**移除的错误处理：**
- ~~Agent 繁忙状态~~
- ~~任务队列满~~
- ~~Agent 分配失败~~

## 迁移策略

### 影响范围

**GameFramework 核心库（需修改）：**
- `IResourceManager.cs` - 移除 `AddLoadResourceAgentHelper()`
- `ResourceManager.cs` - 移除 Agent 和队列逻辑
- `IResourceHelper.cs` - 新增加载方法
- 删除 `ILoadResourceAgentHelper.cs`

**UnityGameFramework 运行时（需修改）：**
- `ResourceComponent.cs` - 移除 Agent 创建逻辑
- `YooAssetResourceHelper.cs` - 实现新增方法
- `AddressableResourceHelper.cs` - 实现新增方法
- 删除 `LoadResourceAgentHelperBase.cs`
- 删除 `YooAssetLoadResourceAgentHelper.cs`
- 删除 `AddressableLoadResourceAgentHelper.cs`

**配置文件（需修改）：**
- `ResourceComponentSetting.cs` - 移除 Agent 相关配置

### 向后兼容性

**破坏性改动：**
- 移除 `ILoadResourceAgentHelper` 接口
- 移除 `IResourceManager.AddLoadResourceAgentHelper()` 方法

**影响评估：**
- 如果有外部代码依赖 `ILoadResourceAgentHelper` 接口，需要适配
- 建议在 CHANGELOG 中明确标注为 Breaking Change

## 测试策略

### 集成测试（必需）

**YooAsset 集成测试：**
- 加载各类资源（Prefab、Texture、AudioClip 等）
- 加载场景
- 加载二进制文件
- 实例化资源
- 资源卸载

**Addressable 集成测试：**
- 同上

**错误场景测试：**
- 加载不存在的资源
- Helper 未初始化时加载
- 无效参数传递

### 验证清单

**功能验证：**
- [ ] 所有资源加载类型正常工作
- [ ] 回调正确触发（成功/失败）
- [ ] 资源生命周期管理正常
- [ ] 场景加载/卸载正常

**性能验证：**
- [ ] 并发加载性能未下降（应该相同或更好）
- [ ] 内存占用减少（移除了 Agent 和任务队列）
- [ ] 无额外的 GC 压力

**代码质量验证：**
- [ ] 移除的代码行数 > 500 行
- [ ] 无编译错误
- [ ] 无编译警告
- [ ] 代码复杂度降低

## 预期收益

1. **代码简化**：移除 ~500 行冗余代码
2. **架构清晰**：职责更明确，层次更清晰
3. **维护成本降低**：减少需要维护的抽象层
4. **性能保持或提升**：无额外的队列管理开销
5. **依赖底层优化**：充分利用 YooAsset/Addressable 的内部优化

## 风险和缓解

**风险：**
- 破坏性改动可能影响外部代码

**缓解措施：**
1. 在 CHANGELOG 中明确标注 Breaking Change
2. 提供迁移指南
3. 保留当前分支作为回滚点
4. 充分的集成测试验证

## 实施计划

详细的实施计划将在单独的实施计划文档中制定。

## 参考

- YooAsset 文档：https://yooasset.com/
- Addressable 文档：https://docs.unity3d.com/Packages/com.unity.addressables@latest
