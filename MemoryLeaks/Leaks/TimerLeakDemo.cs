namespace MemoryLeaks.Leaks;

/// <summary>
/// 泄漏场景 4：Timer 和 PeriodicTimer 忘记释放
///
/// System.Threading.Timer  → 包装了操作系统定时器，不 Dispose 会持续触发回调
/// System.Timers.Timer     → 同上，且有事件订阅问题
/// PeriodicTimer (.NET 6+) → 更现代，但仍需 Dispose 来停止后台等待
///
/// 异步场景常见问题：
///   - 在异步方法中创建 Timer，方法返回后 Timer 继续运行
///   - Timer 回调闭包捕获了已"过期"的上下文对象
///   - PeriodicTimer + async 循环 在取消时没有正确停止
/// </summary>
public static class TimerLeakDemo
{
    // ❌ 反模式 1：方法中创建 Timer，方法返回但 Timer 继续跑
    public static Task LeakByOrphanedTimer()
    {
        Console.WriteLine("\n  [泄漏] 创建一个孤立的 Timer，方法返回后 Timer 继续跑...");

        var counter = 0;
        var bigCapture = new byte[512 * 1024]; // 512KB，被闭包捕获

        // ❌ timer 是局部变量，没有赋给任何字段/属性
        // 但 GC 不会立即回收它——.NET 的 Timer 内部被 TimerQueue 持有
        // 所以它会继续触发，直到被 GC（时机不确定）或显式 Dispose
        var timer = new System.Threading.Timer(_ =>
        {
            counter++;
            _ = bigCapture.Length; // 闭包持有 bigCapture，512KB 无法释放
            if (counter <= 3)
                Console.WriteLine($"  [泄漏] 孤立 Timer 触发第 {counter} 次，方法早已返回！");
        }, null, 0, 200);

        // 方法返回，timer 局部变量离开作用域
        // 但 Timer 不会停，GC 也不保证立即回收
        Console.WriteLine("  [泄漏] 方法已返回，但 Timer 还在后台运行...");

        // 演示后清理（现实中泄漏的代码不会有这行）
        Task.Delay(700).ContinueWith(_ => timer.Dispose());

        return Task.CompletedTask;
    }

    // ❌ 反模式 2：PeriodicTimer 在异步循环中没有正确取消
    public static async Task LeakByPeriodicTimerNotStopped(CancellationToken outerToken)
    {
        Console.WriteLine("\n  [泄漏] PeriodicTimer 没有绑定 CancellationToken，停不下来...");

        // ❌ PeriodicTimer 自己没有 CancellationToken 参数（.NET 6/7）
        // 如果 WaitForNextTickAsync 没传 token，外部取消无法终止循环
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(150));

        var count = 0;
        try
        {
            // ❌ 没有传 outerToken，即使外部取消了，这里也不会停止
            while (await timer.WaitForNextTickAsync(outerToken).ConfigureAwait(false))
            {
                count++;
                Console.WriteLine($"  [泄漏] PeriodicTimer tick #{count}");
                if (count >= 3) break; // 演示用，限制次数
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  [泄漏演示] 外部取消了，循环终止");
        }
    }

    // ❌ 反模式 3：System.Timers.Timer 的事件订阅泄漏
    public static Task LeakByTimersTimerEvent()
    {
        Console.WriteLine("\n  [泄漏] System.Timers.Timer 的 Elapsed 事件订阅没有取消...");

        var bigData = new byte[256 * 1024]; // 256KB

        var timer = new System.Timers.Timer(300);
        var count = 0;

        // ❌ Elapsed 是事件，闭包捕获 bigData
        // 如果 timer 不 Dispose，bigData 无法释放
        timer.Elapsed += (_, _) =>
        {
            count++;
            _ = bigData.Length;
            if (count <= 2)
                Console.WriteLine($"  [泄漏] System.Timers.Timer 触发 #{count}，bigData 仍被持有");
        };
        timer.Start();

        // 演示后清理
        Task.Delay(800).ContinueWith(_ =>
        {
            timer.Stop();
            timer.Dispose();
            Console.WriteLine("  [演示] Timer 已 Dispose，bigData 现在可以被 GC");
        });

        return Task.CompletedTask;
    }
}
