# CancellationToken 示例项目

> 第 06 章《CancellationToken 与超时控制》的配套示例代码

---

## 📁 项目结构

```
CancellationToken/
├── Program.cs                          # 主程序入口（交互式菜单）
├── Example01_BasicCancellation.cs      # 示例1：基础用法 - 手动取消
├── Example02_TimeoutCancellation.cs    # 示例2：超时自动取消
├── Example03_LinkedTokens.cs           # 示例3：链接多个 CancellationToken
├── Example04_FileDownloader.cs         # 示例4：文件下载器（用户取消 + 超时）
├── Example05_CpuIntensiveTask.cs       # 示例5：CPU 密集型任务取消
├── Example06_FileBatchProcessor.cs     # 示例6：文件批量处理器（完整案例）
├── Example07_CancellationCallback.cs   # 示例7：取消回调注册
├── Example08_BadExample.cs             # 示例8：错误示例对比
├── Example09_PerformanceBenchmark.cs   # 示例9：性能基准测试
├── Example10_TokenPropagation.cs       # 示例10：Token 的传递性 ⭐
└── Example11_AsyncPatterns.cs          # 示例11：异步编程模式 ⭐
```

---

## 🚀 如何运行

### 方式1：Visual Studio

1. 打开解决方案 `ConcurrencyCookbook.sln`
2. 右键点击 `CancellationToken` 项目 → **设为启动项目**
3. 按 `F5` 运行

### 方式2：命令行

```bash
cd CancellationToken
dotnet run
```

### 方式3：直接运行可执行文件

```bash
cd CancellationToken\bin\Debug\net10.0
CancellationToken.exe
```

---

## 📚 示例说明

### 示例1：基础用法 - 手动取消

演示 CancellationToken 的基本使用流程：

- 创建 `CancellationTokenSource`
- 传递 `Token` 给异步方法
- 调用 `Cancel()` 发送取消信号
- 使用 `ThrowIfCancellationRequested()` 检查取消

**关键代码**：
```csharp
using var cts = new CancellationTokenSource();
var task = LongRunningOperationAsync(cts.Token);
await Task.Delay(2000);
cts.Cancel(); // 发送取消信号
```

---

### 示例2：超时自动取消

演示两种设置超时的方式：

- 创建时设置：`new CancellationTokenSource(TimeSpan.FromSeconds(3))`
- 延迟设置：`cts.CancelAfter(TimeSpan.FromSeconds(3))`

**应用场景**：
- HTTP 请求超时
- 数据库查询超时
- 任何需要自动超时的操作

---

### 示例3：链接多个 CancellationToken

演示如何组合多个取消条件：

- 用户手动取消
- 超时自动取消
- 使用 `CreateLinkedTokenSource` 组合

**关键代码**：
```csharp
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    userToken,
    timeoutToken);
```

**应用场景**：
- 用户取消 + 超时控制
- 多级取消传播
- 复杂的取消逻辑

---

### 示例4：文件下载器

模拟真实的文件下载场景：

- 显示下载进度
- 支持用户取消
- 自动超时控制
- 区分取消原因（用户 vs 超时）

**特点**：
- 使用 `when` 子句区分异常
- 显示进度百分比
- 模拟网络延迟

---

### 示例5：CPU 密集型任务取消

演示如何取消 CPU 密集型计算：

- 质数计算（长时间运行）
- 定期检查取消信号（每 10000 次）
- 显示实时进度

**性能权衡**：
- ❌ 每次循环都检查：性能开销大
- ✅ 每 10000 次检查：平衡响应性和性能

**关键代码**：
```csharp
for (int i = 2; i < max; i++)
{
    if (i % 10000 == 0) // 适度检查
    {
        cancellationToken.ThrowIfCancellationRequested();
    }
    // 计算逻辑...
}
```

---

### 示例6：文件批量处理器 ⭐

**最完整的实战案例**，涵盖所有知识点：

- 批量处理多个文件
- 整体超时（5 分钟）
- 单个文件超时（2 秒）
- 用户随时取消
- 实时进度报告
- 资源自动清理

**技术亮点**：
1. **多级取消控制**：整体 + 单个文件
2. **异常分类处理**：超时 vs 用户取消 vs 错误
3. **IProgress 进度报告**：实时反馈
4. **资源管理**：临时文件创建和清理

**输出示例**：
```
[14:23:15] 处理中: file1.txt
[14:23:16] ✅ 完成: file1.txt (1/5)
[14:23:16] 处理中: file2.txt
[14:23:17] ✅ 完成: file2.txt (2/5)
[14:23:17] 处理中: file3.txt
[14:23:19] ⏱️ 超时: file3.txt (2/5)
[14:23:19] 处理中: file4.txt
[14:23:20] ❌ 用户取消 (2/5)
```

---

### 示例7：取消回调注册

演示如何在取消时执行清理逻辑：

- 注册取消回调：`cancellationToken.Register(callback)`
- 自动删除临时文件
- 资源清理保证

**应用场景**：
- 删除临时文件
- 关闭网络连接
- 释放非托管资源

**关键代码**：
```csharp
using var registration = cancellationToken.Register(() =>
{
    // 取消时执行
    if (File.Exists(tempFile))
    {
        File.Delete(tempFile);
    }
});
```

---

### 示例8：错误示例对比

**教学示例**：对比错误和正确的实现

#### ❌ 错误：不传递 Token

```csharp
await Task.Delay(1000); // 缺少 cancellationToken
```

**问题**：无法取消，任务会一直运行完

#### ✅ 正确：传递 Token

```csharp
await Task.Delay(1000, cancellationToken); // 可以取消
```

**效果**：立即响应取消请求

---

### 示例9：性能基准测试 ⚡

**博客章节 7.9 的配套代码**

演示不同检查频率对性能的影响，提供两种测试模式：

#### 模式1：简单测试（快速）

- 使用 `Stopwatch` 计时
- 约 30 秒完成
- 适合快速验证

**运行方式**：在菜单中选择 `9`

**测试内容**：
1. 不检查（基准）
2. 每次检查
3. 每 100 次检查
4. 每 1000 次检查
5. 每 10000 次检查

**输出示例**：
```
=== CancellationToken 简单性能测试 ===

1. 不检查:           12.34 ms  (基准)
2. 每次检查:         30.56 ms  (慢 2.48x)
3. 每 100 次检查:    13.21 ms  (慢 1.07x)
4. 每 1000 次检查:   12.67 ms  (慢 1.03x)
5. 每 10000 次检查:  12.45 ms  (慢 1.01x)

结论：
- 每次检查比基准慢 2.48x
- 每 100 次检查比基准慢 1.07x
- 每 1000 次检查比基准慢 1.03x
- 每 10000 次检查比基准慢 1.01x

推荐：
- 循环次数 < 1,000：每次检查（响应时间 < 1ms）
- 循环次数 1,000 - 10,000：每 100 次检查（响应时间 ~10ms）
- 循环次数 10,000 - 100,000：每 1,000 次检查（响应时间 ~100ms）
- 循环次数 > 100,000：每 10,000 次检查（响应时间 ~1s）
```

#### 模式2：BenchmarkDotNet（精确）

- 使用 BenchmarkDotNet 框架
- 包含预热、多次迭代
- 约 5-10 分钟完成
- 提供详细的性能报告

**运行方式**：
1. **切换到 Release 模式**（重要！）
2. 在菜单中选择 `B`
3. 等待测试完成

**⚠️ 重要提示**：
- 必须在 **Release 模式**下运行
- Debug 模式结果不准确
- 关闭其他占用 CPU 的程序
- 不要在虚拟机中运行

**测试方法**：
- `NoCheck`：不检查（基准）
- `CheckEveryIteration`：每次检查
- `CheckEvery100`：每 100 次检查
- `CheckEvery1000`：每 1000 次检查
- `CheckEvery10000`：每 10000 次检查
- `CheckEvery100000`：每 100000 次检查

**输出位置**：
结果保存在 `BenchmarkDotNet.Artifacts/results/` 目录

**报告内容**：
- 平均执行时间
- 标准差
- 内存分配情况
- 相对性能比较

**性能结论**（参考值）：
```
| Method              | Mean      | Ratio | Allocated |
|-------------------- |----------:|------:|----------:|
| NoCheck             |  10.00 ms |  1.00 |       0 B |
| CheckEvery100000    |  10.05 ms |  1.01 |       0 B |
| CheckEvery10000     |  10.10 ms |  1.01 |       0 B |
| CheckEvery1000      |  10.50 ms |  1.05 |       0 B |
| CheckEvery100       |  11.20 ms |  1.12 |       0 B |
| CheckEveryIteration |  25.30 ms |  2.53 |       0 B |
```

**推荐频率**：

| 循环次数 | 推荐频率 | 响应时间 | 性能影响 | 适用场景 |
|----------|----------|----------|----------|----------|
| < 1,000 | 每次 | < 1 ms | 可忽略 | 小数据量 |
| 1,000 - 10,000 | 每 100 次 | ~10 ms | < 5% | 用户交互 |
| 10,000 - 100,000 | 每 1,000 次 | ~100 ms | < 5% | 文件处理 |
| 100,000 - 1,000,000 | 每 10,000 次 | ~1 s | < 5% | 批量处理 |
| > 1,000,000 | 每 100,000 次 | ~10 s | < 5% | 大数据分析 |

**使用建议**：
1. 开发阶段：使用简单测试快速验证
2. 性能调优：使用 BenchmarkDotNet 获取精确数据
3. 生产环境：根据实际业务场景选择合适的检查频率

---

### 示例10：Token 的传递性 ⭐⭐⭐

**必读示例**：理解为什么必须一路传递 CancellationToken

演示三个关键场景：

#### 场景1：错误示例 - 不传递的后果

```csharp
// ❌ 错误：不传递 Token
public async Task ProcessOrderAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();  // ✅ 第一层检查

    await ChargePaymentAsync();  // ❌ 没传递，无法取消！
    await SendEmailAsync();      // ❌ 没传递，无法取消！
}
```

**灾难性后果**：
- 用户点击取消
- 界面显示"已取消"
- 但后台：钱被扣了，邮件发了！

#### 场景2：正确示例 - 一路传递

```csharp
// ✅ 正确：Token 一路传递
public async Task ProcessOrderAsync(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    await ChargePaymentAsync(cancellationToken);  // ✅ 传递
    await SendEmailAsync(cancellationToken);      // ✅ 传递
}
```

**传递链路可视化**：
```
用户界面
  └─> OrderService
        ├─> PaymentService
        │     └─> HttpClient.PostAsync(token)  ← 真正的网络请求
        └─> EmailService
              └─> SmtpClient.SendMailAsync(token)  ← 真正的邮件发送
```

#### 场景3：5层调用的完整传递链

演示从 Web API → 业务层 → 数据层 → 数据库 → I/O 操作的完整传递链。

**关键教训**：
- 任何一个环节不传递，后续所有操作都无法取消
- 使用 Roslyn 分析器自动检测缺失的传递
- 这是 CancellationToken 使用中最常见的错误

---

### 示例11：异步编程模式最佳实践 ⭐⭐⭐

**必读示例**：.NET Core 时代的异步编程标准模式

演示四个核心模式：

#### 模式1：后台服务（BackgroundService）

```csharp
// 模拟 BackgroundService.ExecuteAsync
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await ProcessOrdersAsync(stoppingToken);  // 传递 stoppingToken
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
}
```

**优雅停机流程**：
1. 用户按 Ctrl+C
2. IHostApplicationLifetime.ApplicationStopping 触发
3. stoppingToken 被取消
4. 当前批次完成后退出
5. 应用优雅关闭

#### 模式2：并发任务共享 Token

```csharp
// 3个下载任务共享同一个 Token
var tasks = urls.Select(url => DownloadAsync(url, cancellationToken)).ToList();
await Task.WhenAll(tasks);  // 一个取消，全部取消
```

#### 模式3：组合多个取消源

```csharp
// 组合：用户取消 + 超时 + 应用关闭
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    userToken,    // 用户点击取消
    timeoutToken, // 30秒超时
    appToken);    // 应用关闭

// 使用 when 子句区分取消原因
catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
{
    throw new TimeoutException("操作超时");
}
```

#### 模式4：常见错误模式

展示3个常见错误：
1. ❌ 吞掉 OperationCanceledException
2. ❌ 忘记传递 Token 给 Task.Delay
3. ❌ 清理代码中忽略 Token

**应用场景**：
- ASP.NET Core 控制器（自动注入 CancellationToken）
- 后台服务（BackgroundService）
- 定时任务（Quartz.NET、Hangfire）
- 消息队列消费者

---

## 🎯 学习路径

### 初学者

1. **示例1**：掌握基础用法
2. **示例2**：理解超时控制
3. **示例8**：避免常见错误

### 进阶

4. **示例3**：链接多个 Token
5. **示例4**：实际文件下载场景
6. **示例7**：取消回调机制

### 高级

7. **示例5**：CPU 密集型任务的取消策略
8. **示例6**：生产级批量处理器

---

## 💡 最佳实践总结

### 1. 参数规范

```csharp
// ✅ CancellationToken 作为最后一个参数，默认值 = default
public async Task DoWorkAsync(
    string param1,
    int param2,
    CancellationToken cancellationToken = default)
{
    // ...
}
```

### 2. 一路传递

```csharp
// ✅ 传递给所有支持的 API
using var client = new HttpClient();
var response = await client.GetAsync(url, cancellationToken);
var content = await response.Content.ReadAsStringAsync(cancellationToken);
```

### 3. 资源管理

```csharp
// ✅ 使用 using 自动释放
using var cts = new CancellationTokenSource();
await DoWorkAsync(cts.Token);
```

### 4. 异常处理

```csharp
// ✅ 区分取消异常和其他异常
try
{
    await DoWorkAsync(cancellationToken);
}
catch (OperationCanceledException)
{
    // 取消处理
}
catch (Exception ex)
{
    // 其他错误处理
}
```

### 5. CPU 密集型任务

```csharp
// ✅ 适度检查，避免频繁检查影响性能
for (int i = 0; i < max; i++)
{
    if (i % 10000 == 0)
    {
        cancellationToken.ThrowIfCancellationRequested();
    }
    // 计算逻辑...
}
```

---

## ⚠️ 常见陷阱

### 1. ❌ 不传递 Token

```csharp
// ❌ 接收了但不传递
public async Task DownloadAsync(string url, CancellationToken cancellationToken)
{
    await client.GetAsync(url); // 缺少 cancellationToken
}
```

### 2. ❌ 忘记 Dispose

```csharp
// ❌ 不释放，可能导致内存泄漏
var cts = new CancellationTokenSource();
await DoWorkAsync(cts.Token);
// 忘记 Dispose
```

### 3. ❌ 频繁检查

```csharp
// ❌ 每次循环都检查，性能差
for (int i = 0; i < 10_000_000; i++)
{
    cancellationToken.ThrowIfCancellationRequested(); // 太频繁！
}
```

### 4. ❌ 吞掉取消异常

```csharp
// ❌ 吞掉所有异常，包括取消异常
try
{
    await DoWorkAsync(cancellationToken);
}
catch (Exception ex) // 不区分异常类型
{
    Console.WriteLine($"错误: {ex.Message}");
}
```

---

## 📖 相关文档

- [博客文章](../Blogs/06-CancellationToken与超时控制.md)
- [快速参考卡片](../Blogs/Summaries/06/06-快速参考卡片.md)
- [高级话题与注意事项](../Blogs/Summaries/06/06-高级话题与注意事项.md)
- [官方文档](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)

---

## 🎓 知识点检查清单

完成所有示例后，检查你是否掌握：

- [ ] 理解协作式取消模型
- [ ] 能够创建和使用 CancellationTokenSource
- [ ] 知道如何检查取消信号（两种方式）
- [ ] 能够实现超时控制
- [ ] 理解如何链接多个 Token
- [ ] 能够在 CPU 密集型任务中正确使用
- [ ] 知道如何注册取消回调
- [ ] 能够区分不同类型的取消异常
- [ ] 避免常见的使用陷阱
- [ ] 理解何时需要 Dispose

---

**下一步**：阅读第 07 章《异步异常处理》，深入理解 `OperationCanceledException` 的传播机制。
