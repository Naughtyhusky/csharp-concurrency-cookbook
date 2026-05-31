namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 01：[ThreadStatic] 特性的基础用法与陷阱
    ///
    /// [ThreadStatic] 是最古老的线程本地存储方式，它让每个线程都拥有一份独立的静态字段副本。
    /// 但它有一个经典陷阱：静态字段的"初始化表达式"只会在第一个线程（通常是主线程）上执行，
    /// 其他线程拿到的是默认值（int 是 0，引用类型是 null）。
    /// </summary>
    public static class ThreadStaticDemo
    {
        // ❌ 陷阱：= 10 这个初始化只会在"声明它的线程"上生效
        [ThreadStatic]
        private static int _counter = 10;

        // ✅ 正确做法：不在声明时初始化，在线程开始时手动赋初值
        [ThreadStatic]
        private static int _safeCounter;

        public static void Run()
        {
            Console.WriteLine("=== Demo 01：[ThreadStatic] 的用法与陷阱 ===\n");

            // 主线程中，_counter 确实是 10（因为静态初始化在主线程执行）
            Console.WriteLine($"主线程 _counter = {_counter}");  // 输出 10

            var t1 = new Thread(() =>
            {
                // 子线程拿到的是 0，而不是 10！
                Console.WriteLine($"子线程1 _counter（声明初始化的陷阱）= {_counter}");

                // 手动初始化才是正确的做法
                _safeCounter = 100;
                _counter = 999;  // 这里改的是"子线程1自己的副本"
                Console.WriteLine($"子线程1 修改后 _counter = {_counter}");
            });

            var t2 = new Thread(() =>
            {
                // 每个线程都有自己独立的副本，互不干扰
                _safeCounter = 200;
                Console.WriteLine($"子线程2 _counter = {_counter}");        // 不受子线程1影响，还是 0
                Console.WriteLine($"子线程2 _safeCounter = {_safeCounter}");// 自己设置的 200
            });

            t1.Start(); t1.Join();
            t2.Start(); t2.Join();

            // 主线程的 _counter 不受任何子线程影响
            Console.WriteLine($"主线程 _counter（子线程修改后）= {_counter}");
            Console.WriteLine($"\n💡 结论：[ThreadStatic] 字段不要在声明时赋初值，每个线程要手动初始化！\n");
        }
    }
}
