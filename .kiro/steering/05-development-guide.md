---
inclusion: manual
---
# 开发指南与最佳实践

本文档提供了开发"雨甸中文输入"Mod的指南和最佳实践。

## 版本兼容性

由于我们依赖于RainMeadow原版代码,需要特别注意版本兼容性:

1. 在反射获取类型和方法时,要考虑不同版本可能的变化
2. 使用try-catch块包裹可能因版本变化而失败的代码
3. 实现版本检查机制,在不兼容的版本上给出明确提示

## 调试技巧

在[DebugHandler.cs](mdc:src/DebugHandler.cs)中,我们提供了多种日志级别:

```csharp
public static void Log(string message)
public static void LogWarning(string message)
public static void LogError(string message, Exception ex = null)
```

开发新功能时,请使用这些方法记录关键信息,便于调试。

## 通过反射探索原版代码

要了解原版RainMeadow的代码结构,可以使用以下技术:

```csharp
// 获取类型的所有公共方法
MethodInfo[] methods = someType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

// 获取类型的所有公共属性
PropertyInfo[] properties = someType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

// 遍历并打印方法信息
foreach (var method in methods)
{
    DebugHandler.Log($"方法: {method.Name}, 参数数量: {method.GetParameters().Length}");
}
```

## 测试策略

开发新功能时,建议采用以下测试策略:

1. 首先确认原版功能的正常工作方式
2. 实现基本Hook,验证能否成功拦截调用
3. 逐步添加自定义逻辑,频繁测试以确保兼容性
4. 在不同场景和游戏状态下测试功能

## 资源清理

确保在mod禁用或游戏退出时正确清理资源:

```csharp
public void OnDisable()
{
    // 清理ChatHud Hook
    ChatHudHook.Cleanup();
    // 清理ChatLogManager Hook
    ChatLogManagerHook.Cleanup();
}
```
