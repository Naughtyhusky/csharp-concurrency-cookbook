namespace AsyncLocalDemo.Infrastructure
{
    /// <summary>
    /// 基于 AsyncLocal&lt;T&gt; 的请求追踪上下文。
    ///
    /// 核心设计思路：
    ///   - 使用 AsyncLocal&lt;string?&gt; 存储 TraceId，确保值随 ExecutionContext 在整个异步调用链中流转。
    ///   - 对外只暴露静态只读属性（TraceId），写入权只开放给 TraceMiddleware（通过 SetTraceId 内部方法）。
    ///   - 任何地方均可通过 TraceContext.TraceId 直接访问，无需注入。
    ///
    /// 生命周期：
    ///   每个 HTTP 请求进入时，TraceMiddleware 调用 SetTraceId() 写入值；
    ///   由于 AsyncLocal 的"写时复制"语义，这个值只在本次请求的执行上下文中有效，
    ///   并发的其他请求拥有完全独立的副本，互不干扰。
    /// </summary>
    public static class TraceContext
    {
        // 注意：使用 AsyncLocal<string?> 而非值类型，见博客"值类型陷阱"章节的说明。
        private static readonly AsyncLocal<string?> _traceId = new();
        private static readonly AsyncLocal<DateTime> _requestStartTime = new();

        /// <summary>
        /// 当前请求的 TraceId（如果未设置则返回 "N/A"）
        /// </summary>
        public static string TraceId => _traceId.Value ?? "N/A";

        /// <summary>
        /// 当前请求的开始时间
        /// </summary>
        public static DateTime RequestStartTime => _requestStartTime.Value;

        /// <summary>
        /// 当前请求的耗时（毫秒）
        /// </summary>
        public static double ElapsedMs =>
            (DateTime.UtcNow - _requestStartTime.Value).TotalMilliseconds;

        /// <summary>
        /// 由 TraceMiddleware 在请求入口处调用，设置本次请求的 TraceId。
        /// 此方法仅供框架基础设施调用，业务代码请直接读取 TraceId 属性。
        /// </summary>
        internal static void Initialize(string? traceId = null)
        {
            _traceId.Value = traceId ?? Guid.NewGuid().ToString("N")[..16].ToUpper();
            _requestStartTime.Value = DateTime.UtcNow;
        }
    }
}
