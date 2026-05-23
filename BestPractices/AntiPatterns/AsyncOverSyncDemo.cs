namespace BestPractices.AntiPatterns;

/// <summary>
/// 反模式演示：异步转同步（Async over Sync）
/// 这是最容易导致死锁的操作，生产代码中血泪教训
/// </summary>
public class AsyncOverSyncDemo
{
    // ❌ 反模式：在同步方法中用 .Result 或 .Wait() 阻塞异步方法
    // 在有 SynchronizationContext 的环境（WinForms/WPF/ASP.NET Classic）必然死锁
    public static string BadGetData_BlockingResult()
    {
        // 如果 FetchDataAsync() 内部用了 await（没有 ConfigureAwait(false)），
        // 这里会死锁：.Result 阻塞了 UI 线程，await 却在等 UI 线程来 continue
        return FetchDataAsync().Result; // 💀 死锁风险
    }

    public static void BadGetData_BlockingWait()
    {
        FetchDataAsync().Wait(); // 💀 同样的死锁风险
    }

    // ❌ 更隐蔽的反模式：GetAwaiter().GetResult() 看起来专业，实际上一样
    public static string BadGetData_GetAwaiterGetResult()
    {
        return FetchDataAsync().GetAwaiter().GetResult(); // 💀 和 .Result 本质相同，只是不包装 AggregateException
    }

    // ✅ 正确做法：一路 async 到底（async all the way）
    public static async Task<string> GoodGetDataAsync()
    {
        return await FetchDataAsync();
    }

    private static async Task<string> FetchDataAsync()
    {
        await Task.Delay(100); // 模拟 I/O 操作
        return "数据来了！";
    }

    /// <summary>
    /// 演示死锁场景（不会真的死锁，因为控制台程序没有 SynchronizationContext）
    /// </summary>
    public static async Task DemoDeadlockRisk()
    {
        Console.WriteLine("\n=== 异步转同步（Async over Sync）反模式演示 ===");

        Console.WriteLine("❌ .Result / .Wait() 在 UI/ASP.NET 环境必然死锁，原因：");
        Console.WriteLine("   1. UI 线程调用 .Result，线程被阻塞");
        Console.WriteLine("   2. await 完成后需要回到 UI 线程继续执行");
        Console.WriteLine("   3. UI 线程被 .Result 占用，永远无法回来");
        Console.WriteLine("   4. 死锁！");

        Console.WriteLine("\n✅ 控制台程序没有 SynchronizationContext，所以这里不会死锁");
        Console.WriteLine("   但在 WinForms/WPF/ASP.NET Framework 中，这是必死的写法！");

        // 在控制台环境中演示（不会死锁）
        var result = BadGetData_BlockingResult();
        Console.WriteLine($"   控制台环境获取到：{result}");

        // 正确做法
        var goodResult = await GoodGetDataAsync();
        Console.WriteLine($"   ✅ 正确做法获取到：{goodResult}");
    }
}
