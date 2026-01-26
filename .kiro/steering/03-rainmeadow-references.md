---
inclusion: manual
---
# RainMeadow引用管理

由于我们无法直接修改RainMeadow原版代码,我们需要通过反射和引用管理来访问原版的类型和方法。

## 引用加载

在[References.cs](mdc:src/References.cs)中,我们通过Assembly.Load加载RainMeadow程序集:

```csharp
RainMeadowAssembly = Assembly.Load("Rain Meadow");
```

这样可以获取RainMeadow的程序集引用,为后续的反射操作做准备。

## 访问原版类型

在需要访问原版类型时,我们通过RainMeadowAssembly.GetType获取:

```csharp
Type chatHudType = References.RainMeadowAssembly.GetType("RainMeadow.ChatHud");
```

## 访问原版方法

获取到类型后,可以通过反射获取方法:

```csharp
MethodInfo logMessageMethod = chatLogManagerType.GetMethod("LogMessage", 
    BindingFlags.Public | BindingFlags.Static);
```

## 关键RainMeadow类型

以下是我们项目中使用的关键RainMeadow类型:

1. **ChatHud** - 负责显示聊天界面
2. **ChatLogManager** - 负责管理聊天消息
3. **RainMeadow.PlayerController** - 管理玩家控制器

## 查找和分析原版代码

分析原版代码时,可以遵循以下步骤:

1. 通过反射列出相关类的所有方法和属性
2. 使用Hook时记录方法参数和返回值,了解其行为
3. 注意跟踪类之间的依赖关系
