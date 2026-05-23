# 🚀 C# 并发编程实战：从基础到精通

> **系统化学习 C# 并发编程**：21 篇系列博客 + 可运行代码示例，从入门到精通

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Stars](https://img.shields.io/github/stars/Naughtyhusky/csharp-concurrency-cookbook?style=social)](https://github.com/Naughtyhusky/csharp-concurrency-cookbook)
[![Progress](https://img.shields.io/badge/进度-9%2F21-brightgreen)]()

---

## 📌 为什么需要这个项目？

你是否遇到过这些困惑：
- ❓ **并发、并行、异步** 到底有什么区别？
- ❓ 什么时候用 `async/await`，什么时候用 `Parallel`？
- ❓ 为什么我的 API 又慢又卡，10 个并发就崩溃？
- ❓ `Task` 和线程是什么关系？为什么说 `Task` 不等于线程？

**本项目通过可运行的代码示例 + 详细注释**，帮你彻底搞懂这些问题。

---

## ✨ 项目特点

### 🎯 代码即文档
- ✅ **每个示例都能运行**：不是代码片段，而是完整的可运行项目
- ✅ **详细的注释**：每一行关键代码都有解释
- ✅ **性能对比**：实测数据，量化性能差异

### 📚 循序渐进
- ✅ **从概念到实战**：先理解全局，再深入细节
- ✅ **从简单到复杂**：从 Hello World 到生产级模式
- ✅ **从误区到最佳实践**：避免 90% 的开发者都踩过的坑

### 🔧 真实场景
- ✅ **Web API 优化**：从 100 QPS 到 10000 QPS
- ✅ **批量数据处理**：如何充分利用多核 CPU
- ✅ **混合型任务**：I/O + CPU 的正确组合方式

---

## 🚀 快速开始

### 1️⃣ 克隆仓库

```bash
git clone https://github.com/Naughtyhusky/csharp-concurrency-cookbook.git
cd csharp-concurrency-cookbook
```

### 2️⃣ 运行示例

```bash
# 运行所有示例
dotnet run --project Overview

# 或者在 Visual Studio 中直接运行
```

### 3️⃣ 查看代码

所有示例都在 `Overview/` 文件夹中，每个文件都有详细注释。

---

## 📂 项目结构

```
csharp-concurrency-cookbook/
│
├── Blogs/                          # 📚 系列博客文章（21篇规划，已完成7篇）
│   ├── 01-并发编程全景图-博客版.md
│   ├── 02-并发的底层-Thread-ThreadPool-Task.md
│   ├── 03-Task-API完全指南.md
│   ├── 04-async-await原理与性能优化.md
│   ├── 05-SynchronizationContext与死锁问题.md
│   ├── 06-CancellationToken与超时控制.md
│   ├── 07-异步异常处理-AggregateException的拆解.md
│   ├── 08-异步编程最佳实践与反模式.md
│   ├── 09-异步编程中的内存泄漏.md
│   └── 大纲.md                     # 📋 完整系列大纲（21篇）
│
├── Overview/                       # 第01章：并发编程全景图
│   ├── Program.cs                  # 程序入口
│   ├── ConcurrencyDemo.cs          # 并发示例
│   ├── ParallelDemo.cs             # 并行示例
│   ├── AsyncDemo.cs                # 异步示例
│   ├── TaskTypeDemo.cs             # 任务类型识别
│   └── CommonMistakesDemo.cs       # 常见误区
│
├── Threads/                        # 第02章：并发的底层
│   ├── Program.cs                  # 程序入口
│   ├── ThreadVsTaskDemo.cs         # Thread vs Task 对比
│   ├── FalseSharingDemo.cs         # False Sharing 演示
│   └── ...
│
├── TaskAPI/                        # 第03章：Task API 完全指南
│   ├── Program.cs                  # 程序入口
│   ├── Demo01_TaskCreation.cs      # 创建任务
│   ├── Demo02_TaskWaiting.cs       # 等待任务
│   ├── Demo03_TaskComposition.cs   # 组合任务（WhenAll/WhenAny）
│   ├── Demo04_TaskContinuation.cs  # 任务延续
│   ├── Demo05_TaskStatus.cs        # 任务状态
│   ├── Demo06_CommonPitfalls.cs    # 常见陷阱
│   └── Demo07_PracticalExamples.cs # 实战示例
│
├── AsyncAwait/                     # 第04章：async/await 原理与性能优化
│   ├── Program.cs                  # 程序入口
│   ├── Demo01_AsyncBasics.cs       # async/await 基础
│   ├── Demo02_StateMachine.cs      # 状态机原理
│   ├── Demo03_ThreadComparison.cs  # 线程使用对比
│   ├── Demo04_SynchronizationContext.cs # 上下文捕获
│   ├── Demo05_ValueTask.cs         # ValueTask 性能优化
│   ├── Demo06_CommonPitfalls.cs    # 常见陷阱
│   └── Demo07_PracticalExamples.cs # 实战示例
│
├── SyncContext/                    # 第05章：SynchronizationContext 与死锁
│   ├── Program.cs                  # 程序入口
│   ├── DeadlockDemo.cs             # 死锁演示
│   ├── ConfigureAwaitDemo.cs       # ConfigureAwait 用法
│   └── ...
│
├── SyncContext.Winform/            # 第05章：WinForms 死锁示例
│   └── (WinForms 项目)
│
├── CancellationToken/              # 第06章：CancellationToken 与超时控制
│   ├── Program.cs                  # 程序入口
│   ├── BasicCancellationDemo.cs    # 基础取消示例
│   ├── TimeoutDemo.cs              # 超时控制
│   ├── LinkedCancellationDemo.cs   # 链接取消令牌
│   └── ...
│
├── CancellationToken.WebApi.Demo/  # 第06章：Web API 取消示例
│   └── (Web API 项目)
│
├── ExceptionHandling/              # 第07章：异步异常处理
│   ├── Program.cs                  # 程序入口
│   ├── Extensions/
│   │   └── TaskExtensions.cs       # SafeWhenAll、SafeFireAndForget
│   └── Demos/
│       ├── BasicExceptionHandling.cs        # await vs Wait/Result
│       ├── WhenAllExceptionHandling.cs      # WhenAll 异常处理
│       ├── SafeWhenAllDemo.cs               # SafeWhenAll 扩展方法
│       ├── ApiAggregatorDemo.cs             # 实战：API 聚合器
│       ├── FireAndForgetDemo.cs             # 后台任务异常处理
│       └── AggregateExceptionAdvanced.cs    # Flatten/Handle 高级用法
│
├── BestPractices/                  # 第08章：异步编程最佳实践与反模式
│   ├── Program.cs                  # 程序入口
│   ├── AntiPatterns/
│   │   ├── AsyncVoidDemo.cs        # async void 危害演示
│   │   ├── AsyncOverSyncDemo.cs    # 异步转同步死锁
│   │   ├── FireAndForgetDemo.cs    # Fire-and-Forget 与 Task.Run 滥用
│   │   └── AsyncDisposableDemo.cs  # async using 资源泄漏
│   └── GoodPractices/
│       ├── NamingConventionDemo.cs # 命名规范
│       ├── ConfigureAwaitDemo.cs   # ConfigureAwait 最佳实践
│       ├── ValueTaskDemo.cs        # ValueTask 正确用法
│       └── SyncCallingAsyncDemo.cs # 同步调用异步的安全写法
│
├── MemoryLeaks/                    # 第09章：异步编程中的内存泄漏
│   ├── Program.cs                  # 程序入口
│   ├── Leaks/
│   │   ├── CtsLeakDemo.cs          # CancellationTokenSource 泄漏
│   │   ├── EventLeakDemo.cs        # 事件订阅泄漏
│   │   ├── TaskLeakDemo.cs         # Task 集合与状态机泄漏
│   │   ├── TimerLeakDemo.cs        # Timer / PeriodicTimer 泄漏
│   │   └── ChannelLeakDemo.cs      # Channel 未 Complete 泄漏
│   └── Fixes/
│       ├── CtsFixDemo.cs           # CTS 修复方案
│       ├── EventFixDemo.cs         # 事件订阅修复方案
│       ├── TaskFixDemo.cs          # Task 集合修复方案
│       ├── TimerFixDemo.cs         # Timer 修复方案
│       └── ChannelFixDemo.cs       # Channel 修复方案
│
├── README.md                       # 📖 本文件
└── ConcurrencyCookbook.sln         # 解决方案
```

> **进度更新**：本项目是 21 篇系列教程，当前已完成：**9/21 章节** (42.9%)  
> 📖 **博客** + 💻 **代码示例** 齐全！

---

## 🎓 已完成章节

### ✅ 第01章：并发编程全景图

**学习目标**：理解并发、并行、异步的区别，学会根据场景选择技术

**💻 代码位置**：`Overview/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `ConcurrencyDemo.cs` | 并发示例 | 一个人如何"同时"做多件事 |
| `ParallelDemo.cs` | 并行示例 | 多核 CPU 并行计算 |
| `AsyncDemo.cs` | 异步示例 | I/O 操作不阻塞线程 |
| `TaskTypeDemo.cs` | 任务类型识别 | 如何判断 CPU/IO 密集型 |
| `CommonMistakesDemo.cs` | 常见误区 | 90% 的人都犯过的错误 |

**运行方式**：
```bash
dotnet run --project Overview
```

---

### ✅ 第02章：并发的底层（Thread、ThreadPool、Task）

**学习目标**：深入理解 Thread、ThreadPool 和 Task 的本质区别和底层机制

**💻 代码位置**：`Threads/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `ThreadVsTaskDemo.cs` | Thread vs Task 对比 | 创建 10000 个 Thread vs Task 的性能差异 |
| `FalseSharingDemo.cs` | False Sharing 演示 | CPU 缓存行竞争导致的性能问题 |

**运行方式**：
```bash
dotnet run --project Threads
```

**核心知识点**：
- 🧵 Thread 的创建成本和内存开销
- 🔄 ThreadPool 的工作窃取算法
- 📦 Task 的本质（异步操作抽象）
- ⚡ I/O 密集型 Task 不占用线程的原理
- 🚫 False Sharing：多线程性能杀手

---

### ✅ 第03章：Task API 完全指南

**学习目标**：系统掌握 Task 类的核心 API，为 async/await 打下坚实基础

**💻 代码位置**：`TaskAPI/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `Demo01_TaskCreation.cs` | 创建任务 | Task.Run vs Task.Factory.StartNew |
| `Demo02_TaskWaiting.cs` | 等待任务 | Wait()、WaitAll、WaitAny |
| `Demo03_TaskComposition.cs` | 组合任务 | WhenAll、WhenAny、WhenEach |
| `Demo04_TaskContinuation.cs` | 任务延续 | ContinueWith vs await |
| `Demo05_TaskStatus.cs` | 任务状态 | TaskStatus 枚举 |
| `Demo06_CommonPitfalls.cs` | 常见陷阱 | 闭包陷阱、死锁、异常吞没 |
| `Demo07_PracticalExamples.cs` | 实战示例 | 超时控制、重试、并发限流 |

**运行方式**：
```bash
dotnet run --project TaskAPI
```

**核心知识点**：
- 🎨 创建任务的三种方式及选择
- ⏳ 等待任务的正确姿势
- 🔀 组合任务（WhenAll/WhenAny）
- 🔗 任务延续的最佳实践
- ⚠️ 常见陷阱及解决方案

---

### ✅ 第04章：async/await 原理与性能优化

**学习目标**：深入理解 async/await 的编译器魔法，掌握性能优化技巧

**💻 代码位置**：`AsyncAwait/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `Demo01_AsyncBasics.cs` | 基础回顾 | async/await 语义规则 |
| `Demo02_StateMachine.cs` | 状态机原理 | 编译器生成的状态机 |
| `Demo03_ThreadComparison.cs` | 线程使用对比 | I/O 异步不占用线程 |
| `Demo04_SynchronizationContext.cs` | 上下文捕获 | ConfigureAwait(false) |
| `Demo05_ValueTask.cs` | 性能优化 | ValueTask<T> 减少堆分配 |
| `Demo06_CommonPitfalls.cs` | 常见陷阱 | async void、死锁、过度异步化 |
| `Demo07_PracticalExamples.cs` | 实战示例 | 缓存、并发控制、超时 |

**运行方式**：
```bash
dotnet run --project AsyncAwait
```

**核心知识点**：
- 🔮 状态机原理：编译器魔法揭秘
- 🧵 线程真相：I/O 异步不占用线程
- 🚀 性能优化：ValueTask 减少 GC 压力
- 📝 上下文捕获：ConfigureAwait 的正确使用
- ⚠️ 常见陷阱：async void 的危害

---

### ✅ 第05章：SynchronizationContext 与死锁问题

**学习目标**：彻底搞懂 SynchronizationContext，理解死锁的根本原因

**💻 代码位置**：`SyncContext/` 和 `SyncContext.Winform/` 文件夹

**核心知识点**：
- 🔒 死锁产生的三个条件
- 📱 UI 线程与 SynchronizationContext
- ✅ ConfigureAwait(false) 的使用场景
- 🌐 ASP.NET Core 为什么没有 SynchronizationContext
- 💡 库代码的最佳实践

**运行方式**：
```bash
# 控制台示例
dotnet run --project SyncContext

# WinForms 示例（需在 Visual Studio 中运行）
```

---

### ✅ 第06章：CancellationToken 与超时控制

**学习目标**：掌握 CancellationToken 的正确使用，实现优雅的取消和超时控制

**💻 代码位置**：`CancellationToken/` 和 `CancellationToken.WebApi.Demo/` 文件夹

**核心知识点**：
- 🚫 协作式取消机制
- ⏱️ 超时控制的三种方式
- 🔗 链接取消令牌（LinkedTokenSource）
- 🌐 Web API 中的取消令牌传递
- 💡 资源释放的正确姿势

**运行方式**：
```bash
# 控制台示例
dotnet run --project CancellationToken

# Web API 示例
dotnet run --project CancellationToken.WebApi.Demo
```

---

### ✅ 第07章：异步异常处理

**学习目标**：掌握异步异常处理的正确姿势，理解 AggregateException 的设计思想

**💻 代码位置**：`ExceptionHandling/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `BasicExceptionHandling.cs` | 基础异常处理 | await vs Wait/Result 的异常差异 |
| `WhenAllExceptionHandling.cs` | WhenAll 异常处理 | 三种解决方案对比 |
| `SafeWhenAllDemo.cs` | SafeWhenAll 扩展方法 | 优雅的异常处理 |
| `ApiAggregatorDemo.cs` | 实战场景 | API 聚合器 + 容错处理 |
| `FireAndForgetDemo.cs` | 后台任务异常处理 | SafeFireAndForget 扩展方法 |
| `AggregateExceptionAdvanced.cs` | 高级用法 | Flatten() 和 Handle() |

**运行方式**：
```bash
dotnet run --project ExceptionHandling
```

**核心知识点**：
- 🎯 AggregateException 的设计思想
- ⚖️ await vs Wait/Result 的异常行为差异
- 🔄 Task.WhenAll 的三种异常处理方案
- 🔥 后台任务（Fire-and-Forget）的异常处理
- 🔧 实用的扩展方法：SafeWhenAll、SafeFireAndForget
- ✅ 最佳实践清单（Do's and Don'ts）

**实用扩展方法**：
```csharp
// SafeWhenAll：同时返回成功和失败的结果
var (successes, failures) = await tasks.SafeWhenAll();

// SafeFireAndForget：安全的后台任务执行
DoWorkAsync().SafeFireAndForget(ex =>
{
    _logger.LogError(ex, "后台任务失败");
});
```

---

### ✅ 第08章：异步编程最佳实践与反模式

**学习目标**：辨别 7 大异步反模式，掌握对应最佳实践，写出生产级异步代码

**💻 代码位置**：`BestPractices/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `AntiPatterns/AsyncVoidDemo.cs` | async void 反模式 | 异常吞没、无法 await |
| `AntiPatterns/AsyncOverSyncDemo.cs` | 异步转同步 | .Result / .Wait() 死锁复现 |
| `AntiPatterns/FireAndForgetDemo.cs` | Fire-and-Forget | 委托化错误处理扩展方法 |
| `AntiPatterns/AsyncDisposableDemo.cs` | 资源释放 | async using 正确用法 |
| `GoodPractices/ConfigureAwaitDemo.cs` | ConfigureAwait | 库代码 vs 应用代码的区别 |
| `GoodPractices/ValueTaskDemo.cs` | ValueTask | 缓存路径零分配 |
| `GoodPractices/SyncCallingAsyncDemo.cs` | 同步调用异步 | 工厂方法 / AsyncHelper 安全写法 |

**运行方式**：
```bash
dotnet run --project BestPractices
```

**核心知识点**：
- 🚫 async void 的危害：异常无法捕获、无法追踪生命周期
- 🔒 死锁的三个触发条件与 ConfigureAwait(false) 解法
- 🔥 Fire-and-Forget 的委托化设计模式
- ⚡ ValueTask 的适用场景：高频调用 + 同步路径占多数
- 🏭 同步上下文中安全调用异步的工厂方法模式

---

### ✅ 第09章：异步编程中的内存泄漏

**学习目标**：识别 5 类异步内存泄漏根因，掌握诊断工具与修复方案

**💻 代码位置**：`MemoryLeaks/` 文件夹

| 文件 | 说明 | 核心内容 |
|------|------|---------|
| `Leaks/CtsLeakDemo.cs` | CTS 泄漏 | 带超时的 CTS 循环堆积 Timer |
| `Leaks/EventLeakDemo.cs` | 事件订阅泄漏 | 未 -= 导致 publisher 持有 subscriber |
| `Leaks/TaskLeakDemo.cs` | Task 集合泄漏 | 无限增长的 Task 列表 + 状态机闭包 |
| `Leaks/TimerLeakDemo.cs` | Timer 泄漏 | Timer / PeriodicTimer 未 Dispose |
| `Leaks/ChannelLeakDemo.cs` | Channel 泄漏 | Writer 未 Complete 导致 Reader 永久挂起 |
| `Fixes/` | 修复方案 | 每个场景的 using / WeakReference / BoundedChannel 修复 |

**运行方式**：
```bash
dotnet run --project MemoryLeaks
```

**核心知识点**：
- 🔑 泄漏本质：GC 没坏，是仍存在强引用链
- ⏱️ CancellationTokenSource 带超时时内部会创建 Timer，必须 Dispose
- 📡 事件订阅必须配对取消，或改用 WeakReference / 弱事件
- 📦 Task 状态机闭包会捕获并持有大对象，及时清理列表
- 🔄 Channel 必须在生产者完成后调用 `TryComplete()`，否则消费者永久阻塞

---

## 💡 核心代码示例

### 示例 1：并发做饭（理解并发）

```csharp
// 来自 ConcurrencyDemo.cs
// 一个人同时处理多个任务
private static async Task ConcurrentCookingAsync()
{
    Console.WriteLine("开始做饭（并发模式）");

    // 启动三个异步任务
    var task1 = StirFryAsync();   // 炒菜
    var task2 = MakeSoupAsync();   // 煮汤
    var task3 = SteamRiceAsync();  // 蒸米饭

    // 等待所有任务完成
    await Task.WhenAll(task1, task2, task3);

    Console.WriteLine("所有菜都做好了！");
}

// 运行后你会发现：所有任务可能在同一个线程上完成！
// 这就是并发的魔力：通过异步切换，一个人也能"同时"做多件事
```

### 示例 2：并行计算质数（理解并行）

```csharp
// 来自 ParallelDemo.cs
// 使用多核 CPU 并行处理
private static void DemonstrateCpuBoundTask()
{
    var data = Enumerable.Range(1, 1_000_000).ToArray();

    // 使用 PLINQ 并行处理
    var primes = data
        .AsParallel()      // ⭐ 魔法在这里！
        .Where(IsPrime)
        .ToArray();

    Console.WriteLine($"找到 {primes.Length} 个质数");
}

// 性能提升：4 核 CPU 上约 2.5-3.5 倍
```

### 示例 3：异步 Web API（理解异步）

```csharp
// 来自 AsyncDemo.cs
// ❌ 错误：同步阻塞（吞吐量 100 QPS）
public IActionResult GetUserData(int userId)
{
    var user = _db.Users.FirstOrDefault(u => u.Id == userId);
    return Ok(user);
}

// ✅ 正确：异步非阻塞（吞吐量 10000 QPS）
public async Task<IActionResult> GetUserDataAsync(int userId)
{
    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    return Ok(user);
}

// 性能提升：100 倍！
// 关键：等待数据库期间，线程被释放去处理其他请求
```

### 示例 4：任务类型识别（避免踩坑）

```csharp
// 来自 TaskTypeDemo.cs
// 核心判断标准：任务在等什么？

// CPU 密集型：等 CPU 计算
var primes = data.AsParallel().Where(IsPrime).ToArray();

// I/O 密集型：等网络/磁盘
var data = await httpClient.GetStringAsync(url);

// ❌ 常见错误：I/O 操作使用 Task.Run
var badResult = await Task.Run(async () => 
{
    return await httpClient.GetAsync(url);  // 浪费线程！
});

// ✅ 正确做法：直接 await
var goodResult = await httpClient.GetAsync(url);
```

---

## 🔥 常见误区（CommonMistakesDemo.cs）

### 误区 1："async 就是多线程"

```csharp
❌ 错误认知：加了 async 就会创建新线程
✅ 真相：async 只是语法糖，生成状态机

// 证明
public async Task TestAsync()
{
    Console.WriteLine($"Before: {Thread.CurrentThread.ManagedThreadId}");
    await Task.Delay(1000);  // 异步等待
    Console.WriteLine($"After: {Thread.CurrentThread.ManagedThreadId}");
}

// 运行结果：线程 ID 可能相同！
```

### 误区 2："Task.Run 能提升性能"

```csharp
// ❌ 错误：浪费线程
var data = await Task.Run(async () => 
{
    await Task.Delay(500);  // I/O 操作
    return "Data";
});

// ✅ 正确：直接 await
await Task.Delay(500);
var data = "Data";

// 原因：Task.Delay 已经是异步的，不需要 Task.Run
```

### 误区 3："Task 就是线程"

```csharp
✅ 真相：
- I/O 密集型 Task：在等待期间不占用线程（使用 I/O 完成端口）
- CPU 密集型 Task：使用线程池线程（通常 < 100 个）

// 验证：创建 10000 个 I/O 密集型 Task
var tasks = Enumerable.Range(1, 10000)
    .Select(_ => Task.Delay(5000))
    .ToArray();

await Task.WhenAll(tasks);
// 线程池线程数：几乎没增加！
```

---

## 📊 性能对比实测

### 案例 1：Web API 查询订单

```csharp
// 场景：查询数据库（50ms） + 调用物流 API（100ms） + 调用支付 API（80ms）
```

| 实现方式 | 响应时间 | 吞吐量 | 性能提升 |
|---------|---------|-------|---------|
| 同步阻塞 | 230ms | 100 QPS | - |
| 异步串行 | 230ms | **10000 QPS** | **100x** ⬆️ |
| 异步并发 | **100ms** | **10000 QPS** | **2.3x + 100x** ⬆️ |

**关键代码**：

```csharp
// 异步并发版本（最优）
public async Task<IActionResult> GetOrderAsync(int orderId)
{
    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

    // 并发调用两个 API
    var logisticsTask = _logisticsClient.GetAsync(order.TrackingNo);
    var paymentTask = _paymentClient.GetAsync(order.PaymentId);

    await Task.WhenAll(logisticsTask, paymentTask);

    return Ok(new { order, logisticsTask.Result, paymentTask.Result });
}
```

### 案例 2：批量图像处理

| 实现方式 | 耗时 | 性能提升 |
|---------|------|---------|
| 单线程串行 | 10s | - |
| Parallel.ForEach | **2.8s** | **3.5x** ⬆️ |

**关键代码**：

```csharp
// 并行处理
Parallel.ForEach(imagePaths, path =>
{
    var image = LoadImage(path);
    var processed = ApplyFilters(image);  // CPU 密集
    SaveImage(processed, path);
});
```

---

## 🎯 核心技术决策树

```
任务在等什么？
     │
     ├─ 等 CPU 计算 ────→ CPU 密集型 ────→ 使用 Parallel/PLINQ
     │                                   
     │
     ├─ 等 I/O 完成 ────→ I/O 密集型 ────→ 使用 async/await
     │                                   
     │
     └─ 两者都有 ───────→ 混合型 ────────→ 组合使用
```

### 速查表

| 场景 | 任务类型 | 推荐技术 | 避免使用 |
|------|---------|---------|---------|
| Web API 调用 | I/O 密集 | `async/await` + `HttpClient` | `Task.Run` |
| 数据库查询 | I/O 密集 | `async/await` + EF Core Async | `.Result` / `.Wait()` |
| 文件读写 | I/O 密集 | `async/await` + `Stream` | 同步 I/O |
| 图像处理 | CPU 密集 | `Parallel.ForEach` 或 `PLINQ` | `async/await` |
| 大数据筛选 | CPU 密集 | `PLINQ` | 普通 LINQ |
| 视频编码 | CPU 密集 | `Parallel` + `Task.Run` | 单线程 |

---

## 🛠️ 技术栈

- **框架**：.NET 10
- **语言**：C# 14.0
- **IDE**：Visual Studio 2026 / VS Code / Rider

### 环境要求

```bash
# 检查 .NET 版本
dotnet --version
# 应该显示：10.x.x

# 如果没有安装 .NET 10
# 下载：https://dotnet.microsoft.com/download
```

---

## 📚 系列大纲（21章规划）

> 完整系列大纲请查看：[大纲.md](Blogs/大纲.md)

### 📊 整体进度：9/21 章节（42.9%）

#### **第一篇：基础篇（2章）** ✅ 已完成

| 章节 | 状态 | 主题 | 博客 | 代码 |
|------|------|------|------|------|
| 01 | ✅ | 并发编程全景图 | [📖](Blogs/01-并发编程全景图-博客版.md) | `Overview/` |
| 02 | ✅ | Thread、ThreadPool 与 Task | [📖](Blogs/02-并发的底层-Thread-ThreadPool-Task.md) | `Threads/` |

#### **第二篇：异步基础篇（4章）** ✅ 已完成

| 章节 | 状态 | 主题 | 博客 | 代码 |
|------|------|------|------|------|
| 03 | ✅ | Task API 完全指南 | [📖](Blogs/03-Task-API完全指南.md) | `TaskAPI/` |
| 04 | ✅ | async/await 原理与性能优化 | [📖](Blogs/04-async-await原理与性能优化.md) | `AsyncAwait/` |
| 05 | ✅ | SynchronizationContext 与死锁 | [📖](Blogs/05-SynchronizationContext与死锁问题.md) | `SyncContext/` |
| 06 | ✅ | CancellationToken 与超时控制 | [📖](Blogs/06-CancellationToken与超时控制.md) | `CancellationToken/` |

#### **第三篇：异步进阶篇（3章）** ✅ 已完成

| 章节 | 状态 | 主题 | 博客 | 代码 |
|------|------|------|------|------|
| 07 | ✅ | 异步异常处理 | [📖](Blogs/07-异步异常处理-AggregateException的拆解.md) | `ExceptionHandling/` |
| 08 | ✅ | 异步编程最佳实践与反模式 | [📖](Blogs/08-异步编程最佳实践与反模式.md) | `BestPractices/` |
| 09 | ✅ | 异步编程中的内存泄漏 | [📖](Blogs/09-异步编程中的内存泄漏.md) | `MemoryLeaks/` |

#### **第四篇：并行与同步篇（5章）** 📅 计划中

| 章节 | 状态 | 主题 |
|------|------|------|
| 10 | 📅 | Parallel 与 PLINQ |
| 11 | 📅 | 线程同步完全指南 |
| 12 | 📅 | 并发集合与线程安全 |
| 13 | 📅 | ThreadLocal 与 AsyncLocal |
| 14 | 📅 | 无锁编程与内存模型 |

#### **第五篇：高级模式篇（3章）** 📅 计划中

| 章节 | 状态 | 主题 |
|------|------|------|
| 15 | 📅 | TPL Dataflow 流水线 |
| 16 | 📅 | IAsyncEnumerable 异步流 |
| 17 | 📅 | Background Service 后台服务 |

#### **第六篇：性能实战篇（4章）** 📅 计划中

| 章节 | 状态 | 主题 |
|------|------|------|
| 18 | 📅 | 限流与并发控制 |
| 19 | 📅 | 高性能优化实战 |
| 20 | 📅 | 性能诊断与调优 |
| 21 | 📅 | 实战项目：同步改异步迁移 |

---

1. ✅ **运行示例**：`dotnet run --project Overview`
2. ✅ **阅读代码**：按顺序查看：
   - `Program.cs` - 了解整体结构
   - `ConcurrencyDemo.cs` - 理解并发
   - `ParallelDemo.cs` - 理解并行
   - `AsyncDemo.cs` - 理解异步
   - `TaskTypeDemo.cs` - 学会识别任务类型
   - `CommonMistakesDemo.cs` - 避免常见错误
3. ✅ **实战应用**：应用到你的项目中

---

## 🎯 适合谁学习？

### ✅ 适合你，如果你是：

- **初学者**：想系统学习 C# 并发编程
- **.NET 开发者**：想优化代码性能
- **后端工程师**：想提高 API 吞吐量
- **面试准备者**：想深入理解异步原理

### ⚠️ 前置知识

- 基本的 C# 语法
- LINQ 基础
- 了解什么是线程（不需要深入）

---

## 📖 学习建议

### 🎯 入门路线（1-2周）

#### 第一周：建立全局认知

**Day 1-2：第01章 - 并发编程全景图**
- 📖 阅读博客：理解并发、并行、异步的本质区别
- 💻 运行代码：`dotnet run --project Overview`
- ✍️ 小练习：识别你项目中的 CPU 密集型和 I/O 密集型任务

**Day 3-4：第02章 - Thread、ThreadPool 与 Task**
- 📖 阅读博客：理解 Task 的本质，为什么 Task ≠ 线程
- 💻 运行代码：`dotnet run --project Threads`
- ✍️ 小练习：对比 Thread 和 Task 的内存开销

#### 第二周：掌握 Task 和 async/await

**Day 5-7：第03章 - Task API 完全指南**
- 📖 阅读博客：掌握 Task 的核心 API
- 💻 运行代码：`dotnet run --project TaskAPI`
- ✍️ 小练习：用 WhenAll 实现并发 API 请求

### 🚀 进阶路线（2-3周）

**Week 3：async/await 原理**
- 📖 第04章：理解状态机原理、ValueTask 优化
- 💻 运行代码：`dotnet run --project AsyncAwait`
- ✍️ 实战：优化你的 API 接口，用 ValueTask 减少 GC

**Week 4：避免死锁和取消控制**
- 📖 第05章：SynchronizationContext 与死锁
- 📖 第06章：CancellationToken 与超时控制
- 💻 运行代码：`dotnet run --project SyncContext` 和 `CancellationToken`
- ✍️ 实战：为你的 API 添加超时和取消支持

**Week 5：异常处理**
- 📖 第07章：异步异常处理
- 💻 运行代码：`dotnet run --project ExceptionHandling`
- ✍️ 实战：实现 SafeWhenAll 和 SafeFireAndForget 扩展方法

### 💡 学习建议

- ✅ **先博客，后代码**：先阅读博客理解概念，再运行代码加深印象
- ✅ **动手实践**：每章都有小练习，一定要自己敲一遍代码
- ✅ **循序渐进**：严格按章节顺序学习，不要跳跃（前面的知识是后面的基础）
- ✅ **应用到项目**：学完一章后，立即尝试应用到自己的项目中
- ✅ **多思考**：遇到问题时，先思考"为什么"，再查答案

---

## 🛠️ 技术栈

- **框架**：.NET 10
- **语言**：C# 14.0
- **IDE**：Visual Studio 2026 / VS Code / Rider

### 环境要求

```bash
# 检查 .NET 版本
dotnet --version
# 应该显示：10.x.x

# 如果没有安装 .NET 10
# 下载：https://dotnet.microsoft.com/download
```

---

## 🤝 贡献指南

欢迎贡献代码、提出建议或报告问题！

### 如何贡献

1. **Fork** 本仓库
2. 创建特性分支：`git checkout -b feature/amazing-feature`
3. 提交变更：`git commit -m 'Add amazing feature'`
4. 推送分支：`git push origin feature/amazing-feature`
5. 提交 **Pull Request**

### 贡献要求

- ✅ 代码示例应该简洁、可运行
- ✅ 添加详细的中文注释
- ✅ 提交前运行所有示例，确保无编译错误
- ✅ 一个 PR 解决一个问题
- ✅ 遵循项目现有的代码风格

---

## 📄 许可证

本项目采用 **MIT 许可证**，详见 [LICENSE](LICENSE) 文件。

---

## ⭐ Star History

[![Star History Chart](https://api.star-history.com/svg?repos=Naughtyhusky/csharp-concurrency-cookbook&type=Date)](https://star-history.com/#Naughtyhusky/csharp-concurrency-cookbook&Date)


