# ProductionDiagnostics 项目说明

本项目是《C# 并发编程实战》系列博客第 20 章《生产环境诊断实战》的配套示例代码。

## 项目目标

演示生产环境中常见的并发问题，并提供诊断方法的实践指南：

1. **死锁（Deadlock）** - 两个线程相互等待对方释放锁
2. **线程池饥饿（ThreadPool Starvation）** - 线程池线程被阻塞，导致任务排队
3. **内存泄漏（Memory Leak）** - 对象无法被 GC 回收，内存持续增长
4. **CPU 占用过高（High CPU Usage）** - 死循环、低效算法等导致 CPU 100%
5. **异步死锁（Async Deadlock）** - 混合同步/异步代码导致的死锁
6. **对象池内存泄漏（Object Pool Memory Leak）** - 对象池使用不当导致的泄漏

## 如何运行

```bash
cd ProductionDiagnostics
dotnet run
```

然后根据提示选择要演示的场景（输入 0-6）。

## 诊断工具安装

### dotnet-dump（死锁诊断）

```bash
dotnet tool install --global dotnet-dump
```

### dotnet-counters（线程池监控）

```bash
dotnet tool install --global dotnet-counters
```

### dotnet-gcdump（内存泄漏诊断）

```bash
dotnet tool install --global dotnet-gcdump
```

### dotnet-trace（CPU 占用诊断）

```bash
dotnet tool install --global dotnet-trace
```

## 使用示例

### 1. 诊断死锁

```bash
# 运行程序，选择选项 1（死锁演示）
dotnet run

# 程序卡住后，在另一个终端抓取 dump
dotnet-dump collect -p <进程ID>

# 分析 dump 文件
dotnet-dump analyze <dump文件>

# 在分析器中执行：
> clrthreads
> syncblk
> parallelstacks
```

### 2. 监控线程池饥饿

```bash
# 运行程序，选择选项 2（线程池饥饿演示）
dotnet run

# 在另一个终端监控线程池
dotnet-counters monitor -p <进程ID> --counters System.Runtime[threadpool-thread-count,threadpool-queue-length]
```

### 3. 诊断内存泄漏

```bash
# 运行程序，选择选项 3（内存泄漏演示）
dotnet run

# 抓取第一个快照
dotnet-gcdump collect -p <进程ID> -o snapshot1.gcdump

# 等待一段时间后抓取第二个快照
dotnet-gcdump collect -p <进程ID> -o snapshot2.gcdump

# 使用 Visual Studio 对比两个快照
```

### 4. 诊断 CPU 占用

```bash
# 运行程序，选择选项 4（CPU 占用过高演示）
dotnet run

# 收集 60 秒的性能跟踪
dotnet-trace collect -p <进程ID> --duration 00:01:00

# 使用 PerfView 打开 .nettrace 文件，查看 CPU Stacks
```

## 代码文件说明

| 文件 | 说明 |
|------|------|
| `Program.cs` | 主程序，提供交互式菜单 |
| `DeadlockDemo.cs` | 死锁演示，展示经典的相互等待锁场景 |
| `ThreadPoolStarvationDemo.cs` | 线程池饥饿演示，对比同步阻塞和异步实现 |
| `MemoryLeakDemo.cs` | 内存泄漏演示，包含 4 种常见泄漏场景 |
| `HighCpuDemo.cs` | CPU 占用过高演示，包含死循环、低效算法等 |
| `AsyncDeadlockDemo.cs` | 异步死锁演示，模拟 SynchronizationContext 死锁 |
| `ObjectPoolLeakDemo.cs` | 对象池内存泄漏演示，对比有界和无界对象池 |

## 注意事项

⚠️ **本项目的代码都是故意写错的，用于演示问题场景**

- 不要在生产环境运行这些代码
- 死锁演示会导致程序卡死，需要手动终止
- 内存泄漏演示会占用大量内存
- CPU 占用演示会占用 100% CPU

## 学习路径

建议按以下顺序学习：

1. 阅读博客《20. 生产环境诊断实战》
2. 运行每个演示场景，观察现象
3. 使用诊断工具分析问题
4. 思考如何修复代码
5. 对比正确实现和错误实现的差异

## 相关章节

- 第 2 章《并发的底层：Thread、ThreadPool 与 Task》 - 理解线程池原理
- 第 9 章《异步编程中的内存泄漏》 - 详细讲解内存泄漏场景
- 第 11 章《锁机制完全指南》 - 理解死锁原理

## 扩展阅读

- [dotnet-dump 官方文档](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)
- [dotnet-counters 官方文档](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [dotnet-gcdump 官方文档](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-gcdump)
- [dotnet-trace 官方文档](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
- [PerfView 使用指南](https://github.com/microsoft/perfview)

## 常见问题

### Q: 为什么死锁演示在控制台应用中不会真的死锁？

A: 控制台应用没有 `SynchronizationContext`，所以不会出现典型的 UI 死锁。但是两个线程仍然会相互等待对方释放锁，可以用 `dotnet-dump` 诊断。

### Q: 如何找到进程 ID？

```bash
# Windows
tasklist | findstr ProductionDiagnostics

# Linux/macOS
ps aux | grep ProductionDiagnostics
```

### Q: 如何在 Visual Studio 中调试这些问题？

1. 打开项目
2. 设置断点
3. 启动调试（F5）
4. 使用以下窗口：
   - **调试 → 窗口 → 并行堆栈**（查看线程状态）
   - **调试 → 窗口 → 任务**（查看 Task 状态）
   - **调试 → 性能分析器**（分析 CPU/内存）

## 贡献

如果你发现了更好的诊断方法或示例，欢迎提交 PR！

---

**祝学习愉快！** 🚀
