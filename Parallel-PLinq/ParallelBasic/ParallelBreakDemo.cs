namespace Parallel_PLinq.ParallelBasic;

/// <summary>
/// Parallel 循环的中断与退出演示
/// ParallelLoopState.Break() vs Stop() vs 异常 vs CancellationToken
/// </summary>
public static class ParallelBreakDemo
{
    // ─── 1. Break：当前迭代跑完就停，但比它索引小的迭代保证都会执行 ─────────

    /// <summary>
    /// Break()：告诉运行时"找到了，不需要比当前索引更大的迭代了"
    /// 但比当前索引小的迭代还是会跑完，保证最低索引语义
    /// </summary>
    public static void BreakDemo()
    {
        Console.WriteLine("\n── Break() 演示 ──");

        long? lowestBreakIndex = null;

        Parallel.For(0, 20, (i, state) =>
        {
            Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId,2}] 执行 i={i}");

            if (i == 10)
            {
                Console.WriteLine($"  → i=10 调用 Break()");
                state.Break();
            }

            Thread.Sleep(10); // 模拟工作
        });

        // Break 之后，LowestBreakIteration 会记录第一次 Break 的索引
        Console.WriteLine($"  完成。索引 < 10 的迭代全部执行，>= 10 的可能未执行");
        Console.WriteLine($"  应用场景：在有序集合中搜索，找到后停止继续向后查找");
    }

    // ─── 2. Stop：立刻停，已在跑的跑完，新的不再开始 ─────────────────────────

    /// <summary>
    /// Stop()：更暴力，通知所有线程尽快停止，连"比我小的索引"也不保证了
    /// </summary>
    public static void StopDemo()
    {
        Console.WriteLine("\n── Stop() 演示 ──");

        var result = Parallel.For(0, 100, (i, state) =>
        {
            if (state.IsStopped)
            {
                return; // 检查到停止信号，提前返回
            }

            if (i == 15)
            {
                Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId,2}] i=15 调用 Stop()");
                state.Stop();
                return;
            }

            if (!state.IsStopped)
            {
                Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId,2}] 执行 i={i}");
            }
        });

        Console.WriteLine($"  IsCompleted = {result.IsCompleted}（false = 被中断）");
        Console.WriteLine($"  应用场景：并行扫描中发现了致命错误，立刻叫停所有工作");
    }

    // ─── 3. CancellationToken：外部取消 Parallel 循环 ─────────────────────────

    /// <summary>
    /// 使用 CancellationToken 从外部取消 Parallel 循环
    /// 取消时 Parallel 会抛出 OperationCanceledException
    /// </summary>
    public static void CancellationDemo()
    {
        Console.WriteLine("\n── CancellationToken 取消 Parallel ──");

        using var cts = new CancellationTokenSource();
        var options = new ParallelOptions
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = 4
        };

        // 500ms 后从外部取消
        Task.Run(async () =>
        {
            await Task.Delay(200);
            Console.WriteLine("  → 外部发出取消信号！");
            cts.Cancel();
        });

        try
        {
            Parallel.For(0, 1000, options, i =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(50);
                Console.WriteLine($"  [Thread {Thread.CurrentThread.ManagedThreadId,2}] 完成 i={i}");
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  ✅ 捕获到 OperationCanceledException，循环已取消");
        }
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  Parallel 循环中断与退出演示");
        Console.WriteLine("═══════════════════════════════════════");

        BreakDemo();
        StopDemo();
        CancellationDemo();
    }
}
