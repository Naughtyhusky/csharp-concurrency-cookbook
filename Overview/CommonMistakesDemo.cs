// ========================================
// 常见误区示例
// ========================================

using System.Diagnostics;

namespace Overview;

/// <summary>
/// 常见误区演示
/// </summary>
internal static class CommonMistakesDemo
{
    /// <summary>
    /// 运行常见误区示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("【常见误区演示】\n");

        // 误区1：Task.Run 包装 I/O 操作
        Console.WriteLine("--- 误区1：不要用 Task.Run 包装 I/O 操作 ---");
        await DemonstrateMistake1Async();

        // 误区2：同步阻塞异步方法
        Console.WriteLine("\n--- 误区2：避免使用 .Result 和 .Wait() ---");
        DemonstrateMistake2();

        Console.WriteLine();
    }

    /// <summary>
    /// 误区1：Task.Run 包装 I/O 操作（画蛇添足）
    /// </summary>
    private static async Task DemonstrateMistake1Async()
    {
        var sw = Stopwatch.StartNew();

        // ❌ 错误方式：浪费线程
        Console.WriteLine("❌ 错误方式（Task.Run 包装 I/O）：");
        var badResult = await Task.Run(async () =>
        {
            await Task.Delay(500);
            return "Data";
        });
        Console.WriteLine($"  结果: {badResult}, 耗时: {sw.ElapsedMilliseconds}ms");

        sw.Restart();

        // ✅ 正确方式：直接 await
        Console.WriteLine("✅ 正确方式（直接 await）：");
        await Task.Delay(500);
        var goodResult = "Data";
        Console.WriteLine($"  结果: {goodResult}, 耗时: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 误区2：同步阻塞异步方法
    /// </summary>
    private static void DemonstrateMistake2()
    {
        Console.WriteLine("演示同步阻塞的危害：");

        // 这会阻塞当前线程
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 开始同步等待...");

        var task = Task.Delay(1000);
        task.Wait(); // ❌ 阻塞线程

        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 等待结束（线程被阻塞了1秒）");
        Console.WriteLine("  ⚠️  正确做法：使用 await 而不是 .Wait()");
    }
}
