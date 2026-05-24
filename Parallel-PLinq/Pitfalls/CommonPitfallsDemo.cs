using System.Collections.Concurrent;

namespace Parallel_PLinq.Pitfalls;

/// <summary>
/// 常见陷阱演示：并行编程中最容易踩坑的地方
/// 线程安全问题、副作用陷阱、PLINQ 默认并行度过高
/// </summary>
public static class CommonPitfallsDemo
{
    // ─── 陷阱 1：在 PLINQ 中使用非线程安全集合 ───────────────────────────────

    /// <summary>
    /// 很多人看到 PLINQ 加速就想到处用，然后往 List<T> 里并行写……
    /// </summary>
    public static void NonThreadSafeCollectionPitfall()
    {
        Console.WriteLine("\n── ❌ 陷阱 1：向 List<T> 并行写入 ──");

        var data = Enumerable.Range(1, 10_000).ToArray();

        // ❌ 错误：List<T> 不是线程安全的，并行写会导致数据丢失甚至抛异常
        var unsafeList = new List<int>();
        try
        {
            data.AsParallel().ForAll(x =>
            {
                unsafeList.Add(x); // 多线程写 List，内部数组可能重复扩容
            });
            Console.WriteLine($"  ❌ 错误结果：期望 {data.Length}，实际 {unsafeList.Count}（有时还会抛异常）");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 抛出异常：{ex.GetType().Name} - {ex.Message}");
        }

        // ✅ 正确：用 ConcurrentBag 或直接用 ToList() 让 PLINQ 内部处理合并
        var safeResult = data.AsParallel()
            .Where(x => x % 2 == 0)
            .ToList(); // ✅ ToList() 由 PLINQ 内部安全合并

        Console.WriteLine($"  ✅ 正确结果（ToList 合并）：{safeResult.Count} 个偶数");

        // 也可以用 ConcurrentBag
        var safeBag = new ConcurrentBag<int>();
        data.AsParallel().Where(x => x % 3 == 0).ForAll(safeBag.Add);
        Console.WriteLine($"  ✅ 正确结果（ConcurrentBag）：{safeBag.Count} 个3的倍数");
    }

    // ─── 陷阱 2：在 Parallel 中做 IO 操作 ────────────────────────────────────

    /// <summary>
    /// Parallel 是为 CPU 密集型设计的，不要把 IO 操作塞进去！
    /// IO 操作应该用 async/await + Task.WhenAll（见第02-04章）
    /// </summary>
    public static void IoBoundInParallelPitfall()
    {
        Console.WriteLine("\n── ❌ 陷阱 2：在 Parallel 中做 IO 操作 ──");

        var urls = Enumerable.Range(1, 8).Select(i => $"http://api.example.com/item/{i}").ToList();

        // ❌ 错误：用 Parallel 处理 IO 密集任务
        // 这会消耗线程池线程等待 IO，而不是让线程去做别的工作
        Console.WriteLine("  ❌ 错误做法（概念演示，不实际执行）：");
        Console.WriteLine("     Parallel.ForEach(urls, url => HttpClient.GetString(url));");
        Console.WriteLine("     问题：等待 IO 时线程阻塞，其他任务无法复用这些线程");

        // ✅ 正确：用 async/await + Task.WhenAll
        Console.WriteLine("  ✅ 正确做法（概念演示）：");
        Console.WriteLine("     await Task.WhenAll(urls.Select(url => httpClient.GetStringAsync(url)));");
        Console.WriteLine("     原因：await 释放线程，IO 完成后才占用线程，效率远高于 Parallel");
        Console.WriteLine("  💡 参考第02-04章的 async/await 和 Task.WhenAll 用法");
    }

    // ─── 陷阱 3：PLINQ 默认并行度可能过高，吃光 CPU ─────────────────────────

    /// <summary>
    /// PLINQ 默认并发度 = CPU 核心数，在 Web 服务器上会把所有核心全占了
    /// 其他请求就得饿着，反而降低整体吞吐量
    /// </summary>
    public static void DefaultDegreeOfParallelismPitfall()
    {
        Console.WriteLine("\n── ⚠️  陷阱 3：PLINQ 默认并行度过高 ──");

        Console.WriteLine($"  当前 CPU 核心数: {Environment.ProcessorCount}");
        Console.WriteLine($"  PLINQ 默认并行度: {Environment.ProcessorCount}（会占满所有核心）");
        Console.WriteLine();
        Console.WriteLine("  ❌ 在 ASP.NET 请求处理中直接用 PLINQ（危险）：");
        Console.WriteLine("     var result = data.AsParallel().Where(...).ToList();");
        Console.WriteLine("     风险：当有多个并发请求时，每个请求都想占满 CPU，互相争抢");
        Console.WriteLine();
        Console.WriteLine("  ✅ 限制并行度，给其他请求留资源：");
        Console.WriteLine("     var result = data.AsParallel()");
        Console.WriteLine("                      .WithDegreeOfParallelism(Math.Max(1, Environment.ProcessorCount / 2))");
        Console.WriteLine("                      .Where(...).ToList();");

        // 演示限制并行度
        int safeDegree = Math.Max(1, Environment.ProcessorCount / 2);
        var data = Enumerable.Range(1, 500_000).ToArray();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var count = data.AsParallel()
            .WithDegreeOfParallelism(safeDegree)
            .Where(x => x % 7 == 0)
            .Count();
        sw.Stop();

        Console.WriteLine($"\n  使用并行度 {safeDegree}（总核心数的一半）：{count:N0} 个结果，{sw.ElapsedMilliseconds} ms");
        Console.WriteLine("  对于 Web API：CPU 密集型计算通常建议用 Task.Run 卸载，而非 PLINQ");
    }

    // ─── 陷阱 4：有副作用的 PLINQ 查询 ───────────────────────────────────────

    /// <summary>
    /// PLINQ 查询应该是纯函数（无副作用），否则结果不可预测
    /// </summary>
    public static void SideEffectPitfall()
    {
        Console.WriteLine("\n── ❌ 陷阱 4：PLINQ 查询含有副作用 ──");

        int counter = 0;

        // ❌ 错误：在 Select 中修改共享状态
        var wrong = Enumerable.Range(1, 1000)
            .AsParallel()
            .Select(x =>
            {
                counter++; // 多线程写 counter，数据竞争！
                return x * 2;
            })
            .ToList();

        Console.WriteLine($"  ❌ 期望计数 1000，实际 counter = {counter}（每次运行结果不同）");

        // ✅ 正确：查询本身无副作用，副作用用 ForAll 或在结果上操作
        int safeCounter = 0;
        Enumerable.Range(1, 1000)
            .AsParallel()
            .Select(x => x * 2)
            .ForAll(_ => Interlocked.Increment(ref safeCounter)); // ✅ 原子操作

        Console.WriteLine($"  ✅ 正确计数（Interlocked.Increment）= {safeCounter}");
        Console.WriteLine("  原则：PLINQ 的 Where/Select 保持纯函数，副作用只在 ForAll 中做，且要线程安全");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  常见陷阱演示");
        Console.WriteLine("═══════════════════════════════════════");

        NonThreadSafeCollectionPitfall();
        IoBoundInParallelPitfall();
        DefaultDegreeOfParallelismPitfall();
        SideEffectPitfall();
    }
}
