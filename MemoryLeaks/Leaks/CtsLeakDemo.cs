namespace MemoryLeaks.Leaks;

/// <summary>
/// 泄漏场景 1：CancellationTokenSource 忘记 Dispose
///
/// CancellationTokenSource 内部持有：
///   - 一个 WaitHandle（操作系统内核对象）
///   - 一组回调链表（注册的 CancellationToken.Register 回调）
///   - 定时器（如果用了 CancelAfter / new CTS(timeout)）
///
/// 不 Dispose 的后果：
///   - 注册的回调无法被 GC 收集（回调持有外部对象引用）
///   - 带超时的 CTS 内部 Timer 一直运行，持续占用资源
///   - 大量 CTS 堆积时，WaitHandle 句柄池耗尽
/// </summary>
public static class CtsLeakDemo
{
    // ❌ 反模式 1：在循环中不断创建 CTS，从不释放
    public static async Task LeakInLoop_NoDispose(int iterations = 20)
    {
        Console.WriteLine($"\n  [泄漏] 创建 {iterations} 个 CTS，全部不 Dispose...");

        var ctsList = new List<CancellationTokenSource>(); // 故意保持引用，模拟泄漏

        for (int i = 0; i < iterations; i++)
        {
            // ❌ 带超时的 CTS：内部会创建一个 Timer，永远不释放就永远跑着
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            ctsList.Add(cts); // 某些场景下 CTS 被 Task/回调闭包持有，无法 GC

            await Task.Delay(1).ConfigureAwait(false); // 模拟每次操作间隔
        }

        Console.WriteLine($"  [泄漏] 已创建 {ctsList.Count} 个 CTS，内部 Timer 全部在跑，内存无法释放");
        Console.WriteLine($"  [泄漏] 如果这是请求处理代码，每秒 100 个请求 → 每分钟 6000 个泄漏的 CTS");

        // 演示完毕后手动清理（现实中的泄漏是没有这一步的）
        foreach (var cts in ctsList)
            cts.Dispose();
    }

    // ❌ 反模式 2：Register 回调后忘记释放，且 CTS 被长期持有
    public static async Task LeakViaCallbackRegistration()
    {
        Console.WriteLine("\n  [泄漏] CancellationToken.Register 回调持有外部大对象...");

        var cts = new CancellationTokenSource();

        // 模拟一个大对象（比如缓存数据、HTTP 响应体等）
        var bigData = new byte[1024 * 1024]; // 1MB
        Array.Fill(bigData, (byte)42);

        // ❌ 闭包捕获了 bigData，只要 cts 不 Dispose，bigData 就无法被 GC
        var registration = cts.Token.Register(() =>
        {
            Console.WriteLine($"  取消回调触发，bigData 长度：{bigData.Length}");
        });

        await Task.Delay(10).ConfigureAwait(false);

        // 忘记了 registration.Dispose() 和 cts.Dispose()
        // bigData（1MB）会被 cts 的回调链持有，无法释放

        Console.WriteLine("  [泄漏] bigData（1MB）被回调闭包持有，CTS 不 Dispose 就不释放");

        // 演示后清理
        registration.Dispose();
        cts.Dispose();
    }
}
