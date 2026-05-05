namespace CancellationToken;

/// <summary>
/// 示例6：文件批量处理器
/// </summary>
public static class Example06_FileBatchProcessor
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例6：文件批量处理器 ===\n");

        // 创建临时测试文件
        var tempDir = Path.Combine(Path.GetTempPath(), "CancellationTokenTest");
        Directory.CreateDirectory(tempDir);

        var files = new List<string>();
        for (int i = 1; i <= 5; i++)
        {
            var filePath = Path.Combine(tempDir, $"file{i}.txt");
            await File.WriteAllTextAsync(filePath, $"文件 {i} 的内容\n" + string.Join("\n", Enumerable.Range(1, 100).Select(x => $"Line {x}")));
            files.Add(filePath);
        }

        Console.WriteLine($"已创建 {files.Count} 个测试文件");
        Console.WriteLine($"临时目录: {tempDir}\n");

        using var cts = new CancellationTokenSource();

        // 模拟用户在 3 秒后取消
        var cancelTask = Task.Run(async () =>
        {
            await Task.Delay(3000);
            Console.WriteLine("\n👤 用户点击了取消按钮");
            cts.Cancel();
        });

        var progress = new Progress<string>(msg =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
        });

        var processor = new FileBatchProcessor();

        try
        {
            await processor.ProcessFilesAsync(files, progress, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n批量处理已取消");
        }

        await cancelTask;

        // 清理临时文件
        Console.WriteLine("\n清理临时文件...");
        Directory.Delete(tempDir, true);
        Console.WriteLine("✅ 清理完成");
    }
}

public class FileBatchProcessor
{
    public async Task ProcessFilesAsync(
        List<string> filePaths,
        IProgress<string> progress,
        System.Threading.CancellationToken cancellationToken)
    {
        // 整体超时 5 分钟
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        int processed = 0;
        int failed = 0;

        foreach (var filePath in filePaths)
        {
            CancellationTokenSource fileTimeoutCts = null;
            CancellationTokenSource fileCts = null;

            try
            {
                // 单个文件超时 2 秒
                fileTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                fileCts = CancellationTokenSource.CreateLinkedTokenSource(
                    linkedCts.Token,
                    fileTimeoutCts.Token);

                progress?.Report($"处理中: {Path.GetFileName(filePath)}");

                await ProcessSingleFileAsync(filePath, fileCts.Token);

                processed++;
                progress?.Report($"✅ 完成: {Path.GetFileName(filePath)} ({processed}/{filePaths.Count})");
            }
            catch (OperationCanceledException) when (fileTimeoutCts?.Token.IsCancellationRequested == true)
            {
                failed++;
                progress?.Report($"⏱️ 超时: {Path.GetFileName(filePath)} ({processed}/{filePaths.Count})");
            }
            catch (OperationCanceledException)
            {
                progress?.Report($"❌ 用户取消 ({processed}/{filePaths.Count})");
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                progress?.Report($"❌ 错误: {Path.GetFileName(filePath)} - {ex.Message}");
            }
            finally
            {
                fileCts?.Dispose();
                fileTimeoutCts?.Dispose();
            }
        }

        progress?.Report($"处理完成！成功: {processed}, 失败: {failed}");
    }

    private async Task ProcessSingleFileAsync(string filePath, System.Threading.CancellationToken cancellationToken)
    {
        // 读取文件
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

        // 模拟处理
        foreach (var line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken); // 模拟处理时间
        }

        // 保存结果
        var outputPath = filePath + ".processed";
        await File.WriteAllLinesAsync(outputPath, lines.Select(l => l.ToUpper()), cancellationToken);
    }
}
