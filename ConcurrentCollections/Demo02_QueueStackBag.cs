using System.Collections.Concurrent;

namespace ConcurrentCollections;

/// <summary>
/// 示例02：ConcurrentQueue / ConcurrentStack / ConcurrentBag
/// </summary>
public static class Demo02_QueueStackBag
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Demo02：ConcurrentQueue / ConcurrentStack / ConcurrentBag ===\n");

        await ConcurrentQueueDemo();
        await ConcurrentStackDemo();
        await ConcurrentBagDemo();
    }

    // ---------------------------------------------------------------
    // 1. ConcurrentQueue：线程安全 FIFO 队列
    // ---------------------------------------------------------------
    static async Task ConcurrentQueueDemo()
    {
        Console.WriteLine("--- 1. ConcurrentQueue（FIFO） ---");

        var queue = new ConcurrentQueue<string>();

        // 多线程入队
        var producers = Enumerable.Range(0, 3).Select(i => Task.Run(() =>
        {
            for (int j = 0; j < 5; j++)
            {
                queue.Enqueue($"P{i}-item{j}");
            }
        })).ToArray();

        await Task.WhenAll(producers);
        Console.WriteLine($"入队完毕，队列长度: {queue.Count}");

        // 多线程出队
        int consumed = 0;
        var consumers = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            while (queue.TryDequeue(out string? item))
            {
                Interlocked.Increment(ref consumed);
            }
        })).ToArray();

        await Task.WhenAll(consumers);
        Console.WriteLine($"出队完毕，共消费: {consumed} 个，剩余: {queue.Count} 个\n");
    }

    // ---------------------------------------------------------------
    // 2. ConcurrentStack：线程安全 LIFO 栈
    // ---------------------------------------------------------------
    static Task ConcurrentStackDemo()
    {
        Console.WriteLine("--- 2. ConcurrentStack（LIFO） ---");

        var stack = new ConcurrentStack<int>();

        // 批量入栈
        stack.PushRange([1, 2, 3, 4, 5]);
        Console.WriteLine($"PushRange [1~5]，栈大小: {stack.Count}");

        // 逐个出栈（后进先出）
        Console.Write("出栈顺序: ");
        while (stack.TryPop(out int top))
        {
            Console.Write($"{top} ");
        }
        Console.WriteLine("\n");

        return Task.CompletedTask;
    }

    // ---------------------------------------------------------------
    // 3. ConcurrentBag：自产自销 vs 跨线程对比
    // ---------------------------------------------------------------
    static async Task ConcurrentBagDemo()
    {
        Console.WriteLine("--- 3. ConcurrentBag（无序包，适合自产自销） ---");

        var bag = new ConcurrentBag<int>();

        // 适合场景：同一线程既 Add 又 TryTake（线程本地列表，几乎无竞争）
        var tasks = Enumerable.Range(0, 4).Select(threadId => Task.Run(() =>
        {
            // 每个线程先加一批
            for (int i = 0; i < 5; i++)
                bag.Add(threadId * 100 + i);

            // 再从中取几个（优先从自己的本地列表取）
            int taken = 0;
            while (taken < 3 && bag.TryTake(out _))
                taken++;

            Console.WriteLine($"  线程{threadId}: 添加5个，消费3个");
        })).ToArray();

        await Task.WhenAll(tasks);
        Console.WriteLine($"最终 Bag 中剩余: {bag.Count} 个（4线程各剩2个）\n");
    }
}
