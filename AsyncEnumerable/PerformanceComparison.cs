using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AsyncEnumerable;

/// <summary>
/// 性能对比示例：量化分析 IAsyncEnumerable 的优势
/// </summary>
public class PerformanceComparison
{
    /// <summary>
    /// 对比1：内存占用对比
    /// </summary>
    public static async Task CompareMemoryUsage()
    {
        Console.WriteLine("【性能对比1】内存占用对比");
        Console.WriteLine("场景：处理 10 万条记录\n");

        // 方案1：一次性加载
        Console.WriteLine("方案1：Task<List<T>>（一次性加载）");
        var beforeGC1 = GC.GetTotalMemory(true);
        var list = await LoadAllDataAsync(100000);
        var afterLoad1 = GC.GetTotalMemory(false);
        var memoryUsed1 = (afterLoad1 - beforeGC1) / 1024 / 1024;
        Console.WriteLine($"  加载后内存增加: ~{memoryUsed1} MB");

        int count1 = 0;
        foreach (var item in list)
        {
            count1++;
            // 模拟处理
        }
        Console.WriteLine($"  处理完成: {count1} 条\n");

        // 清理
        list.Clear();
        GC.Collect();
        await Task.Delay(100);

        // 方案2：流式处理
        Console.WriteLine("方案2：IAsyncEnumerable<T>（流式处理）");
        var beforeGC2 = GC.GetTotalMemory(true);

        int count2 = 0;
        await foreach (var item in LoadDataStreamAsync(100000))
        {
            count2++;
            // 模拟处理
        }

        var afterProcess2 = GC.GetTotalMemory(false);
        var memoryUsed2 = (afterProcess2 - beforeGC2) / 1024 / 1024;
        Console.WriteLine($"  处理期间峰值内存增加: ~{memoryUsed2} MB");
        Console.WriteLine($"  处理完成: {count2} 条\n");

        Console.WriteLine($"💡 结论：流式处理内存占用减少约 {memoryUsed1 - memoryUsed2} MB");
    }

    /// <summary>
    /// 对比2：响应时间对比（首次响应）
    /// </summary>
    public static async Task CompareResponseTime()
    {
        Console.WriteLine("\n【性能对比2】首次响应时间对比");
        Console.WriteLine("场景：从 API 获取分页数据（10 页，每页 100 条）\n");

        // 方案1：等待全部加载
        Console.WriteLine("方案1：等待全部加载");
        var sw1 = Stopwatch.StartNew();
        var allData = await FetchAllPagesAsync(10, 100);
        var loadTime = sw1.ElapsedMilliseconds;
        Console.WriteLine($"  首次可用时间: {loadTime}ms");

        int processed1 = 0;
        foreach (var item in allData)
        {
            processed1++;
            if (processed1 == 1)
                Console.WriteLine($"  开始处理第一条: {sw1.ElapsedMilliseconds}ms");
        }
        sw1.Stop();
        Console.WriteLine($"  总耗时: {sw1.ElapsedMilliseconds}ms\n");

        // 方案2：流式加载
        Console.WriteLine("方案2：流式加载");
        var sw2 = Stopwatch.StartNew();
        int processed2 = 0;

        await foreach (var item in FetchAllPagesStreamAsync(10, 100))
        {
            processed2++;
            if (processed2 == 1)
                Console.WriteLine($"  开始处理第一条: {sw2.ElapsedMilliseconds}ms ⚡");
        }
        sw2.Stop();
        Console.WriteLine($"  总耗时: {sw2.ElapsedMilliseconds}ms\n");

        Console.WriteLine($"💡 结论：流式处理首次响应快 {loadTime - sw2.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 对比3：提前退出的效率
    /// </summary>
    public static async Task CompareEarlyExit()
    {
        Console.WriteLine("\n【性能对比3】提前退出效率对比");
        Console.WriteLine("场景：查找前 10 条符合条件的记录（总共 10 万条）\n");

        // 方案1：先加载全部
        Console.WriteLine("方案1：先加载全部，再查找");
        var sw1 = Stopwatch.StartNew();
        var allData = await LoadAllDataAsync(100000);
        Console.WriteLine($"  全部加载完成: {sw1.ElapsedMilliseconds}ms");

        int found1 = 0;
        foreach (var item in allData)
        {
            if (item.Id % 1000 == 0) // 模拟条件
            {
                found1++;
                if (found1 >= 10)
                    break;
            }
        }
        sw1.Stop();
        Console.WriteLine($"  找到 {found1} 条，总耗时: {sw1.ElapsedMilliseconds}ms\n");

        // 方案2：流式查找
        Console.WriteLine("方案2：流式查找（找到就停止）");
        var sw2 = Stopwatch.StartNew();
        int found2 = 0;

        await foreach (var item in LoadDataStreamAsync(100000))
        {
            if (item.Id % 1000 == 0)
            {
                found2++;
                if (found2 >= 10)
                    break; // 立即停止，不再加载后续数据
            }
        }
        sw2.Stop();
        Console.WriteLine($"  找到 {found2} 条，总耗时: {sw2.ElapsedMilliseconds}ms\n");

        Console.WriteLine($"💡 结论：流式处理节省 {sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds}ms（无需加载无用数据）");
    }

    /// <summary>
    /// 示例：实际场景 - 日志分析
    /// </summary>
    public static async Task RealWorldScenario_LogAnalysis()
    {
        Console.WriteLine("\n【实际场景】日志分析：查找错误日志");

        // 创建临时日志文件
        var logFile = Path.GetTempFileName();
        await CreateLargeLogFileAsync(logFile, 10000);

        Console.WriteLine($"已创建日志文件: {logFile}（10,000 行）\n");

        // 使用 IAsyncEnumerable 逐行读取和分析
        var sw = Stopwatch.StartNew();
        int errorCount = 0;
        int warningCount = 0;
        int lineCount = 0;

        await foreach (var line in ReadLogFileAsync(logFile))
        {
            lineCount++;

            if (line.Contains("[ERROR]"))
                errorCount++;
            else if (line.Contains("[WARNING]"))
                warningCount++;

            // 每 1000 行报告一次进度
            if (lineCount % 1000 == 0)
                Console.WriteLine($"  已处理 {lineCount} 行...");
        }

        sw.Stop();
        Console.WriteLine($"\n📊 分析完成:");
        Console.WriteLine($"  总行数: {lineCount}");
        Console.WriteLine($"  错误日志: {errorCount}");
        Console.WriteLine($"  警告日志: {warningCount}");
        Console.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  峰值内存: 很低（逐行处理，不占用大量内存）");

        // 清理临时文件
        File.Delete(logFile);
    }

    // === 辅助方法 ===

    private static async Task<List<DataItem>> LoadAllDataAsync(int count)
    {
        var result = new List<DataItem>(count);

        for (int i = 0; i < count; i++)
        {
            result.Add(new DataItem { Id = i, Value = $"Data-{i}" });

            // 模拟加载延迟（每 1000 条稍微等待一下）
            if (i % 1000 == 0 && i > 0)
                await Task.Delay(1);
        }

        return result;
    }

    private static async IAsyncEnumerable<DataItem> LoadDataStreamAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new DataItem { Id = i, Value = $"Data-{i}" };

            // 模拟加载延迟
            if (i % 1000 == 0 && i > 0)
                await Task.Delay(1);
        }
    }

    private static async Task<List<string>> FetchAllPagesAsync(int pageCount, int pageSize)
    {
        var result = new List<string>(pageCount * pageSize);

        for (int page = 1; page <= pageCount; page++)
        {
            await Task.Delay(100); // 模拟 API 延迟

            for (int i = 1; i <= pageSize; i++)
            {
                result.Add($"Page{page}-Item{i}");
            }
        }

        return result;
    }

    private static async IAsyncEnumerable<string> FetchAllPagesStreamAsync(int pageCount, int pageSize)
    {
        for (int page = 1; page <= pageCount; page++)
        {
            await Task.Delay(100); // 模拟 API 延迟

            for (int i = 1; i <= pageSize; i++)
            {
                yield return $"Page{page}-Item{i}";
            }
        }
    }

    private static async Task CreateLargeLogFileAsync(string filePath, int lineCount)
    {
        await using var writer = new StreamWriter(filePath);
        var random = new Random();

        for (int i = 1; i <= lineCount; i++)
        {
            var timestamp = DateTime.Now.AddSeconds(-lineCount + i);
            var level = random.Next(100) switch
            {
                < 70 => "INFO",
                < 90 => "WARNING",
                _ => "ERROR"
            };

            await writer.WriteLineAsync($"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] Log message #{i}");
        }
    }

    private static async IAsyncEnumerable<string> ReadLogFileAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(filePath);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) // null 表示已到达流的末尾
                break;

            yield return line;
        }
    }

    private class DataItem
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
