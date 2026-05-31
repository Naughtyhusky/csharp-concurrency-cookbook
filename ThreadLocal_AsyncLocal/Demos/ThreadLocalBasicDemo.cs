using System.Text;

namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 02：ThreadLocal&lt;T&gt; 的基础用法
    ///
    /// ThreadLocal&lt;T&gt; 是 .NET 4.0 引入的线程本地存储的"正式版本"。
    /// 相比 [ThreadStatic]，它最大的优势就是：支持工厂函数初始化，
    /// 每个线程第一次访问 .Value 时，工厂函数就会被调用一次，确保正确初始化。
    /// </summary>
    public static class ThreadLocalBasicDemo
    {
        // ✅ 通过工厂函数初始化，每个线程第一次访问 .Value 时都会执行 () => new StringBuilder()
        private static readonly ThreadLocal<StringBuilder> _perThreadSB =
            new(() => new StringBuilder());

        // 一个带有线程 ID 标记的计数器，方便观察每个线程独立维护自己的计数
        private static readonly ThreadLocal<int> _perThreadCounter =
            new(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"  [线程 {tid}] 首次访问，初始化 _perThreadCounter = 0");
                return 0;
            });

        public static void Run()
        {
            Console.WriteLine("=== Demo 02：ThreadLocal<T> 基础用法 ===\n");

            var threads = Enumerable.Range(1, 3).Select(i => new Thread(() =>
            {
                var tid = Thread.CurrentThread.ManagedThreadId;

                // 第一次访问 .Value 时工厂函数才执行（懒加载）
                var sb = _perThreadSB.Value!;
                sb.Append($"来自线程{tid}的消息 | ");

                // 演示计数器独立性
                // 注意：ThreadLocal<int> 的 Value 是值类型，不能 ++ 后直接生效，需要重新赋值
                for (int j = 0; j < 3; j++)
                {
                    _perThreadCounter.Value++;  // 这里实际上每次都是读取本地副本、+1后写回
                }

                Console.WriteLine($"[线程 {tid}] StringBuilder = \"{sb}\"，Counter = {_perThreadCounter.Value}");
            })).ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            // 主线程的 _perThreadSB 是独立的，子线程的内容不会出现在这里
            Console.WriteLine($"\n主线程自己的 StringBuilder = \"{_perThreadSB.Value}\"（空的，因为主线程没有追加过内容）");

            // 用完要释放！ThreadLocal 实现了 IDisposable
            _perThreadSB.Dispose();
            _perThreadCounter.Dispose();

            Console.WriteLine("\n💡 ThreadLocal<T> 的工厂函数让每个线程都能安全地拥有独立实例，无需加锁。\n");
        }
    }
}
