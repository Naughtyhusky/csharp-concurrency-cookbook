namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 03：ThreadLocal&lt;T&gt; 追踪所有线程的值
    ///
    /// ThreadLocal&lt;T&gt; 有一个鲜为人知的特性：当构造时传入 trackAllValues: true，
    /// 可以通过 .Values 属性拿到所有线程当前的副本值的快照。
    /// 这在"需要聚合所有线程的统计数据"时非常有用，比如无锁的多线程计数器。
    /// </summary>
    public static class ThreadLocalTrackingDemo
    {
        public static void Run()
        {
            Console.WriteLine("=== Demo 03：ThreadLocal<T> 追踪所有线程的值 ===\n");

            // trackAllValues: true —— 开启追踪，可以通过 .Values 拿到所有线程的值
            using var perThreadCount = new ThreadLocal<int>(
                valueFactory: () => 0,
                trackAllValues: true);

            var threads = Enumerable.Range(1, 5).Select(i => new Thread(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                // 每个线程自己计数（假设执行了 i * 10 次操作）
                perThreadCount.Value = i * 10;
                Console.WriteLine($"[线程 {tid}] 完成 {perThreadCount.Value} 次操作");
            })).ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            // 聚合：把所有线程的计数加起来，得到全局总数
            // 注意：.Values 返回的是"快照"，只包含还存活的线程（或已死亡但值已记录的线程）的值
            var total = perThreadCount.Values.Sum();
            Console.WriteLine($"\n✅ 所有线程合计操作次数 = {total}（各线程独立计数，无锁！）");
            Console.WriteLine($"   各线程值快照：[{string.Join(", ", perThreadCount.Values)}]");

            Console.WriteLine("\n💡 trackAllValues: true 是实现无锁多线程统计的利器，比全局锁计数器性能更好。\n");
        }
    }
}
