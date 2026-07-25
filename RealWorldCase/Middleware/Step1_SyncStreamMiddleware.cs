namespace RealWorldCase.Middleware;

/// <summary>
/// 🥉 第一重境界：传统同步 Stream 读取请求体
/// 问题：阻塞线程、需要手动管理位置、大请求体内存暴涨
/// </summary>
public class SyncStreamLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SyncStreamLoggingMiddleware> _logger;

    public SyncStreamLoggingMiddleware(RequestDelegate next,
        ILogger<SyncStreamLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        // EnableBuffering 将请求体缓存，允许多次读取
        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true); // 不关闭底层流

        // ⚠️ ReadToEndAsync 内部会将全部内容读入内存
        string body = await reader.ReadToEndAsync();

        // ⚠️ 重置流位置——如果启用 EnableBuffering，这里没问题
        // 但如果忘了启 EnableBuffering，Seek 可能抛异常
        context.Request.Body.Position = 0;

        sw.Stop();
        var bodyPreview = body.Length > 200 ? body[..200] + "..." : body;
        _logger.LogInformation(
            "[SyncStream] Body({Len}B, {Elapsed}ms): {Body}",
            body.Length, sw.ElapsedMilliseconds, bodyPreview);

        await _next(context);
    }
}

/// <summary>
/// 同步 stream 中间件的扩展方法
/// </summary>
public static class SyncStreamLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseSyncStreamLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SyncStreamLoggingMiddleware>();
    }
}
