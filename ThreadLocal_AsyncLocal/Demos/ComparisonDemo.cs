namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 09：ThreadLocal vs AsyncLocal 场景对比
    ///
    /// 通过一个对比实验，直观展示两者在"异步场景"下的本质区别：
    ///
    /// ThreadLocal：跟踪的是"线程"，await 后可能跑在另一个线程上，值就"找不到了"
    /// AsyncLocal ：跟踪的是"逻辑调用链（ExecutionContext）"，await 后值依然在
    /// </summary>
    public static class ComparisonDemo
    {
        private static readonly ThreadLocal<string?> _threadLocal = new(() => null);
        private static readonly AsyncLocal<string?> _asyncLocal = new();

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Demo 09：ThreadLocal vs AsyncLocal 对比实验 ===\n");

            Console.WriteLine("实验：在异步方法中设置值，await 后检查值是否还在\n");

            // 场景1：ThreadLocal 在异步中的问题
            Console.WriteLine("--- ❌ ThreadLocal 在异步方法中 ---");
            await TestThreadLocalAsync();

            // 场景2：AsyncLocal 正确处理异步
            Console.WriteLine("\n--- ✅ AsyncLocal 在异步方法中 ---");
            await TestAsyncLocalAsync();

            // 总结对比表
            Console.WriteLine("\n📊 ThreadLocal vs AsyncLocal 选型指南：");
            Console.WriteLine("┌─────────────────┬──────────────────────────┬──────────────────────────┐");
            Console.WriteLine("│ 特性            │ ThreadLocal<T>           │ AsyncLocal<T>            │");
            Console.WriteLine("├─────────────────┼──────────────────────────┼──────────────────────────┤");
            Console.WriteLine("│ 绑定对象        │ 线程（Thread）           │ 执行上下文（Execution）  │");
            Console.WriteLine("│ await 后值保持  │ ❌ 可能丢失              │ ✅ 保持                  │");
            Console.WriteLine("│ 子任务可见      │ ❌ 不可见                │ ✅ 可继承                │");
            Console.WriteLine("│ 子任务修改隔离  │ N/A                      │ ✅ 写时复制，隔离安全    │");
            Console.WriteLine("│ 线程池复用问题  │ ⚠️  需手动清理           │ ✅ 自动隔离              │");
            Console.WriteLine("│ 适用场景        │ CPU密集型多线程           │ 异步调用链上下文         │");
            Console.WriteLine("│ 典型用例        │ Random/StringBuilder缓存  │ TraceId/UserId/Tenant    │");
            Console.WriteLine("│ IDisposable     │ ✅ 需要手动 Dispose      │ ❌ 不需要                │");
            Console.WriteLine("└─────────────────┴──────────────────────────┴──────────────────────────┘\n");
        }

        private static async Task TestThreadLocalAsync()
        {
            _threadLocal.Value = "Hello-ThreadLocal";
            var tid1 = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"  await 前：[线程 {tid1}] ThreadLocal = '{_threadLocal.Value}'");

            // ConfigureAwait(false) 故意让 await 可能切换到另一个线程
            await Task.Delay(1).ConfigureAwait(false);

            var tid2 = Thread.CurrentThread.ManagedThreadId;
            var value = _threadLocal.Value;
            if (tid1 != tid2)
                Console.WriteLine($"  await 后：[线程 {tid2}] ThreadLocal = '{value ?? "null"}'  ← 线程切换了，值可能消失！");
            else
                Console.WriteLine($"  await 后：[线程 {tid2}] ThreadLocal = '{value}'  ← 这次运气好，线程没切换，但不能依赖这个！");
        }

        private static async Task TestAsyncLocalAsync()
        {
            _asyncLocal.Value = "Hello-AsyncLocal";
            var tid1 = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"  await 前：[线程 {tid1}] AsyncLocal = '{_asyncLocal.Value}'");

            await Task.Delay(1).ConfigureAwait(false);

            var tid2 = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"  await 后：[线程 {tid2}] AsyncLocal = '{_asyncLocal.Value}'  ← 无论线程是否切换，值始终在！");
        }
    }
}
