using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace RealWorldCase.Middleware;

/// <summary>
/// 🥇 第三重境界：PipeReader 直接读取请求体
/// 优势：零拷贝、池化内存、背压控制、无需 EnableBuffering
/// 注意：读取后 Body 已被消费，后续管道无法再读取
/// </summary>
public class PipeReaderLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PipeReaderLoggingMiddleware> _logger;
    private const int MaxBodyLogLength = 1024 * 100; // 100KB

    public PipeReaderLoggingMiddleware(RequestDelegate next,
        ILogger<PipeReaderLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        // 🎯 直接使用 BodyReader！跳过 EnableBuffering
        var reader = context.Request.BodyReader;
        long totalLength = 0;

        // 使用 ArrayPool 存储数据以便重建请求体
        byte[]? pooledBuffer = null;

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer; // ReadOnlySequence<byte> - 零拷贝视图

                // 🎯 单段优化路径：大多数请求体在一个段里
                if (buffer.IsSingleSegment)
                {
                    var span = buffer.FirstSpan;
                    var toRead = (int)Math.Min(span.Length, MaxBodyLogLength - totalLength);

                    if (pooledBuffer == null)
                    {
                        pooledBuffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(
                            buffer.Length, MaxBodyLogLength));
                    }

                    span[..toRead].CopyTo(pooledBuffer.AsSpan((int)totalLength));
                    totalLength += toRead;
                }
                else
                {
                    // 多段路径：遍历所有段
                    foreach (var segment in buffer)
                    {
                        var toRead = (int)Math.Min(segment.Length, MaxBodyLogLength - totalLength);
                        if (toRead <= 0) break;

                        if (pooledBuffer == null)
                        {
                            pooledBuffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(
                                buffer.Length, MaxBodyLogLength));
                        }

                        segment.Span[..toRead].CopyTo(pooledBuffer.AsSpan((int)totalLength));
                        totalLength += toRead;
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted || totalLength >= MaxBodyLogLength)
                    break;
            }

            // 🎯 重建请求体供后续管道使用
            if (pooledBuffer != null && totalLength > 0)
            {
                var body = Encoding.UTF8.GetString(pooledBuffer.AsSpan(0, (int)totalLength));
                context.Request.Body = new MemoryStream(pooledBuffer, 0, (int)totalLength);

                sw.Stop();
                _logger.LogInformation(
                    "[PipeReader] Body({Len}B, {Elapsed}ms, SingleSeg={SS}): {Body}",
                    body.Length, sw.ElapsedMilliseconds,
                    totalLength <= 4096,
                    body.Length > 200 ? body[..200] + "..." : body);
            }
            else
            {
                context.Request.Body = new MemoryStream();
            }
        }
        finally
        {
            if (pooledBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(pooledBuffer);
            }
        }

        await _next(context);
    }
}

public static class PipeReaderLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UsePipeReaderLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PipeReaderLoggingMiddleware>();
    }
}
