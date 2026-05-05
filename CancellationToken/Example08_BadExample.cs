namespace CancellationToken;

/// <summary>
/// 示例8：错误示例 - 不传递 Token
/// </summary>
public static class Example08_BadExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例8：错误示例 - 不传递 Token ===\n");

        using var cts = new CancellationTokenSource();

        // 启动错误的下载任务
        var badTask = BadDownloadAsync("https://example.com/file.zip", cts.Token);

        // 1 秒后取消
        await Task.Delay(1000);
        Console.WriteLine("\n发送取消信号（但是不会生效）...");
        cts.Cancel();

        try
        {
            await badTask;
            Console.WriteLine("✅ 下载完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 下载已取消");
        }

        Console.WriteLine("\n现在演示正确的实现：\n");

        using var cts2 = new CancellationTokenSource();

        // 启动正确的下载任务
        var goodTask = GoodDownloadAsync("https://example.com/file.zip", cts2.Token);

        // 1 秒后取消
        await Task.Delay(1000);
        Console.WriteLine("\n发送取消信号（立即生效）...");
        cts2.Cancel();

        try
        {
            await goodTask;
            Console.WriteLine("✅ 下载完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 下载已取消");
        }
    }

    // ❌ 错误：接收了 Token 但不传递
    private static async Task BadDownloadAsync(string url, System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("❌ 错误实现：不传递 CancellationToken");
        Console.WriteLine("开始下载（无法取消）...");

        for (int i = 1; i <= 5; i++)
        {
            // 没有传递 cancellationToken，无法取消！
            await Task.Delay(1000); // ❌ 缺少 cancellationToken 参数

            Console.WriteLine($"  下载进度: {i * 20}%");
        }

        Console.WriteLine("下载完成（即使取消也完成了）");
    }

    // ✅ 正确：传递 Token
    private static async Task GoodDownloadAsync(string url, System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("✅ 正确实现：传递 CancellationToken");
        Console.WriteLine("开始下载（可以取消）...");

        for (int i = 1; i <= 5; i++)
        {
            // 传递 cancellationToken，可以取消
            await Task.Delay(1000, cancellationToken); // ✅ 正确

            Console.WriteLine($"  下载进度: {i * 20}%");
        }

        Console.WriteLine("下载完成");
    }
}
