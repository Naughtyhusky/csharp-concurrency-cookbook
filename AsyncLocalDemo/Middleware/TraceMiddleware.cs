using AsyncLocalDemo.Infrastructure;

namespace AsyncLocalDemo.Middleware
{
    /// <summary>
    /// 请求追踪中间件。
    ///
    /// 职责：
    ///   1. 在每个请求进入管道时，从请求头 X-Trace-Id 中读取（或生成）TraceId，
    ///      调用 TraceContext.Initialize() 写入 AsyncLocal，让整个异步调用链都能取到。
    ///   2. 将 TraceId 写入响应头 X-Trace-Id，方便客户端/网关关联日志。
    ///   3. 请求结束后记录总耗时日志。
    ///
    /// 注册顺序：应放在管道最前端（UseRouting 之前），确保后续所有中间件和业务代码都能取到 TraceId。
    /// </summary>
    public sealed class TraceMiddleware(RequestDelegate next, ILogger<TraceMiddleware> logger)
    {
        // 约定的请求头名称，与网关/调用方对齐
        private const string TraceIdHeader = "X-Trace-Id";

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. 优先使用调用方传入的 TraceId（链路追踪透传），否则自动生成
            var incomingTraceId = context.Request.Headers[TraceIdHeader].FirstOrDefault();
            TraceContext.Initialize(incomingTraceId);

            // 2. 将 TraceId 写入响应头，调用方可通过此 Header 关联请求
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[TraceIdHeader] = TraceContext.TraceId;
                return Task.CompletedTask;
            });

            // 3. 将 TraceId 写入日志作用域（Scope），使该请求所有日志自动携带 TraceId
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["TraceId"] = TraceContext.TraceId
            }))
            {
                logger.LogInformation(
                    "请求开始 [{Method}] {Path}  TraceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    TraceContext.TraceId);

                try
                {
                    await next(context);
                }
                finally
                {
                    logger.LogInformation(
                        "请求结束 [{Method}] {Path}  TraceId={TraceId}  StatusCode={StatusCode}  Elapsed={Elapsed:F1}ms",
                        context.Request.Method,
                        context.Request.Path,
                        TraceContext.TraceId,
                        context.Response.StatusCode,
                        TraceContext.ElapsedMs);
                }
            }
        }
    }

    /// <summary>TraceMiddleware 的扩展方法，方便在 Program.cs 中注册</summary>
    public static class TraceMiddlewareExtensions
    {
        public static IApplicationBuilder UseTracing(this IApplicationBuilder app)
            => app.UseMiddleware<TraceMiddleware>();
    }
}
