using System.Buffers;

namespace RealWorldCase.Middleware;

/// <summary>
/// 🥈 第二重境界：异步 Stream + ArrayPool 缓冲区
/// 改进：ArrayPool 减少 GC 压力，循环读取可控
/// 问题：仍需 EnableBuffering，代码复杂
/// </summary>
public class AsyncStreamLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AsyncStreamLoggingMiddleware> _logger;
    private const int MaxBodyLogLength = 1024 * 100; // 100KB 限制

    public AsyncStreamLoggingMiddleware(RequestDelegate next,
        ILogger<AsyncStreamLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        context.Request.EnableBuffering();

        // 🎯 使用 ArrayPool 代替 StreamReader 的默认缓冲区
        byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            int totalRead = 0;
            int bytesRead;

            while (totalRead < MaxBodyLogLength &&
                   (bytesRead = await context.Request.Body.ReadAsync(
                       buffer.AsMemory(totalRead, buffer.Length - totalRead))) > 0)
            {
                totalRead += bytesRead;

                // 缓冲区不够了？扩容
                if (totalRead == buffer.Length && totalRead < MaxBodyLogLength)
                {
                    var newSize = Math.Min(buffer.Length * 2, MaxBodyLogLength);
                    var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
                    buffer.AsSpan(0, totalRead).CopyTo(newBuffer);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }
            }

            var body = Encoding.UTF8.GetString(buffer.AsSpan(0, totalRead));

            // 🎯 用量 MemoryStream 重建，比 Position=0 更可靠
            context.Request.Body = new MemoryStream(buffer, 0, totalRead);

            sw.Stop();
            _logger.LogInformation(
                "[AsyncStream] Body({Len}B, {Elapsed}ms): {Body}",
                body.Length, sw.ElapsedMilliseconds,
                body.Length > 200 ? body[..200] + "..." : body);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        await _next(context);
    }
}

public static class AsyncStreamLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAsyncStreamLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AsyncStreamLoggingMiddleware>();
    }
}
