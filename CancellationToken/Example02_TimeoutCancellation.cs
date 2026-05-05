namespace CancellationToken;

/// <summary>
/// 示例2：超时自动取消
/// </summary>
public static class Example02_TimeoutCancellation
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例2：超时自动取消 ===\n");

        // 方式1：创建时设置超时
        Console.WriteLine("--- 方式1：创建时设置超时（3秒）---");
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
        {
            try
            {
                await LongRunningOperationAsync(cts.Token);
                Console.WriteLine("✅ 任务完成");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("⏱️ 任务超时（3秒）");
            }
        }

        Console.WriteLine();

        // 方式2：延迟设置超时
        Console.WriteLine("--- 方式2：延迟设置超时（3秒）---");
        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(TimeSpan.FromSeconds(3)); // 3秒后自动取消

            try
            {
                await LongRunningOperationAsync(cts.Token);
                Console.WriteLine("✅ 任务完成");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("⏱️ 任务超时（3秒）");
            }
        }
    }

    private static async Task LongRunningOperationAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("开始处理（预计需要 10 秒）...");

        for (int i = 1; i <= 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"  步骤 {i}/10");
            await Task.Delay(1000, cancellationToken);
        }

        Console.WriteLine("处理完成！");
    }
}
