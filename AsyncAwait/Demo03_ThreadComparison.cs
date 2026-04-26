using System.Diagnostics;

namespace AsyncAwait;

/// <summary>
/// Demo03: 线程使用对比（async vs 同步阻塞）
/// </summary>
public static class Demo03_ThreadComparison
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo03: 线程使用对比 ===\n");

        // 3.1 I/O 异步：不占用线程
        Console.WriteLine("--- 3.1 I/O 异步操作：观察线程 ID ---");
        await IOAsyncExample();

        // 3.2 同步阻塞 vs 异步
        Console.WriteLine("\n--- 3.2 对比：同步阻塞 vs 异步 ---");
        await CompareBlockingVsAsync();

        // 3.3 CPU 密集型：需要 Task.Run
        Console.WriteLine("\n--- 3.3 CPU 密集型：需要 Task.Run ---");
        await CPUBoundExample();
    }

    static async Task IOAsyncExample()
    {
        Console.WriteLine($"✓ [Before await] 线程 ID: {Environment.CurrentManagedThreadId}");

        // 模拟 I/O 操作（如网络请求、文件读取）
        await Task.Delay(500);

        Console.WriteLine($"✓ [After await] 线程 ID: {Environment.CurrentManagedThreadId}");
        Console.WriteLine("  💡 线程 ID 可能不同，await 期间线程被释放");
    }

    static async Task CompareBlockingVsAsync()
    {
        int requestCount = 10;

        // 方案 1：同步阻塞
        Console.WriteLine("✓ 方案 1: 同步阻塞（.Result）");
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < requestCount; i++)
        {
            SimulateIOOperation().Wait(); // ⚠️ 阻塞线程
        }
        sw1.Stop();
        Console.WriteLine($"  耗时: {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"  问题: 每次都阻塞一个线程（浪费资源）");

        Console.WriteLine();

        // 方案 2：异步
        Console.WriteLine("✓ 方案 2: 异步（await）");
        var sw2 = Stopwatch.StartNew();
        var tasks = new List<Task>();
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(SimulateIOOperation()); // ⭐ 不阻塞
        }
        await Task.WhenAll(tasks);
        sw2.Stop();
        Console.WriteLine($"  耗时: {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"  优势: 并发执行，无线程等待");
    }

    static async Task SimulateIOOperation()
    {
        await Task.Delay(100); // 模拟 I/O 操作
    }

    static async Task CPUBoundExample()
    {
        // ❌ 错误：CPU 密集型不使用 Task.Run
        Console.WriteLine("❌ 错误做法：");
        var sw1 = Stopwatch.StartNew();
        int result1 = await CalculatePrimesWrong(10000);
        sw1.Stop();
        Console.WriteLine($"  没有 Task.Run: {result1} 个质数，耗时 {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"  问题: 仍然阻塞当前线程");

        Console.WriteLine();

        // ✅ 正确：使用 Task.Run
        Console.WriteLine("✅ 正确做法：");
        var sw2 = Stopwatch.StartNew();
        int result2 = await CalculatePrimesCorrect(10000);
        sw2.Stop();
        Console.WriteLine($"  使用 Task.Run: {result2} 个质数，耗时 {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"  优势: 在线程池线程上执行，不阻塞主线程");
    }

    // ❌ 错误：async 方法中直接执行 CPU 密集型操作
    static async Task<int> CalculatePrimesWrong(int max)
    {
        int count = 0;
        for (int i = 2; i <= max; i++)
        {
            if (IsPrime(i)) count++;
        }
        return count; // 编译器警告：CS1998
    }

    // ✅ 正确：使用 Task.Run
    static async Task<int> CalculatePrimesCorrect(int max)
    {
        return await Task.Run(() =>
        {
            int count = 0;
            for (int i = 2; i <= max; i++)
            {
                if (IsPrime(i)) count++;
            }
            return count;
        });
    }

    static bool IsPrime(int number)
    {
        if (number < 2) return false;
        for (int i = 2; i <= Math.Sqrt(number); i++)
        {
            if (number % i == 0) return false;
        }
        return true;
    }
}
