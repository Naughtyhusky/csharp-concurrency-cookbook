using System.IO.Pipelines;
using System.Text;

namespace RealWorldCase.Middleware;

/// <summary>
/// 👑 终极方案：流式 PipeReader + 只记录前 N 字节
/// 适用于只关心请求体前面部分的场景（如日志预览）
/// </summary>
public class StreamingPipeReaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StreamingPipeReaderMiddleware> _logger;
    private const int MaxBodyLogLength = 4096; // 只记录前 4KB

    public StreamingPipeReaderMiddleware(RequestDelegate next,
        ILogger<StreamingPipeReaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        // 🎯 EnableBuffering 配合 PipeReader：
        // EnableBuffering 支持后续 seek，PipeReader 做高效读取
        context.Request.EnableBuffering(bufferThreshold: 1024 * 100);

        var reader = context.Request.BodyReader;
        var previewBuilder = new StringBuilder();
        int bytesRead = 0;

        while (bytesRead < MaxBodyLogLength)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            // 🎯 只处理前 MaxBodyLogLength 字节
            foreach (var segment in buffer)
            {
                var remaining = MaxBodyLogLength - bytesRead;
                if (remaining <= 0) break;

                var toRead = Math.Min(segment.Length, remaining);
                previewBuilder.Append(Encoding.UTF8.GetString(
                    segment.Span[..toRead]));
                bytesRead += toRead;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted || bytesRead >= MaxBodyLogLength)
                break;
        }

        sw.Stop();

        var truncated = bytesRead >= MaxBodyLogLength ? " (已截断)" : "";
        _logger.LogInformation(
            "[StreamingPipeReader] Preview({Len}B{Truncated}, {Elapsed}ms): {Body}",
            bytesRead, truncated, sw.ElapsedMilliseconds, previewBuilder);

        // 🎯 重置位置，让 Controller 能读取完整请求体
        context.Request.Body.Position = 0;

        await _next(context);
    }
}

public static class StreamingPipeReaderMiddlewareExtensions
{
    public static IApplicationBuilder UseStreamingPipeReaderLogging(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StreamingPipeReaderMiddleware>();
    }
}
