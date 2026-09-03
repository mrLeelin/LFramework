# Resource 架构简化实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标：** 移除 Resource 系统的 Agent 层，简化为 Manager → Helper 两层架构

**架构：** 将 ResourceManager 从三层（Manager → Agent → Helper）简化为两层（Manager → Helper），移除所有 Agent 相关代码，直接调用 ResourceHelper

**技术栈：** C#, Unity, GameFramework, YooAsset, Addressable

---

## Task 1: 扩展 IResourceHelper 接口

**文件：**
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Resource/IResourceHelper.cs`

**步骤 1: 添加加载方法到接口**

在 `IResourceHelper` 接口中添加以下方法：

```csharp
/// <summary>
/// 加载资源
/// </summary>
void LoadAsset(string assetName, Type assetType,
               LoadAssetCallbacks callbacks, object userData);

/// <summary>
/// 加载场景
/// </summary>
void LoadScene(string sceneAssetName,
               LoadSceneCallbacks callbacks, object userData);

/// <summary>
/// 加载二进制/原始文件
/// </summary>
void LoadBinary(string binaryAssetName,
                LoadBinaryCallbacks callbacks, object userData);

/// <summary>
/// 实例化资源
/// </summary>
void InstantiateAsset(string assetName,
                      LoadAssetCallbacks callbacks, object userData);
```

**步骤 2: 提交**

```bash
git add Framework/UnityGameFramework/Libraries/GameFramework/Resource/IResourceHelper.cs
git commit -m "refactor: 扩展 IResourceHelper 接口，添加加载方法"
```

---

## Task 2: 实现 YooAssetResourceHelper 新方法

**文件：**
- Modify: `Framework/UnityGameFramework/Scripts/Runtime/Resource/YooAsset/YooAssetResourceHelper.cs`

**步骤 1: 实现 LoadAsset 方法**

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

**步骤 2: 实现 LoadScene 方法**

```csharp
public override void LoadScene(string sceneAssetName,
    LoadSceneCallbacks callbacks, object userData)
{
    var package = YooAssets.GetPackage(PackageName);
    var handle = package.LoadSceneAsync(sceneAssetName);

    handle.Completed += (op) =>
    {
        if (op.Status == EOperationStatus.Succeed)
        {
            callbacks.LoadSceneSuccessCallback?.Invoke(
                sceneAssetName, 0f, userData);
        }
        else
        {
            callbacks.LoadSceneFailureCallback?.Invoke(
                sceneAssetName, LoadResourceStatus.NotExist,
                op.LastError ?? "Load scene failed.", userData);
        }
    };
}
```

**步骤 3: 实现 LoadBinary 方法**

```csharp
public override void LoadBinary(string binaryAssetName,
    LoadBinaryCallbacks callbacks, object userData)
{
    var package = YooAssets.GetPackage(PackageName);
    var handle = package.LoadRawFileAsync(binaryAssetName);

    handle.Completed += (op) =>
    {
        if (op.Status == EOperationStatus.Succeed)
        {
            callbacks.LoadBinarySuccessCallback?.Invoke(
                binaryAssetName, op.GetRawFileData(), 0f, userData);
        }
        else
        {
            callbacks.LoadBinaryFailureCallback?.Invoke(
                binaryAssetName, LoadResourceStatus.NotExist,
                op.LastError ?? "Load binary failed.", userData);
        }
    };
}
```

**步骤 4: 实现 InstantiateAsset 方法**

```csharp
public override void InstantiateAsset(string assetName,
    LoadAssetCallbacks callbacks, object userData)
{
    var package = YooAssets.GetPackage(PackageName);
    var handle = package.LoadAssetAsync<GameObject>(assetName);

    handle.Completed += (op) =>
    {
        if (op.Status == EOperationStatus.Succeed)
        {
            var instance = handle.InstantiateAsync();
            callbacks.LoadAssetSuccessCallback?.Invoke(
                assetName, instance, 0f, userData);
        }
        else
        {
            callbacks.LoadAssetFailureCallback?.Invoke(
                assetName, LoadResourceStatus.NotExist,
                op.LastError ?? "Instantiate failed.", userData);
        }
    };
}
```

**步骤 5: 提交**

```bash
git add Framework/UnityGameFramework/Scripts/Runtime/Resource/YooAsset/YooAssetResourceHelper.cs
git commit -m "feat: 实现 YooAssetResourceHelper 加载方法"
```

---

## Task 3: 简化 ResourceManager

**文件：**
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Resource/ResourceManager.cs`

**步骤 1: 移除 Agent 相关字段**

删除以下字段：
```csharp
private readonly List<ILoadResourceAgentHelper> _agentHelpers;
private readonly LinkedList<LoadResourceTaskBase> _pendingTasks;
```

从构造函数中移除初始化：
```csharp
_agentHelpers = new List<ILoadResourceAgentHelper>();
_pendingTasks = new LinkedList<LoadResourceTaskBase>();
```

**步骤 2: 移除 AddLoadResourceAgentHelper 方法**

删除整个方法。

**步骤 3: 简化 LoadAsset 方法**

替换为：
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

    _resourceHelper.LoadAsset(assetName, assetType, callbacks, userData);
}
```

**步骤 4: 简化 LoadScene、LoadBinary、InstantiateAsset 方法**

使用相同模式简化这三个方法。

**步骤 5: 移除 Update 方法中的队列处理**

删除 `Update()` 方法中的所有队列处理代码。

**步骤 6: 移除 GetAvailableAgent 方法**

删除整个方法。

**步骤 7: 删除所有 Task 类**

删除 `LoadResourceTaskBase`、`LoadAssetTask`、`LoadSceneTask`、`LoadBinaryTask`、`InstantiateAssetTask` 类。

**步骤 8: 更新 Shutdown 方法**

移除队列和 Agent 清理代码：
```csharp
internal override void Shutdown()
{
    _resourceHelper = null;
}
```

**步骤 9: 提交**

```bash
git add Framework/UnityGameFramework/Libraries/GameFramework/Resource/ResourceManager.cs
git commit -m "refactor: 简化 ResourceManager，移除 Agent 和任务队列"
```

---

## Task 4: 更新 IResourceManager 接口

**文件：**
- Modify: `Framework/UnityGameFramework/Libraries/GameFramework/Resource/IResourceManager.cs`

**步骤 1: 移除 AddLoadResourceAgentHelper 方法**

删除方法声明。

**步骤 2: 提交**

```bash
git add Framework/UnityGameFramework/Libraries/GameFramework/Resource/IResourceManager.cs
git commit -m "refactor: 从 IResourceManager 移除 AddLoadResourceAgentHelper"
```

---

## Task 5: 更新 ResourceComponent

**文件：**
- Modify: `Framework/UnityGameFramework/Scripts/Runtime/Resource/ResourceComponent.cs`

**步骤 1: 移除 Agent 创建逻辑**

找到并删除所有创建和添加 `LoadResourceAgentHelper` 的代码。

**步骤 2: 提交**

```bash
git add Framework/UnityGameFramework/Scripts/Runtime/Resource/ResourceComponent.cs
git commit -m "refactor: 从 ResourceComponent 移除 Agent 创建逻辑"
```

---

## Task 6: 删除 Agent 相关文件

**文件：**
- Delete: `Framework/UnityGameFramework/Libraries/GameFramework/Resource/ILoadResourceAgentHelper.cs`
- Delete: `Framework/UnityGameFramework/Scripts/Runtime/Resource/LoadResourceAgentHelperBase.cs`
- Delete: `Framework/UnityGameFramework/Scripts/Runtime/Resource/YooAsset/YooAssetLoadResourceAgentHelper.cs`
- Delete: `Framework/UnityGameFramework/Scripts/Runtime/Resource/Addressable/AddressableLoadResourceAgentHelper.cs`

**步骤 1: 删除文件**

```bash
git rm Framework/UnityGameFramework/Libraries/GameFramework/Resource/ILoadResourceAgentHelper.cs
git rm Framework/UnityGameFramework/Scripts/Runtime/Resource/LoadResourceAgentHelperBase.cs
git rm Framework/UnityGameFramework/Scripts/Runtime/Resource/YooAsset/YooAssetLoadResourceAgentHelper.cs
git rm Framework/UnityGameFramework/Scripts/Runtime/Resource/Addressable/AddressableLoadResourceAgentHelper.cs
```

**步骤 2: 提交**

```bash
git commit -m "refactor: 删除 Agent 相关文件"
```

---

## Task 7: 验证功能

**步骤 1: 打开 Unity 编辑器**

确保项目编译成功，无错误。

**步骤 2: 测试资源加载**

在 Unity 中测试加载各类资源（Prefab、Texture、AudioClip 等）。

**步骤 3: 测试场景加载**

测试场景加载和卸载功能。

**步骤 4: 验证完成**

确认所有功能正常工作。

---

## 完成标准

- [ ] 所有 Agent 相关代码已移除
- [ ] ResourceManager 直接调用 ResourceHelper
- [ ] 编译无错误
- [ ] 资源加载功能正常
- [ ] 场景加载功能正常
- [ ] 代码量减少 ~500 行
