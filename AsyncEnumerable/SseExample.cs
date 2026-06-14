using System.Runtime.CompilerServices;

namespace AsyncEnumerable;

/// <summary>
/// SSE (Server-Sent Events) 示例：演示 IAsyncEnumerable 与 SSE 的关系
/// </summary>
/// <remarks>
/// 在 ASP.NET Core 中，返回 IAsyncEnumerable&lt;T&gt; 的控制器方法会自动转换为 SSE 格式
/// </remarks>
public class SseExample
{
    /// <summary>
    /// 示例1：实时时钟推送
    /// </summary>
    /// <remarks>
    /// 模拟 SSE 端点：GET /api/clock/stream
    /// 客户端：new EventSource('/api/clock/stream')
    /// </remarks>
    public async IAsyncEnumerable<string> StreamClockAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // 每秒推送一次当前时间
            yield return $"🕐 当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            await Task.Delay(1000, cancellationToken);
        }
    }

    /// <summary>
    /// 示例2：股票价格实时推送
    /// </summary>
    public async IAsyncEnumerable<StockPrice> StreamStockPricesAsync(
        string symbol,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random();
        decimal basePrice = 100.00m;

        while (!cancellationToken.IsCancellationRequested)
        {
            // 模拟价格波动（±2%）
            var change = (decimal)(random.NextDouble() * 4 - 2);
            basePrice += change;

            yield return new StockPrice
            {
                Symbol = symbol,
                Price = Math.Round(basePrice, 2),
                Timestamp = DateTime.Now,
                Change = Math.Round(change, 2)
            };

            // 每 500ms 更新一次
            await Task.Delay(500, cancellationToken);
        }
    }

    /// <summary>
    /// 示例3：AI 流式输出（模拟 ChatGPT 打字机效果）
    /// </summary>
    public async IAsyncEnumerable<string> StreamAiResponseAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 模拟 AI 生成的回答
        var fullResponse = $"关于「{prompt}」的回答：这是一个非常有趣的问题。让我为您详细解答...";

        // 逐字返回
        foreach (var character in fullResponse)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return character.ToString();

            // 模拟打字速度（30-80ms 每字符）
            await Task.Delay(Random.Shared.Next(30, 80), cancellationToken);
        }
    }

    /// <summary>
    /// 示例4：服务器日志实时推送
    /// </summary>
    public async IAsyncEnumerable<LogMessage> StreamLogsAsync(
        LogLevel minLevel = LogLevel.Information,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var logProcessor = new LogStreamProcessor();
        var logFile = "app.log";

        // 确保日志文件存在
        if (!File.Exists(logFile))
        {
            await LogStreamProcessor.CreateSampleLogFileAsync(logFile, 100);
        }

        await foreach (var line in logProcessor.TailFileAsync(logFile, cancellationToken))
        {
            // 解析日志行
            var parts = line.Split(["] [", "[", "]"], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;

            if (Enum.TryParse<LogLevel>(parts[1], true, out var level) && level >= minLevel)
            {
                yield return new LogMessage
                {
                    Timestamp = DateTime.Parse(parts[0]),
                    Level = level,
                    Message = string.Join(" ", parts.Skip(2))
                };
            }
        }
    }

    /// <summary>
    /// 示例5：进度更新推送
    /// </summary>
    public async IAsyncEnumerable<ProgressUpdate> StreamProgressAsync(
        string taskName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const int totalSteps = 100;

        for (int i = 1; i <= totalSteps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new ProgressUpdate
            {
                TaskName = taskName,
                Percentage = i,
                CurrentStep = i,
                TotalSteps = totalSteps,
                Message = $"正在处理第 {i} 步..."
            };

            // 模拟耗时操作
            await Task.Delay(100, cancellationToken);
        }

        // 完成
        yield return new ProgressUpdate
        {
            TaskName = taskName,
            Percentage = 100,
            CurrentStep = totalSteps,
            TotalSteps = totalSteps,
            Message = "✅ 任务完成！"
        };
    }

    /// <summary>
    /// 示例6：组合多个流（合并多个数据源）
    /// </summary>
    public async IAsyncEnumerable<string> StreamMultipleSourcesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 模拟多个数据源
        var source1 = StreamSourceAsync("源A", 1000, cancellationToken);
        var source2 = StreamSourceAsync("源B", 1500, cancellationToken);
        var source3 = StreamSourceAsync("源C", 800, cancellationToken);

        // 合并流（轮询方式）
        var enumerator1 = source1.GetAsyncEnumerator(cancellationToken);
        var enumerator2 = source2.GetAsyncEnumerator(cancellationToken);
        var enumerator3 = source3.GetAsyncEnumerator(cancellationToken);

        try
        {
            bool hasMore = true;
            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                hasMore = false;

                if (await enumerator1.MoveNextAsync())
                {
                    yield return enumerator1.Current;
                    hasMore = true;
                }

                if (await enumerator2.MoveNextAsync())
                {
                    yield return enumerator2.Current;
                    hasMore = true;
                }

                if (await enumerator3.MoveNextAsync())
                {
                    yield return enumerator3.Current;
                    hasMore = true;
                }
            }
        }
        finally
        {
            await enumerator1.DisposeAsync();
            await enumerator2.DisposeAsync();
            await enumerator3.DisposeAsync();
        }
    }

    private async IAsyncEnumerable<string> StreamSourceAsync(
        string sourceName,
        int intervalMs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int counter = 1;
        while (counter <= 5 && !cancellationToken.IsCancellationRequested)
        {
            yield return $"[{sourceName}] 消息 #{counter}";
            await Task.Delay(intervalMs, cancellationToken);
            counter++;
        }
    }
}

/// <summary>
/// 股票价格数据模型
/// </summary>
public record StockPrice
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal Change { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// 日志消息模型
/// </summary>
public record LogMessage
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// 进度更新模型
/// </summary>
public record ProgressUpdate
{
    public string TaskName { get; init; } = string.Empty;
    public int Percentage { get; init; }
    public int CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public string Message { get; init; } = string.Empty;
}
