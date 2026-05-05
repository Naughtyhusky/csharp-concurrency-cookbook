namespace CancellationToken;

/// <summary>
/// 示例3：链接多个 CancellationToken
/// </summary>
public static class Example03_LinkedTokens
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例3：链接多个 CancellationToken ===\n");

        // 场景：用户取消 + 超时
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // 创建链接 Token：任意一个取消都会触发
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userCts.Token,
            timeoutCts.Token);

        // 模拟用户在 3 秒后点击取消按钮
        var cancelTask = Task.Run(async () =>
        {
            await Task.Delay(3000);
            Console.WriteLine("\n👤 用户点击了取消按钮");
            userCts.Cancel();
        });

        try
        {
            Console.WriteLine("任务开始（5秒超时，用户将在3秒后取消）...");
            await LongRunningOperationAsync(linkedCts.Token);
            Console.WriteLine("✅ 任务完成");
        }
        catch (OperationCanceledException) when (userCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("❌ 用户取消");
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("⏱️ 超时取消");
        }

        await cancelTask; // 等待取消任务完成
    }

    private static async Task LongRunningOperationAsync(System.Threading.CancellationToken cancellationToken)
    {
        for (int i = 1; i <= 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"  处理步骤 {i}/10...");
            await Task.Delay(1000, cancellationToken);
        }

        Console.WriteLine("操作完成！");
    }
}
