// ========================================
// 工作窃取算法演示（简化模拟）
// ========================================

using System.Collections.Concurrent;

namespace Threads;

/// <summary>
/// 简化模拟工作窃取算法
/// </summary>
internal static class WorkStealingDemo
{
    public static void Run()
    {
        Console.WriteLine("这是工作窃取算法的简化演示\n");
        Console.WriteLine("说明：");
        Console.WriteLine("  - 每个线程有本地队列（LIFO）");
        Console.WriteLine("  - 本地队列空时，从全局队列或其他线程的队列\"偷\"任务（FIFO）");
        Console.WriteLine("  - 减少全局队列竞争，提高性能\n");

        var queue = new WorkStealingQueue();

        // 模拟：线程 1 添加任务
        Console.WriteLine("【线程 1】添加 5 个任务到本地队列");
        for (int i = 1; i <= 5; i++)
        {
            queue.Enqueue($"Task-{i}");
            Console.WriteLine($"  入队：Task-{i}");
        }

        Console.WriteLine();

        // 模拟：线程 1 从本地队列取任务（LIFO）
        Console.WriteLine("【线程 1】从本地队列取任务（LIFO - 后进先出）");
        for (int i = 0; i < 3; i++)
        {
            if (queue.TryDequeue(out var task))
            {
                Console.WriteLine($"  出队（本线程）：{task}");
            }
        }

        Console.WriteLine();

        // 模拟：线程 2 "偷" 任务（FIFO）
        Console.WriteLine("【线程 2】本地队列空，尝试\"偷\"线程 1 的任务（FIFO - 先进先出）");
        for (int i = 0; i < 2; i++)
        {
            if (queue.TrySteal(out var task))
            {
                Console.WriteLine($"  窃取：{task}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("总结：");
        Console.WriteLine("  - 本线程从尾部取（LIFO）：Task-5, Task-4, Task-3");
        Console.WriteLine("  - 其他线程从头部偷（FIFO）：Task-1, Task-2");
        Console.WriteLine("  - 减少竞争：两端操作，冲突概率低");
        Console.WriteLine("  - 缓存友好：本线程刚放入的数据，CPU 缓存中可能还有");
    }

    /// <summary>
    /// 简化的工作窃取队列
    /// </summary>
    private class WorkStealingQueue
    {
        private readonly Queue<string> _localQueue = new();
        private readonly ConcurrentQueue<string> _globalQueue = new();

        public void Enqueue(string item)
        {
            // 模拟：放入本地队列
            _localQueue.Enqueue(item);

            // 如果本地队列过大，转移一部分到全局队列
            if (_localQueue.Count > 10)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (_localQueue.Count > 0)
                    {
                        _globalQueue.Enqueue(_localQueue.Dequeue());
                    }
                }
            }
        }

        /// <summary>
        /// 本线程取任务（LIFO - 从尾部）
        /// </summary>
        public bool TryDequeue(out string? item)
        {
            // 简化实现：真实的工作窃取队列使用双端队列（Deque）
            // 这里用 Queue 模拟，实际应该从尾部取

            if (_localQueue.Count > 0)
            {
                // 模拟从尾部取（LIFO）
                var temp = _localQueue.ToArray();
                item = temp[^1];  // 取最后一个
                _localQueue.Clear();
                for (int i = 0; i < temp.Length - 1; i++)
                {
                    _localQueue.Enqueue(temp[i]);
                }
                return true;
            }

            // 本地队列空，尝试从全局队列取
            return _globalQueue.TryDequeue(out item);
        }

        /// <summary>
        /// 其他线程"偷"任务（FIFO - 从头部）
        /// </summary>
        public bool TrySteal(out string? item)
        {
            // 从本地队列的头部偷（FIFO）
            if (_localQueue.Count > 0)
            {
                item = _localQueue.Dequeue();
                return true;
            }

            // 本地队列空，尝试从全局队列取
            return _globalQueue.TryDequeue(out item);
        }
    }
}
