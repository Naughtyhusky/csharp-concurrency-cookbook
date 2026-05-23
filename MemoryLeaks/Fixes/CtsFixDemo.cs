namespace MemoryLeaks.Fixes;

/// <summary>
/// 修复方案 1：CancellationTokenSource 的正确生命周期管理
/// </summary>
public static class CtsFixDemo
{
    // ✅ 修复 1：短生命周期 CTS → using 语句，离开作用域自动 Dispose
    public static async Task FixShortLivedCts()
    {
        Console.WriteLine("\n  [修复] using 语句确保 CTS 及时释放...");

        for (int i = 0; i < 5; i++)
        {
            // ✅ using 确保 CTS 在每次循环结束时被释放
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await SimulateWorkAsync(cts.Token).ConfigureAwait(false);
        }

        Console.WriteLine("  [修复] 每轮循环结束，CTS 立即 Dispose，内部 Timer 停止，无堆积");
    }

    // ✅ 修复 2：长生命周期 CTS（比如服务级别）→ 绑定到服务生命周期，在 Dispose 时释放
    public sealed class ManagedWorkerService : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private Task? _backgroundTask;

        public void Start()
        {
            Console.WriteLine("  [修复] 服务启动，CTS 绑定到服务生命周期");
            _backgroundTask = RunLoopAsync(_cts.Token);
        }

        private static async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(200, token).ConfigureAwait(false);
            }
        }

        // ✅ Dispose 时取消并释放 CTS
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            Console.WriteLine("  [修复] 服务 Dispose，CTS 已取消并释放");
        }
    }

    // ✅ 修复 3：Register 回调时，持有 IDisposable 注册句柄，并在合适时机 Dispose
    public static async Task FixCallbackRegistration()
    {
        Console.WriteLine("\n  [修复] Register 回调时，持有句柄并在完成后 Dispose...");

        using var cts = new CancellationTokenSource();

        var bigData = new byte[1024 * 1024]; // 1MB

        // ✅ 持有注册句柄
        using var registration = cts.Token.Register(() =>
        {
            Console.WriteLine($"  取消回调触发，bigData 长度：{bigData.Length}");
        });

        await SimulateWorkAsync(cts.Token).ConfigureAwait(false);

        // ✅ using 离开作用域 → registration.Dispose() → 从回调链中移除
        // ✅ using 离开作用域 → cts.Dispose() → 内部资源全部释放
        Console.WriteLine("  [修复] registration 和 cts 均随 using 作用域自动释放");
    }

    private static async Task SimulateWorkAsync(CancellationToken token)
    {
        await Task.Delay(20, token).ConfigureAwait(false);
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== CancellationTokenSource 修复方案 ===");
        await FixShortLivedCts();
        await FixCallbackRegistration();

        Console.WriteLine("\n  [修复] 服务生命周期绑定演示:");
        using var service = new ManagedWorkerService();
        service.Start();
        await Task.Delay(100).ConfigureAwait(false);
        // using 块结束，service.Dispose() 自动调用
    }
}
