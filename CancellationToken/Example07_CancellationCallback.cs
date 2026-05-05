namespace CancellationToken;

/// <summary>
/// 示例7：取消回调注册
/// </summary>
public static class Example07_CancellationCallback
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例7：取消回调注册 ===\n");

        using var cts = new CancellationTokenSource();

        // 启动任务
        var task = DownloadWithCleanupAsync("https://example.com/file.zip", cts.Token);

        // 2 秒后取消
        await Task.Delay(2000);
        Console.WriteLine("\n发送取消信号...");
        cts.Cancel();

        try
        {
            await task;
            Console.WriteLine("✅ 下载完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 下载已取消");
        }
    }

    private static async Task DownloadWithCleanupAsync(string url, System.Threading.CancellationToken cancellationToken)
    {
        var tempFile = Path.GetTempFileName();
        Console.WriteLine($"创建临时文件: {tempFile}");

        // 注册取消回调：删除临时文件
        using var registration = cancellationToken.Register(() =>
        {
            Console.WriteLine("\n🧹 取消回调触发：删除临时文件");
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
                Console.WriteLine($"✅ 已删除: {tempFile}");
            }
        });

        try
        {
            // 模拟下载过程
            Console.WriteLine("开始下载...");
            for (int i = 1; i <= 10; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(500, cancellationToken);
                Console.WriteLine($"  下载进度: {i * 10}%");

                // 模拟写入临时文件
                await File.AppendAllTextAsync(tempFile, $"Chunk {i}\n", cancellationToken);
            }

            Console.WriteLine($"下载完成，保存到: {tempFile}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("下载被中断");
            throw;
        }
    }
}
