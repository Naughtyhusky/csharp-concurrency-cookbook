namespace BestPractices.AntiPatterns;

/// <summary>
/// 反模式演示：Fire-and-Forget 的错误姿势
/// 以及 Task.Run 的常见滥用
/// </summary>
public class FireAndForgetDemo
{
    // ❌ 反模式 1：最糟糕的 Fire-and-Forget —— 直接忽略 Task
    // 编译器会警告：CS4014 - 由于此调用不会等待，当前方法继续运行...
    public static void BadFireAndForget_IgnoreTask()
    {
        // 异常会被完全吞掉，你完全不知道后台任务有没有成功
        DoBackgroundWorkAsync(); // ⚠️ 编译器警告 CS4014
    }

    // ❌ 反模式 2：用 async void 做 Fire-and-Forget（前面讲过，异常会崩进程）
    public static async void BadFireAndForget_AsyncVoid()
    {
        await DoBackgroundWorkAsync();
    }

    // ❌ 反模式 3：Task.Run 包装 I/O 操作（浪费线程）
    // I/O 操作本身是异步的，不需要占用线程池线程来"等待"
    public static async Task BadTaskRunForIO()
    {
        // 完全没必要！这会从线程池借一个线程出来干等着 I/O 完成
        var result = await Task.Run(async () =>
        {
            return await ReadFileAsync(); // I/O 密集型操作
        });
        Console.WriteLine(result);
    }

    // ✅ 正确做法：直接 await，不需要 Task.Run
    public static async Task GoodDirectAwaitForIO()
    {
        var result = await ReadFileAsync(); // 直接 await，不浪费线程
        Console.WriteLine(result);
    }

    // ✅ Task.Run 的正确用法：CPU 密集型操作，防止阻塞 UI 线程
    public static async Task GoodTaskRunForCPU()
    {
        // 把 CPU 密集型工作扔到线程池，让 UI 线程保持响应
        var result = await Task.Run(() => HeavyCpuWork());
        Console.WriteLine($"CPU 计算结果: {result}");
    }

    // ✅ 正确的 Fire-and-Forget：显式处理异常
    public static void GoodFireAndForget()
    {
        _ = DoBackgroundWorkSafelyAsync(); // 使用 _ 丢弃，表示"我知道我在干什么"
    }

    // ✅ 更完善的 Fire-and-Forget：带异常处理
    public static void BetterFireAndForget()
    {
        var task = DoBackgroundWorkSafelyAsync();
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                // 记录日志，而不是让异常消失
                Console.WriteLine($"[后台任务异常] {t.Exception?.GetBaseException().Message}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    // ✅ 最推荐的 Fire-and-Forget：封装成扩展方法，错误处理逻辑由调用方决定
    public static void BestFireAndForget_SyncHandler()
    {
        // 传入同步委托：调用方自行决定出错后怎么办
        DoBackgroundWorkAsync().FireAndForget(ex =>
            Console.WriteLine($"[同步处理] 捕获到异常: {ex.Message}"));
    }

    public static void BestFireAndForget_AsyncHandler()
    {
        // 传入异步委托：出错后可以做异步操作，比如写数据库、发告警
        DoBackgroundWorkAsync().FireAndForget(async ex =>
        {
            await Task.Delay(10); // 模拟异步告警上报
            Console.WriteLine($"[异步处理] 异常已上报: {ex.Message}");
        });
    }

    private static async Task DoBackgroundWorkAsync()
    {
        await Task.Delay(500);
        // 模拟可能失败的后台操作
        throw new InvalidOperationException("后台任务失败了，但没人知道...");
    }

    private static async Task DoBackgroundWorkSafelyAsync()
    {
        try
        {
            await Task.Delay(500);
            Console.WriteLine("后台工作完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[后台任务异常处理] {ex.Message}");
        }
    }

    private static async Task<string> ReadFileAsync()
    {
        await Task.Delay(50); // 模拟文件读取
        return "文件内容";
    }

    private static long HeavyCpuWork()
    {
        // 模拟 CPU 密集型计算
        long sum = 0;
        for (int i = 0; i < 10_000_000; i++)
            sum += i;
        return sum;
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== Fire-and-Forget 与 Task.Run 反模式演示 ===");

        Console.WriteLine("\n❌ 直接忽略 Task（异常被吞掉）:");
        BadFireAndForget_IgnoreTask();
        await Task.Delay(200); // 给时间让后台任务"失败"，但你根本不知道

        Console.WriteLine("\n✅ 正确的 Fire-and-Forget（ContinueWith 捕获异常）:");
        BetterFireAndForget();
        await Task.Delay(700); // 等待后台任务完成并报告错误

        Console.WriteLine("\n✅ 最推荐：FireAndForget 扩展方法 + 同步错误处理委托:");
        BestFireAndForget_SyncHandler();
        await Task.Delay(700);

        Console.WriteLine("\n✅ 最推荐：FireAndForget 扩展方法 + 异步错误处理委托:");
        BestFireAndForget_AsyncHandler();
        await Task.Delay(700);

        Console.WriteLine("\n✅ Task.Run 的正确用法（CPU 密集型）:");
        await GoodTaskRunForCPU();

        Console.WriteLine("\n✅ I/O 操作直接 await（不需要 Task.Run）:");
        await GoodDirectAwaitForIO();
    }
}

// 简单的 ILogger 接口用于演示（保留，其他地方可能用到）
public interface ILogger
{
    void LogError(string message, Exception? ex = null);
}

// Task 扩展方法：优雅的 Fire-and-Forget
// 错误处理逻辑由调用方通过委托传入，扩展方法本身不关心你要怎么处理
public static class TaskExtensions
{
    /// <summary>
    /// Fire-and-Forget：出错时执行同步回调。
    /// </summary>
    /// <param name="task">要在后台执行的任务</param>
    /// <param name="onError">出错时的处理逻辑，传入原始异常；为 null 则静默忽略（不推荐）</param>
    public static void FireAndForget(this Task task, Action<Exception>? onError = null)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var ex = t.Exception!.GetBaseException();
                onError?.Invoke(ex);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Fire-and-Forget：出错时执行异步回调（比如写数据库、发告警接口等）。
    /// </summary>
    /// <param name="task">要在后台执行的任务</param>
    /// <param name="onError">出错时的异步处理逻辑，传入原始异常</param>
    public static void FireAndForget(this Task task, Func<Exception, Task> onError)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var ex = t.Exception!.GetBaseException();
                // 启动异步回调，同样 Fire-and-Forget 地运行
                // 如果回调本身也失败，这里会静默——调用方的回调应自行保证健壮性
                _ = onError(ex);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
