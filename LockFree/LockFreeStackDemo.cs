namespace LockFree;

/// <summary>
/// 无锁数据结构示例
/// 演示：用 CAS 实现无锁栈（Treiber Stack）
/// </summary>
public static class LockFreeStackDemo
{
    // ---- 无锁栈实现（Treiber Stack 算法，1986年提出，至今仍是经典）----

    /// <summary>
    /// 无锁栈，使用 CAS 实现线程安全的 Push/Pop
    /// </summary>
    public class LockFreeStack<T>
    {
        private sealed class Node(T value)
        {
            public readonly T Value = value;
            public Node? Next;
        }

        private volatile Node? _head;
        private int _count;

        public int Count => Volatile.Read(ref _count);

        /// <summary>
        /// 无锁入栈：
        /// 1. 创建新节点，让新节点的 Next 指向当前头节点
        /// 2. CAS 把头节点换成新节点
        /// 3. 失败就重读头节点，重试
        /// </summary>
        public void Push(T value)
        {
            var newNode = new Node(value);
            while (true)
            {
                newNode.Next = _head; // 新节点指向当前头
                // 如果 _head 还是 newNode.Next（没有其他线程改过），就把 _head 换成 newNode
                if (Interlocked.CompareExchange(ref _head, newNode, newNode.Next) == newNode.Next)
                {
                    Interlocked.Increment(ref _count);
                    return; // CAS 成功，入栈完成
                }
                // 否则有人抢先修改了 _head，重新尝试
            }
        }

        /// <summary>
        /// 无锁出栈：
        /// 1. 读取当前头节点
        /// 2. CAS 把头节点换成 head.Next
        /// 3. 失败就重读，重试
        /// </summary>
        public bool TryPop(out T? value)
        {
            while (true)
            {
                Node? head = _head;
                if (head is null)
                {
                    value = default;
                    return false; // 栈为空
                }

                if (Interlocked.CompareExchange(ref _head, head.Next, head) == head)
                {
                    value = head.Value;
                    Interlocked.Decrement(ref _count);
                    return true; // CAS 成功，出栈完成
                }
                // 有人抢先，重试
            }
        }
    }

    // ---- 验证无锁栈的线程安全性 ----

    public static void RunStackCorrectnessDemo()
    {
        Console.WriteLine("=== 示例1：无锁栈（Treiber Stack）线程安全验证 ===\n");

        var stack = new LockFreeStack<int>();
        const int threadCount = 8;
        const int opsPerThread = 10_000;

        // 多线程并发 Push
        var pushTasks = Enumerable.Range(0, threadCount)
            .Select(t => Task.Run(() =>
            {
                for (int i = 0; i < opsPerThread; i++)
                    stack.Push(t * opsPerThread + i);
            }))
            .ToArray();
        Task.WaitAll(pushTasks);

        Console.WriteLine($"  {threadCount} 个线程，每个 Push {opsPerThread} 次");
        Console.WriteLine($"  期望栈大小：{threadCount * opsPerThread:N0}，实际：{stack.Count:N0}");

        // 多线程并发 Pop
        int popCount = 0;
        var popTasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                int local = 0;
                while (stack.TryPop(out _))
                    local++;
                Interlocked.Add(ref popCount, local);
            }))
            .ToArray();
        Task.WaitAll(popTasks);

        Console.WriteLine($"  并发 Pop 后，共弹出 {popCount:N0} 个元素，剩余 {stack.Count:N0}");
        Console.WriteLine($"  {(popCount == threadCount * opsPerThread ? "✅ 完全正确" : "❌ 有数据丢失！")}\n");
    }

    // ---- 性能对比：无锁栈 vs ConcurrentStack（系统内置）----

    public static void RunPerformanceComparison()
    {
        Console.WriteLine("=== 示例2：无锁栈 vs ConcurrentStack 性能对比 ===\n");

        const int ops = 1_000_000;
        var lockFreeStack = new LockFreeStack<int>();
        var concurrentStack = new System.Collections.Concurrent.ConcurrentStack<int>();

        // 预热
        for (int i = 0; i < 1000; i++) { lockFreeStack.Push(i); concurrentStack.Push(i); }
        while (lockFreeStack.TryPop(out _)) { }
        concurrentStack.Clear();

        // 测试无锁栈（单线程）
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < ops; i++) lockFreeStack.Push(i);
        sw.Stop();
        Console.WriteLine($"  LockFreeStack  Push {ops:N0} 次：{sw.ElapsedMilliseconds}ms");

        // 测试 ConcurrentStack（单线程）
        sw.Restart();
        for (int i = 0; i < ops; i++) concurrentStack.Push(i);
        sw.Stop();
        Console.WriteLine($"  ConcurrentStack Push {ops:N0} 次：{sw.ElapsedMilliseconds}ms");

        Console.WriteLine("  （ConcurrentStack 内部也是无锁实现，性能接近）\n");
    }

    public static void RunAll()
    {
        RunStackCorrectnessDemo();
        RunPerformanceComparison();
    }
}
