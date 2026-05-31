namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 04：ThreadLocal&lt;T&gt; 在线程池中的"复用陷阱"
    ///
    /// 线程池里的线程是"复用"的——一个线程完成任务后不会死，
    /// 而是被放回池子，下次有新任务时再拿出来用。
    ///
    /// 问题在于：ThreadLocal 的值和线程绑定，如果线程被复用，
    /// 上一个任务留下的 ThreadLocal 值就会"污染"下一个任务！
    /// </summary>
    public static class ThreadLocalThreadPoolDemo
    {
        private static readonly ThreadLocal<string?> _requestContext = new(() => null);

        public static void Run()
        {
            Console.WriteLine("=== Demo 04：ThreadLocal 在线程池中的复用陷阱 ===\n");

            Console.WriteLine("--- ❌ 危险写法：不清理 ThreadLocal，可能被下一个任务读到脏数据 ---");

            // 第一批任务：设置了 ThreadLocal 但故意不清理
            var task1 = Task.Run(() =>
            {
                _requestContext.Value = "RequestId=AAA";
                var tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"[线程 {tid}] Task1 设置了 RequestContext = {_requestContext.Value}，但不清理...");
                // 故意不清理
            });
            task1.Wait();

            // 第二批任务：如果运气不好跑在同一个线程上，会拿到 Task1 留下的脏数据
            var task2 = Task.Run(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                // 如果这个线程就是刚才跑 Task1 的线程，_requestContext.Value 可能还是 "RequestId=AAA"
                Console.WriteLine($"[线程 {tid}] Task2 开始，发现 RequestContext = '{_requestContext.Value ?? "null"}'");
                if (_requestContext.Value != null)
                    Console.WriteLine($"  ⚠️  脏数据！Task2 本不应该知道任何 RequestId，但拿到了上个任务的值！");
                else
                    Console.WriteLine($"  ✅ 这次幸运，分到了不同的线程，没有脏数据（但不能依赖运气！）");
            });
            task2.Wait();

            Console.WriteLine("\n--- ✅ 正确写法：用 try/finally 保证清理 ---");

            var task3 = Task.Run(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                try
                {
                    _requestContext.Value = "RequestId=BBB";
                    Console.WriteLine($"[线程 {tid}] Task3 处理中，RequestContext = {_requestContext.Value}");
                    // ... 处理业务逻辑 ...
                }
                finally
                {
                    _requestContext.Value = null;  // ✅ 清理！无论异常与否都执行
                    Console.WriteLine($"[线程 {tid}] Task3 完成，已清理 RequestContext");
                }
            });
            task3.Wait();

            var task4 = Task.Run(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"[线程 {tid}] Task4 开始，RequestContext = '{_requestContext.Value ?? "null"}'（干净的！）");
            });
            task4.Wait();

            Console.WriteLine("\n💡 在线程池场景下使用 ThreadLocal，必须在任务结束时清理，否则会污染下一个任务！\n");
        }
    }
}
