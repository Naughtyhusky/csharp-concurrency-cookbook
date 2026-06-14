using System.Runtime.CompilerServices;
using System.IO.Pipelines;
using System.Text;
using System.Buffers;

namespace AsyncEnumerable;

/// <summary>
/// 日志流处理器：演示实时文件监控场景（类似 tail -f）
/// </summary>
public class LogStreamProcessor
{
    /// <summary>
    /// 实时监控日志文件，返回新增的行
    /// </summary>
    /// <param name="filePath">日志文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async IAsyncEnumerable<string> TailFileAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在: {filePath}");

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fileStream);

        // 先跳到文件末尾
        reader.BaseStream.Seek(0, SeekOrigin.End);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line != null)
            {
                yield return line; // 返回新增的行
            }
            else
            {
                // 文件暂时没有新内容，等待一会儿
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 读取整个文件（逐行返回）
    /// </summary>
    public async IAsyncEnumerable<string> ReadAllLinesAsync(
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

    /// <summary>
    /// 过滤日志：只返回包含指定级别的日志
    /// </summary>
    public async IAsyncEnumerable<LogEntry> FilterLogsAsync(
        string filePath,
        LogLevel minLevel = LogLevel.Information,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var line in ReadAllLinesAsync(filePath, cancellationToken))
        {
            var entry = ParseLogEntry(line);
            if (entry != null && entry.Level >= minLevel)
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// 解析日志行（简单示例）
    /// </summary>
    private LogEntry? ParseLogEntry(string line)
    {
        // 假设日志格式：[时间] [级别] 消息
        // 例如：[2024-01-01 10:00:00] [ERROR] 发生错误

        if (string.IsNullOrWhiteSpace(line))
            return null;

        try
        {
            var parts = line.Split(["] [", "[", "]"], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return null;

            var timestamp = DateTime.Parse(parts[0]);
            var level = Enum.Parse<LogLevel>(parts[1], true);
            var message = string.Join(" ", parts.Skip(2));

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 演示：创建示例日志文件
    /// </summary>
    public static async Task CreateSampleLogFileAsync(string filePath, int lineCount = 1000)
    {
        await using var writer = new StreamWriter(filePath, false);

        var random = new Random();
        var levels = new[] { LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error };

        for (int i = 1; i <= lineCount; i++)
        {
            var timestamp = DateTime.Now.AddSeconds(-lineCount + i);
            var level = levels[random.Next(levels.Length)];
            var message = $"日志消息 #{i}";

            await writer.WriteLineAsync($"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");

            // 模拟慢速写入
            if (i % 100 == 0)
                await Task.Delay(10);
        }

        Console.WriteLine($"✅ 已创建示例日志文件: {filePath}，共 {lineCount} 行");
    }

    #region Pipeline 高性能实现（.NET Core 3.0+）

    /// <summary>
    /// 使用 Pipeline 读取文件（高性能版本）
    /// 适合大文件、高吞吐量场景
    /// </summary>
    /// <remarks>
    /// 优势：
    /// - 使用内存池，减少 GC 压力
    /// - 更高效的缓冲管理
    /// - 支持背压控制
    /// </remarks>
    public async IAsyncEnumerable<string> ReadAllLinesWithPipelineAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(filePath);
        var reader = PipeReader.Create(fileStream);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 读取数据到缓冲区
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                // 逐行解析
                while (TryReadLine(ref buffer, out var line))
                {
                    yield return line;
                }

                // 告诉 PipeReader 我们已经处理了多少数据
                reader.AdvanceTo(buffer.Start, buffer.End);

                // 如果已经完成，退出循环
                if (result.IsCompleted)
                    break;
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    /// <summary>
    /// 从 ReadOnlySequence 中读取一行
    /// </summary>
    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out string line)
    {
        // 查找换行符（\n 或 \r\n）
        var position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = string.Empty;
            return false;
        }

        // 提取一行数据
        var lineBuffer = buffer.Slice(0, position.Value);
        line = Encoding.UTF8.GetString(lineBuffer);

        // 移除可能的 \r
        if (line.EndsWith('\r'))
            line = line[..^1];

        // 移动到下一行的起始位置
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        return true;
    }

    #endregion
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;

    public override string ToString()
        => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
}

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
