namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 08：ThreadLocal&lt;T&gt; 实战 —— 线程安全的 Random
    ///
    /// System.Random 不是线程安全的！多线程共享一个 Random 实例，
    /// 在 .NET Framework 时代会导致 Random 内部状态被破坏，
    /// 所有后续调用都只返回 0（经典 bug）。
    ///
    /// 解决方案1：每个线程一个 Random 实例（ThreadLocal）
    /// 注意：.NET 6+ 以后，Random.Shared 是线程安全的，但理解 ThreadLocal 的用法依然重要。
    /// </summary>
    public static class ThreadLocalRandomDemo
    {
        // 每个线程一个 Random 实例，用线程 ID 做种子确保各线程结果不同
        private static readonly ThreadLocal<Random> _random =
            new(() => new Random(Thread.CurrentThread.ManagedThreadId * 31 + Environment.TickCount));

        public static void Run()
        {
            Console.WriteLine("=== Demo 08：ThreadLocal<Random> —— 线程安全的随机数 ===\n");

            Console.WriteLine("--- 方式1：ThreadLocal<Random>（.NET 所有版本适用）---");
            var results1 = new System.Collections.Concurrent.ConcurrentBag<(int tid, int value)>();

            var threads = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                var value = _random.Value!.Next(1, 100);
                results1.Add((tid, value));
            })).ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            foreach (var (tid, value) in results1.OrderBy(x => x.tid))
                Console.WriteLine($"  [线程 {tid}] 生成随机数 = {value}");

            Console.WriteLine("\n--- 方式2：Random.Shared（.NET 6+ 推荐，官方线程安全实现）---");
            var results2 = new System.Collections.Concurrent.ConcurrentBag<(int tid, int value)>();

            var threads2 = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                var value = Random.Shared.Next(1, 100);  // .NET 6+ 线程安全
                results2.Add((tid, value));
            })).ToList();

            threads2.ForEach(t => t.Start());
            threads2.ForEach(t => t.Join());

            foreach (var (tid, value) in results2.OrderBy(x => x.tid))
                Console.WriteLine($"  [线程 {tid}] 生成随机数 = {value}");

            _random.Dispose();

            Console.WriteLine("\n💡 .NET 6+ 优先用 Random.Shared；低版本项目用 ThreadLocal<Random> 是标准解法。\n");
        }
    }
}
