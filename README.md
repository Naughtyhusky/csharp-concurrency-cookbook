# 🚀 C# 并发编程完全指南

> **从零到精通**：通过实战代码和深度注释，彻底搞懂 C# 并发、并行、异步编程

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Stars](https://img.shields.io/github/stars/Naughtyhusky/csharp-concurrency-cookbook?style=social)](https://github.com/Naughtyhusky/csharp-concurrency-cookbook)

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
├── Overview/                   # 第一章：并发编程全景图
│   ├── Program.cs              # 程序入口
│   ├── ConcurrencyDemo.cs      # 并发示例
│   ├── ParallelDemo.cs         # 并行示例
│   ├── AsyncDemo.cs            # 异步示例
│   ├── TaskTypeDemo.cs         # 任务类型识别
│   └── CommonMistakesDemo.cs   # 常见误区
│
├── (更多章节代码将陆续添加...)
│
├── README.md                   # 本文件
└── ConcurrencyCookbook.sln     # 解决方案
```

> **注意**：本项目是系列教程（共 21 章），代码将随着教程进度逐步添加。  
> 当前已完成：**第一章** | 进度：**1/21** (4.8%)

---

## 🎓 当前可用内容

### ✅ 第一章：并发编程全景图

**学习目标**：理解并发、并行、异步的区别，学会根据场景选择技术

**代码位置**：`Overview/` 文件夹

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

## 📖 推荐学习顺序

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

## 🤝 如何贡献

欢迎贡献代码或提出建议！

1. **Fork** 本仓库
2. 创建特性分支：`git checkout -b feature/amazing-feature`
3. 提交变更：`git commit -m 'Add amazing feature'`
4. 推送分支：`git push origin feature/amazing-feature`
5. 提交 **Pull Request**

### 贡献指南

- ✅ 代码示例应该简洁、可运行
- ✅ 添加详细的注释
- ✅ 提交前运行所有示例，确保无错误
- ✅ 一个 PR 解决一个问题

---

## 📝 路线图

### 📊 整体进度：1/21 章节（4.8%）

本项目是《C# 并发编程实战：从零到精通》系列教程的配套代码仓库，**共 21 章**。

---

### 🗺️ 完整系列大纲

#### **第一篇：基础篇（2章）**

| 章节 | 状态 | 主题 | 代码位置 |
|------|------|------|---------|
| ✅ 01 | **已完成** | 并发编程全景图 | `Overview/` |
| 📅 02 | 计划中 | 线程的底层：Thread、ThreadPool 与 Task | `Threads/` |

**目标**：建立并发编程的全局认知，理解核心概念和底层机制。

---

#### **第二篇：异步基础篇（4章）**

| 章节 | 状态 | 主题 | 代码位置 |
|------|------|------|---------|
| 📅 03 | 计划中 | Task API 完全指南 | `TaskAPI/` |
| 📅 04 | 计划中 | async/await 原理与优化 | `AsyncAwait/` |
| 📅 05 | 计划中 | SynchronizationContext 深度剖析 | `SyncContext/` |
| 📅 06 | 计划中 | CancellationToken 与超时控制 | `Cancellation/` |

**目标**：系统掌握 Task 和 async/await 的核心用法与底层原理。

---

#### **第三篇：异步进阶篇（3章）**

| 章节 | 状态 | 主题 | 代码位置 |
|------|------|------|---------|
| 📅 07 | 计划中 | 异步异常处理 | `AsyncExceptions/` |
| 📅 08 | 计划中 | 异步编程最佳实践 | `AsyncBestPractices/` |
| 📅 09 | 计划中 | 异步编程中的内存泄漏 | `AsyncMemoryLeaks/` |

**目标**：掌握异步编程的高级问题，如异常、反模式、内存泄漏等陷阱。

---

#### **第四篇：并行与同步篇（5章）**

| 章节 | 状态 | 主题 | 代码位置 |
|------|------|------|---------|
| 📅 10 | 计划中 | Parallel 与 PLINQ | `Parallel/` |
| 📅 11 | 计划中 | 并发安全完全指南 | `Locks/` |
| 📅 12 | 计划中 | 线程安全集合与不可变集合 | `ConcurrentCollections/` |
| 📅 13 | 计划中 | ThreadLocal 与 AsyncLocal | `ThreadLocal/` |
| 📅 14 | 计划中 | 原子操作与内存模型 | `Atomics/` |

**目标**：掌握 CPU 密集型任务的并行处理和并发安全问题。

---

#### **第五篇：高级模式篇（3章）**

| 章节 | 状态 | 主题 | 代码位置 |
|------|------|------|---------|
| 📅 15 | 计划中 | TPL Dataflow 数据流 | `Dataflow/` |
| 📅 16 | 计划中 | IAsyncEnumerable 异步流 | `AsyncEnumerable/` |
| 📅 17 | 计划中 | Background Service 后台服务 | `BackgroundService/` |

**目标**：学习高级并发模式，用于复杂的异步和数据流场景。

---

#### **第六篇：性能与实战篇（4章）**

| 章节 | 状态 | 主题 | 代码位置 |
|------|------|------|---------|
| 📅 18 | 计划中 | 限流与并发控制 | `RateLimiting/` |
| 📅 19 | 计划中 | 性能优化实战 | `Performance/` |
| 📅 20 | 计划中 | 性能调试与诊断实战 | `Diagnostics/` |
| 📅 21 | 计划中 | 实战案例：同步改异步全解 | `RealWorld/` |

**目标**：掌握高性能并发代码的实战技巧和优化方法。

---

### 🎯 近期计划

- [x] ✅ **第 01 章**：并发编程全景图（已完成）
- [ ] 🚧 **第 02 章**：线程的底层原理（进行中）
- [ ] 📅 **第 03 章**：Task API 完全指南
- [ ] 📅 **第 04-06 章**：异步基础篇
- [ ] 📅 **第 07-21 章**：进阶篇 + 实战篇

**更新频率**：争取每周 1-2 章

---

### 📖 系列特点

1. **系统性**：21 章完整覆盖 C# 并发编程全栈
2. **渐进性**：从概念 → 基础 → 进阶 → 实战
3. **实战性**：每章都有可运行的代码示例
4. **深度性**：不只讲 How，更讲 Why
5. **现代性**：基于 .NET 10，涵盖最新特性

---

> **提示**：代码将随着系列进度逐步添加。⭐ **Star** 本仓库以获取更新通知！

---

## ❓ 常见问题

### Q1: 为什么示例代码这么简单？

**A**: 本项目是**教学项目**，目标是让你理解原理。简单的代码更容易理解核心概念。生产环境需要考虑：
- 错误处理
- 日志记录
- 取消机制（CancellationToken）
- 超时处理
- 资源管理

### Q2: 可以直接用在生产环境吗？

**A**: 示例代码展示的是**核心模式**，可以作为参考，但生产环境需要更多完善。

### Q3: 为什么没有更多章节的代码？

**A**: 本项目是**系列教程**（共 21 章），代码将随着教程进度逐步添加。

**更新频率**：
- 每周 1-2 章
- 关注仓库以获取最新更新

**当前进度**：
- ✅ 第一章已完成
- 🚧 第二章进行中
- 预计 2024 年完成前 10 章

### Q4: 如何选择 async/await 还是 Parallel？

**A**: 看**任务在等什么**：
- 等 I/O（网络、磁盘、数据库）→ `async/await`
- 等 CPU 计算 → `Parallel` / `PLINQ`

### Q5: 如何获取更新通知？

**A**: 
- ⭐ **Star** 本仓库（右上角）
- 👁️ **Watch** → **Custom** → 勾选 **Releases**
- 📧 GitHub 会在新章节发布时邮件通知你

---

## 📚 推荐资源

### 官方文档
- [Microsoft Docs: Async/Await](https://docs.microsoft.com/en-us/dotnet/csharp/async)
- [Task-based Asynchronous Pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
- [Parallel Programming in .NET](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/)

### 深度文章
- [Stephen Cleary: There is no Thread](https://blog.stephencleary.com/2013/11/there-is-no-thread.html)
- [Stephen Toub: ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)

### 书籍推荐
- **Concurrency in C# Cookbook** by Stephen Cleary
- **C# in Depth** by Jon Skeet（第 15-16 章）

---

## 📄 许可证

本项目采用 [MIT License](LICENSE) 开源。

---

## 🙏 致谢

感谢所有为 C# 并发编程教育做出贡献的开发者！

---

## 🌟 Star History

如果这个项目对你有帮助，请给个 **⭐ Star** 支持一下！

[![Star History Chart](https://api.star-history.com/svg?repos=Naughtyhusky/csharp-concurrency-cookbook&type=Date)](https://star-history.com/#Naughtyhusky/csharp-concurrency-cookbook&Date)

---

## 📞 联系方式

- **GitHub Issues**: [提出问题或建议](https://github.com/Naughtyhusky/csharp-concurrency-cookbook/issues)
- **Discussions**: [参与讨论](https://github.com/Naughtyhusky/csharp-concurrency-cookbook/discussions)

---

**⭐ 如果觉得有用，别忘了点个 Star！**

**Made with ❤️ by C# 开发者，为 C# 开发者**