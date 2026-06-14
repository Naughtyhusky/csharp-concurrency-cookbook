using System.Diagnostics;

namespace AsyncEnumerable;

/// <summary>
/// StreamReader vs Pipeline 性能对比
/// </summary>
public class StreamReaderVsPipelineComparison
{
    /// <summary>
    /// 对比 StreamReader 和 Pipeline 的性能差异
    /// </summary>
    public static async Task ComparePerformanceAsync()
    {
        Console.WriteLine("=== StreamReader vs Pipeline 性能对比 ===\n");

        // 创建测试文件
        var testFile = Path.GetTempFileName();
        const int lineCount = 100_000; // 10 万行

        Console.WriteLine($"创建测试文件：{lineCount:N0} 行...");
        await CreateLargeTestFileAsync(testFile, lineCount);
        var fileSize = new FileInfo(testFile).Length / 1024 / 1024;
        Console.WriteLine($"文件大小：{fileSize:F2} MB\n");

        // 方案1：StreamReader（传统方式）
        Console.WriteLine("【方案1】StreamReader（传统方式）");
        var processor = new LogStreamProcessor();

        var sw1 = Stopwatch.StartNew();
        int count1 = 0;
        var beforeGC1 = GC.GetTotalMemory(true);

        await foreach (var line in processor.ReadAllLinesAsync(testFile))
        {
            count1++;
            // 模拟简单处理
        }

        sw1.Stop();
        var afterGC1 = GC.GetTotalMemory(false);
        var memoryUsed1 = (afterGC1 - beforeGC1) / 1024 / 1024;

        Console.WriteLine($"  读取行数：{count1:N0}");
        Console.WriteLine($"  耗时：{sw1.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  峰值内存：~{memoryUsed1} MB");
        Console.WriteLine($"  吞吐量：{count1 * 1000 / sw1.ElapsedMilliseconds:N0} 行/秒\n");

        // 强制 GC
        GC.Collect();
        await Task.Delay(100);

        // 方案2：Pipeline（高性能方式）
        Console.WriteLine("【方案2】Pipeline（高性能方式，.NET Core 3.0+）");

        var sw2 = Stopwatch.StartNew();
        int count2 = 0;
        var beforeGC2 = GC.GetTotalMemory(true);

        await foreach (var line in processor.ReadAllLinesWithPipelineAsync(testFile))
        {
            count2++;
            // 模拟简单处理
        }

        sw2.Stop();
        var afterGC2 = GC.GetTotalMemory(false);
        var memoryUsed2 = (afterGC2 - beforeGC2) / 1024 / 1024;

        Console.WriteLine($"  读取行数：{count2:N0}");
        Console.WriteLine($"  耗时：{sw2.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  峰值内存：~{memoryUsed2} MB");
        Console.WriteLine($"  吞吐量：{count2 * 1000 / sw2.ElapsedMilliseconds:N0} 行/秒\n");

        // 性能对比总结
        Console.WriteLine("📊 性能对比总结：");
        var speedup = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        var memorySaved = memoryUsed1 - memoryUsed2;

        Console.WriteLine($"  Pipeline 速度提升：{speedup:F2}x");
        Console.WriteLine($"  内存节省：{memorySaved} MB");
        Console.WriteLine($"  推荐场景：大文件（> 10 MB）、高吞吐量场景");

        // 清理
        File.Delete(testFile);
    }

    /// <summary>
    /// 创建大型测试文件
    /// </summary>
    private static async Task CreateLargeTestFileAsync(string filePath, int lineCount)
    {
        await using var writer = new StreamWriter(filePath, false);

        for (int i = 1; i <= lineCount; i++)
        {
            // 写入带有时间戳的日志行
            await writer.WriteLineAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] Log message #{i} with some additional content to make the line longer");
        }
    }

    /// <summary>
    /// 演示两种方式的使用场景
    /// </summary>
    public static async Task DemoUsageScenarios()
    {
        Console.WriteLine("\n=== 使用场景对比 ===\n");

        var processor = new LogStreamProcessor();
        var testFile = Path.GetTempFileName();
        await CreateLargeTestFileAsync(testFile, 1000);

        // 场景1：小文件，简单读取
        Console.WriteLine("【场景1】小文件（< 10 MB），简单读取");
        Console.WriteLine("推荐：StreamReader（代码简单，性能够用）\n");

        await foreach (var line in processor.ReadAllLinesAsync(testFile))
        {
            // 简单处理
            if (line.Contains("ERROR"))
            {
                // 处理错误
            }
        }

        // 场景2：大文件，高性能需求
        Console.WriteLine("【场景2】大文件（> 100 MB），高性能需求");
        Console.WriteLine("推荐：Pipeline（更少的内存分配，更高的吞吐量）\n");

        await foreach (var line in processor.ReadAllLinesWithPipelineAsync(testFile))
        {
            // 高性能处理
            if (line.Contains("ERROR"))
            {
                // 处理错误
            }
        }

        // 场景3：实时监控（tail -f）
        Console.WriteLine("【场景3】实时监控（类似 tail -f）");
        Console.WriteLine("推荐：StreamReader（实时性更重要，性能差异不明显）\n");

        // 清理
        File.Delete(testFile);
    }
}
