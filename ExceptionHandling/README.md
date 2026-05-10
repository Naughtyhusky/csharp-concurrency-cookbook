# 异步异常处理 - 示例代码

## 📖 概述

本项目包含《C# 并发编程实战：从基础到精通》第 07 章的所有示例代码：
- **AggregateException 的设计思想**
- **await vs Wait/Result 的异常行为差异**
- **Task.WhenAll 的异常处理问题和解决方案**
- **SafeWhenAll 和 SafeFireAndForget 扩展方法**
- **实战场景：API 聚合器**
- **后台任务异常处理**
- **Flatten() 和 Handle() 高级用法**

---

## 🎯 学习目标

通过本章示例，你将学会：
1. ✅ 理解 AggregateException 的设计思想
2. ✅ 掌握 await 和 Wait/Result 的区别
3. ✅ 处理 Task.WhenAll 中的多个异常
4. ✅ 实现容错的并发任务执行器
5. ✅ 正确处理后台任务的异常
6. ✅ 使用 Flatten() 和 Handle() 高级功能

---

## 🚀 快速开始

### 方式1：Visual Studio
1. 打开解决方案 `ConcurrencyCookbook.sln`
2. 将 `ExceptionHandling` 设置为启动项目（右键项目 → 设为启动项目）
3. 按 `F5` 运行

### 方式2：命令行
```bash
cd D:\Work\ConcurrencyCookbook\ExceptionHandling
dotnet run
```

---

## 📚 示例列表

### 1. await vs Wait/Result 异常行为对比
- **演示内容**: 两种方式的异常类型差异
- **关键文件**: `BasicExceptionHandling.cs`
- **核心概念**: `await` 抛出原始异常，`Wait()` 抛出 `AggregateException`

### 2. Task.WhenAll 只抛出第一个异常
- **演示内容**: WhenAll 的异常陷阱
- **关键文件**: `WhenAllExceptionHandling.cs`
- **问题**: 多个任务失败时，只能看到第一个异常

### 3. 解决方案1：手动检查 Task.Exception
- **演示内容**: 通过 `Task.Exception` 获取所有异常
- **关键文件**: `WhenAllExceptionHandling.cs`
- **优点**: 可以获取所有异常信息

### 4. 解决方案2：逐个 await
- **演示内容**: 逐个 await 每个任务
- **关键文件**: `WhenAllExceptionHandling.cs`
- **优点**: 可以单独处理每个任务的异常

### 5. 解决方案3：SafeWhenAll 扩展方法
- **演示内容**: 封装的扩展方法
- **关键文件**: `SafeWhenAllDemo.cs`, `TaskExtensions.cs`
- **优点**: 封装良好，可复用，同时获取成功和失败的结果

### 6. 实战场景：API 聚合器
- **演示内容**: 并发调用 API + 容错处理
- **关键文件**: `ApiAggregatorDemo.cs`
- **特性**: 
  - 支持部分失败
  - 最低成功数量要求
  - 记录所有失败信息

### 7. 后台任务异常处理
- **演示内容**: Fire-and-Forget 的异常处理
- **关键文件**: `FireAndForgetDemo.cs`, `TaskExtensions.cs`
- **问题**: 后台任务的异常容易被吞掉
- **解决方案**: 
  - `SafeFireAndForget` 扩展方法
  - `BackgroundService` 参考实现

### 8. AggregateException.Flatten() 用法
- **演示内容**: 扁平化嵌套异常
- **关键文件**: `AggregateExceptionAdvanced.cs`
- **使用场景**: 嵌套的 Task.WhenAll

### 9. AggregateException.Handle() 用法
- **演示内容**: 选择性处理异常
- **关键文件**: `AggregateExceptionAdvanced.cs`
- **使用场景**: 忽略某些类型的异常，重新抛出其他异常

---

## 🔧 核心扩展方法

### SafeWhenAll<T>
```csharp
var (successes, failures) = await tasks.SafeWhenAll();
```
**功能**: 返回成功和失败的结果，不丢失任何异常信息

### SafeFireAndForget
```csharp
DoWorkAsync().SafeFireAndForget(ex =>
{
    _logger.LogError(ex, "后台任务失败");
});
```
**功能**: 安全的后台任务执行，支持自定义异常处理

---

## 📂 项目结构

```
ExceptionHandling/
├── Program.cs                              # 主程序入口
├── Extensions/
│   └── TaskExtensions.cs                   # SafeWhenAll 和 SafeFireAndForget
└── Demos/
    ├── BasicExceptionHandling.cs           # 示例1：await vs Wait/Result
    ├── WhenAllExceptionHandling.cs         # 示例2-4：WhenAll 异常处理
    ├── SafeWhenAllDemo.cs                  # 示例5：SafeWhenAll
    ├── ApiAggregatorDemo.cs                # 示例6：API 聚合器
    ├── FireAndForgetDemo.cs                # 示例7：后台任务
    └── AggregateExceptionAdvanced.cs       # 示例8-9：高级用法
```

---

## 💡 学习建议

1. **按顺序学习**: 从示例1开始，逐步深入
2. **动手实践**: 修改代码，观察不同的行为
3. **阅读注释**: 代码中有详细的注释说明
4. **结合博客**: 配合博客文章阅读，理解设计思想

---

## 🎯 常见问题

### Q1: 为什么推荐使用 await 而不是 Wait()？
A: 
- `await` 不会阻塞线程，性能更好
- `await` 避免死锁风险（特别是在 UI 线程）
- `await` 抛出原始异常，代码更简洁

### Q2: 什么时候需要 SafeWhenAll？
A:
- 需要同时获取成功和失败的结果
- 实现容错处理（允许部分失败）
- 不希望丢失任何异常信息

### Q3: SafeFireAndForget 和直接 Fire-and-Forget 有什么区别？
A:
- 直接 `_ = task` 会吞掉异常
- `SafeFireAndForget` 提供异常处理机制
- 可以记录日志、发送告警等

---

## 📖 相关资源

- **博客文章**: `Blogs/07-异步异常处理-AggregateException的拆解.md`
- **快速参考**: `Blogs/Summaries/07/快速参考.md`
- **代码说明**: `Blogs/Summaries/07/代码示例说明.md`
- **GitHub 仓库**: [csharp-concurrency-cookbook](https://github.com/Naughtyhusky/csharp-concurrency-cookbook/tree/dev)

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

- 发现 Bug？请提交 Issue
- 有改进建议？欢迎 PR
- 想补充示例？请联系作者

---

## 📝 许可

本项目采用 MIT 许可证。详见 LICENSE 文件。
