namespace CancellationToken;

/// <summary>
/// 示例1：基础用法 - 手动取消
/// </summary>
public static class Example01_BasicCancellation
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例1：基础用法 - 手动取消 ===\n");

        // 创建 CancellationTokenSource
        using var cts = new CancellationTokenSource();

        // 启动异步任务
        var task = LongRunningOperationAsync(cts.Token);

        // 等待 2 秒后取消
        Console.WriteLine("任务开始，2 秒后将取消...");
        await Task.Delay(2000);

        Console.WriteLine("\n发送取消信号...");
        cts.Cancel();

        try
        {
            await task;
            Console.WriteLine("✅ 任务完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 任务已取消");
        }
    }

    private static async Task LongRunningOperationAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("开始长时间运行的操作...");

        for (int i = 1; i <= 10; i++)
        {
            // 检查是否取消
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"  处理步骤 {i}/10...");
            await Task.Delay(500, cancellationToken); // 模拟工作
        }

        Console.WriteLine("操作完成！");
    }
}
