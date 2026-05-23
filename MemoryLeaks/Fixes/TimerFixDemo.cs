namespace MemoryLeaks.Fixes;

/// <summary>
/// 修复方案 4：Timer 和 PeriodicTimer 的正确使用
/// </summary>
public static class TimerFixDemo
{
    // ✅ 修复 1：Timer 必须赋给字段或 using，确保 Dispose
    public sealed class ManagedTimerService : IDisposable
    {
        // ✅ 赋给字段，生命周期明确
        private readonly System.Threading.Timer _timer;
        private int _tickCount;

        public ManagedTimerService()
        {
            _timer = new System.Threading.Timer(OnTick, null, 0, 200);
            Console.WriteLine("  [修复] ManagedTimerService 启动，Timer 已赋给字段");
        }

        private void OnTick(object? state)
        {
            var count = Interlocked.Increment(ref _tickCount);
            if (count <= 3)
                Console.WriteLine($"  [修复] Timer tick #{count}");
        }

        // ✅ Dispose 时停止并释放 Timer
        public void Dispose()
        {
            _timer.Dispose();
            Console.WriteLine("  [修复] Timer 已 Dispose，不再触发，资源释放");
        }
    }

    // ✅ 修复 2：PeriodicTimer + CancellationToken 正确配合
    public static async Task FixPeriodicTimerWithCancellation(CancellationToken ct)
    {
        Console.WriteLine("\n  [修复] PeriodicTimer 传入 CancellationToken，可正常停止...");

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(150));
        var count = 0;

        try
        {
            // ✅ 传入 ct，外部取消时 WaitForNextTickAsync 会抛 OperationCanceledException
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                count++;
                Console.WriteLine($"  [修复] PeriodicTimer tick #{count}");
                if (count >= 3) break;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  [修复] 收到取消信号，PeriodicTimer 循环正常退出");
        }

        Console.WriteLine($"  [修复] 共执行 {count} 次，using 块结束时 PeriodicTimer 自动 Dispose");
    }

    // ✅ 修复 3：System.Timers.Timer 用 using 管理
    public static async Task FixTimersTimer()
    {
        Console.WriteLine("\n  [修复] System.Timers.Timer 用 using 确保 Dispose...");

        var bigData = new byte[256 * 1024]; // 256KB
        var count = 0;

        using var timer = new System.Timers.Timer(300);

        // ✅ 持有 handler 引用，以便 Dispose 前取消订阅
        System.Timers.ElapsedEventHandler handler = (_, _) =>
        {
            count++;
            _ = bigData.Length;
            if (count <= 2)
                Console.WriteLine($"  [修复] System.Timers.Timer tick #{count}");
        };

        timer.Elapsed += handler;
        timer.Start();

        await Task.Delay(800).ConfigureAwait(false);

        // ✅ 先取消订阅，再 Dispose（using 会调用 Dispose）
        timer.Elapsed -= handler;
        timer.Stop();

        // using 块结束，timer.Dispose() 自动调用
        Console.WriteLine("  [修复] Timer 已停止并取消事件订阅，bigData 可以被 GC");
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== Timer 泄漏修复方案 ===");

        Console.WriteLine("\n  [修复 1] Timer 赋给字段，随服务 Dispose:");
        using (var svc = new ManagedTimerService())
        {
            await Task.Delay(500).ConfigureAwait(false);
        } // ← Dispose 自动调用

        Console.WriteLine("\n  [修复 2] PeriodicTimer + CancellationToken:");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await FixPeriodicTimerWithCancellation(cts.Token);

        await FixTimersTimer();
    }
}
