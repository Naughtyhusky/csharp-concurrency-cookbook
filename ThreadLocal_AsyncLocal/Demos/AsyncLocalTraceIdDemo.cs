namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 07：AsyncLocal&lt;T&gt; 实战 —— 模拟 ASP.NET Core 请求追踪 TraceId
    ///
    /// 在 ASP.NET Core 中，每个 HTTP 请求都有一个唯一的 TraceId（追踪 ID）。
    /// 无论这个请求在哪个线程上执行，无论经历了多少次 await，
    /// 你都能从 AsyncLocal 中取到这个 TraceId，然后写进日志。
    ///
    /// 这就是 ILogger 背后的 Scope 机制、Activity、HttpContext.TraceIdentifier 的底层原理之一。
    /// </summary>
    public static class AsyncLocalTraceIdDemo
    {
        // 模拟 ASP.NET Core 的"请求上下文"
        private static readonly AsyncLocal<string?> _traceId = new();
        private static readonly AsyncLocal<string?> _userName = new();

        /// <summary>模拟日志记录，自动带上 TraceId</summary>
        private static void Log(string message)
        {
            var traceId = _traceId.Value ?? "N/A";
            var user = _userName.Value ?? "anonymous";
            var tid = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"  [TraceId={traceId,-8}][User={user,-8}][线程={tid,2}] {message}");
        }

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Demo 07：AsyncLocal 实战 —— 请求追踪 TraceId ===\n");

            // 模拟 3 个并发请求
            var requests = new[]
            {
                SimulateRequestAsync("REQ-001", "Alice"),
                SimulateRequestAsync("REQ-002", "Bob"),
                SimulateRequestAsync("REQ-003", "Charlie"),
            };

            await Task.WhenAll(requests);

            Console.WriteLine("\n💡 三个并发请求的 TraceId/User 完全独立，日志中每条记录都自动携带了上下文。\n");
            Console.WriteLine("   这就是 ASP.NET Core 中 ILogger.BeginScope、Activity、HttpContext 的底层逻辑！\n");
        }

        private static async Task SimulateRequestAsync(string traceId, string userName)
        {
            // 中间件层：设置请求上下文
            _traceId.Value = traceId;
            _userName.Value = userName;

            Log("请求开始 → 进入控制器");

            await Task.Delay(5);  // 模拟网络 I/O（可能切换线程）

            Log("调用 Service 层");
            await CallServiceAsync();

            Log("请求结束 → 返回响应");
        }

        private static async Task CallServiceAsync()
        {
            // Service 层不需要传参，直接从 AsyncLocal 取上下文
            Log("  Service: 开始执行业务逻辑");

            await Task.Delay(5);

            Log("  Service: 调用数据库");
            await CallRepositoryAsync();

            Log("  Service: 业务逻辑完成");
        }

        private static async Task CallRepositoryAsync()
        {
            Log("    Repository: 执行 SQL 查询");
            await Task.Delay(5);
            Log("    Repository: 查询完成");
        }
    }
}
