namespace CancellationToken;

/// <summary>
/// 示例4：文件下载器（用户取消 + 超时）
/// </summary>
public static class Example04_FileDownloader
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例4：文件下载器（用户取消 + 超时）===\n");

        using var userCts = new CancellationTokenSource();

        // 模拟用户在 3 秒后点击取消
        var cancelTask = Task.Run(async () =>
        {
            await Task.Delay(3000);
            Console.WriteLine("\n👤 用户点击了取消按钮");
            userCts.Cancel();
        });

        try
        {
            Console.WriteLine("开始下载文件（10秒超时）...");
            await DownloadFileAsync(
                "https://example.com/large-file.zip",
                TimeSpan.FromSeconds(10),
                userCts.Token);

            Console.WriteLine("✅ 下载完成");
        }
        catch (TimeoutException)
        {
            Console.WriteLine("⏱️ 下载超时");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 用户取消下载");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 下载失败: {ex.Message}");
        }

        await cancelTask;
    }

    private static async Task DownloadFileAsync(
        string url,
        TimeSpan timeout,
        System.Threading.CancellationToken userCancellationToken)
    {
        // 创建超时 Token
        using var timeoutCts = new CancellationTokenSource(timeout);

        // 链接用户取消和超时
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userCancellationToken,
            timeoutCts.Token);

        try
        {
            // 模拟下载过程
            int totalChunks = 10;
            int downloaded = 0;

            for (int i = 1; i <= totalChunks; i++)
            {
                // 检查取消
                linkedCts.Token.ThrowIfCancellationRequested();

                // 模拟下载分块
                await Task.Delay(1000, linkedCts.Token);
                downloaded += 10;

                // 显示进度
                Console.WriteLine($"  下载进度: {downloaded}%");
            }
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"下载超时（{timeout.TotalSeconds}秒）");
        }
    }
}
