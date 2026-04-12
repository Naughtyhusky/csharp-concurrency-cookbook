// ========================================
// 并发（Concurrency）示例
// ========================================

namespace Overview;

/// <summary>
/// 并发示例：逻辑上同时处理多个任务
/// </summary>
internal static class ConcurrencyDemo
{
    /// <summary>
    /// 运行并发示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("【1. 并发示例：做饭场景】");
        Console.WriteLine("说明：一个人同时做三道菜，通过异步方式组织多个任务\n");

        await ConcurrentCookingAsync();

        Console.WriteLine();
    }

    /// <summary>
    /// 并发做饭场景：一个人做多道菜
    /// </summary>
    private static async Task ConcurrentCookingAsync()
    {
        Console.WriteLine("开始做饭（并发模式）");

        // 启动三个异步任务
        var task1 = StirFryAsync();
        var task2 = MakeSoupAsync();
        var task3 = SteamRiceAsync();

        // 等待所有任务完成
        await Task.WhenAll(task1, task2, task3);

        Console.WriteLine("所有菜都做好了！");
    }

    private static async Task StirFryAsync()
    {
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 开始炒菜...");
        await Task.Delay(1000);
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 炒菜完成 ✓");
    }

    private static async Task MakeSoupAsync()
    {
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 开始煮汤...");
        await Task.Delay(1500);
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 煮汤完成 ✓");
    }

    private static async Task SteamRiceAsync()
    {
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 开始蒸米饭...");
        await Task.Delay(2000);
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 米饭完成 ✓");
    }
}
