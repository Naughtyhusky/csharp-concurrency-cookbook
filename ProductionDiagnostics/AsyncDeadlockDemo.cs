using System.Collections.Concurrent;

namespace ProductionDiagnostics;

/// <summary>
/// 异步死锁演示：模拟 async/await 中的死锁场景
/// 常见于混合同步/异步代码时
/// </summary>
public static class AsyncDeadlockDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== 异步死锁演示 ===");
        Console.WriteLine("演示在 UI 或旧版 ASP.NET 中调用 .Result 导致的死锁\n");

        Console.WriteLine("请选择要演示的场景：");
        Console.WriteLine("1. 模拟 SynchronizationContext 死锁");
        Console.WriteLine("2. 模拟 Task.Wait() 死锁");
        Console.WriteLine("3. 正确的异步实现");
        Console.WriteLine("0. 返回\n");

        Console.Write("请输入选项: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                DemoSyncContextDeadlock();
                break;
            case "2":
                DemoTaskWaitDeadlock();
                break;
            case "3":
                await DemoCorrectAsyncAsync();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("无效的选项");
                break;
        }
    }

    private static void DemoSyncContextDeadlock()
    {
        Console.WriteLine("\n--- 场景1：SynchronizationContext 死锁 ---\n");
        Console.WriteLine("❌ 错误代码（在 WinForms/WPF 中会死锁）：");
        Console.WriteLine(@"
public void Button_Click(object sender, EventArgs e)
{
    var result = GetDataAsync().Result; // ❌ 死锁！
    label.Text = result;
}

private async Task<string> GetDataAsync()
{
    await Task.Delay(1000);
    return ""Data"";
}");

        Console.WriteLine("\n在控制台应用中无法完全模拟（Console 没有 SynchronizationContext）");
        Console.WriteLine("但可以演示类似的阻塞问题：\n");

        Console.WriteLine("尝试同步获取异步结果...");

        try
        {
            // 使用自定义的 SynchronizationContext 模拟
            var syncContext = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncContext);

            // 在 SynchronizationContext 上执行
            syncContext.Post(_ =>
            {
                Console.WriteLine("开始执行异步方法...");
                try
                {
                    // ❌ 这会导致死锁
                    var result = GetDataAsync().Result;
                    Console.WriteLine($"结果: {result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发生错误: {ex.Message}");
                }
            }, null);

            // 等待 5 秒
            Console.WriteLine("等待 5 秒...");
            Thread.Sleep(5000);

            Console.WriteLine("\n⏱️  超时！操作仍未完成（已死锁）");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }

        Console.WriteLine("\n❌ 死锁原因：");
        Console.WriteLine("1. .Result 阻塞当前线程（UI 线程）");
        Console.WriteLine("2. await 后的代码需要回到 UI 线程执行");
        Console.WriteLine("3. UI 线程被阻塞，无法执行 await 后的代码");
        Console.WriteLine("4. 形成死锁\n");

        Console.WriteLine("✅ 解决方案：");
        Console.WriteLine("1. 使用 async/await 到底（不要用 .Result 或 .Wait()）");
        Console.WriteLine("2. 在库代码中使用 ConfigureAwait(false)");
        Console.WriteLine("3. 将同步方法改为异步方法");
    }

    private static void DemoTaskWaitDeadlock()
    {
        Console.WriteLine("\n--- 场景2：Task.Wait() 死锁 ---\n");
        Console.WriteLine("❌ 错误代码：");
        Console.WriteLine(@"
public void ProcessData()
{
    var task = ProcessDataAsync();
    task.Wait(); // ❌ 可能死锁
}

private async Task ProcessDataAsync()
{
    await Task.Delay(1000);
    // 其他操作...
}");

        Console.WriteLine("\n演示 Task.Wait() 的阻塞行为：\n");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("同步等待异步任务...");

        var task = Task.Run(async () =>
        {
            Console.WriteLine("  任务开始执行");
            await Task.Delay(2000);
            Console.WriteLine("  任务执行完成");
            return "完成";
        });

        task.Wait(); // 阻塞当前线程
        sw.Stop();

        Console.WriteLine($"等待完成，耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"结果: {task.Result}");

        Console.WriteLine("\n⚠️  虽然没有死锁，但存在问题：");
        Console.WriteLine("- .Wait() 阻塞了当前线程");
        Console.WriteLine("- 在 ASP.NET Core 中会降低吞吐量");
        Console.WriteLine("- 在有 SynchronizationContext 的环境中可能死锁");
    }

    private static async Task DemoCorrectAsyncAsync()
    {
        Console.WriteLine("\n--- 场景3：正确的异步实现 ---\n");
        Console.WriteLine("✅ 正确代码：");
        Console.WriteLine(@"
public async Task Button_Click(object sender, EventArgs e)
{
    var result = await GetDataAsync(); // ✅ 使用 await
    label.Text = result;
}

private async Task<string> GetDataAsync()
{
    await Task.Delay(1000);
    return ""Data"";
}");

        Console.WriteLine("\n演示正确的异步调用：\n");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("异步等待任务...");

        var result = await GetDataAsync();
        sw.Stop();

        Console.WriteLine($"等待完成，耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"结果: {result}");

        Console.WriteLine("\n✅ 优点：");
        Console.WriteLine("- 不阻塞线程");
        Console.WriteLine("- 不会死锁");
        Console.WriteLine("- 性能更好");
    }

    private static async Task<string> GetDataAsync()
    {
        Console.WriteLine("  开始获取数据...");
        await Task.Delay(1000);
        Console.WriteLine("  数据获取完成");
        return "测试数据";
    }

    /// <summary>
    /// 简单的单线程 SynchronizationContext，用于模拟 UI 线程
    /// </summary>
    private class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback, object?)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            _queue.Add((d, state));
        }

        public void RunOnCurrentThread()
        {
            while (_queue.TryTake(out var item, Timeout.Infinite))
            {
                item.Item1(item.Item2);
            }
        }
    }
}
