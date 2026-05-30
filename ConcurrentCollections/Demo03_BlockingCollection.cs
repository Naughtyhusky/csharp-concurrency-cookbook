using System.Collections.Concurrent;

namespace ConcurrentCollections;

/// <summary>
/// 示例03：BlockingCollection - 有界生产者-消费者（同步阻塞版）
/// </summary>
public static class Demo03_BlockingCollection
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Demo03：BlockingCollection（同步阻塞生产消费） ===\n");

        await SingleProducerSingleConsumer();
        await MultiProducerMultiConsumer();
    }

    // ---------------------------------------------------------------
    // 1. 单生产者 / 单消费者
    // ---------------------------------------------------------------
    static async Task SingleProducerSingleConsumer()
    {
        Console.WriteLine("--- 1. 单生产者 / 单消费者 ---");

        // 容量上限 10，生产超过 10 个时生产者会阻塞等待消费者消费
        using var bc = new BlockingCollection<string>(boundedCapacity: 10);

        var producer = Task.Run(() =>
        {
            for (int i = 1; i <= 20; i++)
            {
                string item = $"item-{i:D2}";
                bc.Add(item); // 满了就阻塞
                Console.WriteLine($"  [生产] {item}");
                Thread.Sleep(20); // 模拟生产耗时
            }
            bc.CompleteAdding(); // ← 告诉消费者：我不会再加东西了
            Console.WriteLine("  [生产] 已完成");
        });

        var consumer = Task.Run(() =>
        {
            // GetConsumingEnumerable：自动阻塞等待，直到 CompleteAdding 且集合为空
            foreach (string item in bc.GetConsumingEnumerable())
            {
                Console.WriteLine($"  [消费] {item}");
                Thread.Sleep(50); // 模拟消费耗时（比生产慢，触发背压）
            }
            Console.WriteLine("  [消费] 已完成");
        });

        await Task.WhenAll(producer, consumer);
        Console.WriteLine();
    }

    // ---------------------------------------------------------------
    // 2. 多生产者 / 多消费者（展示优雅关闭）
    // ---------------------------------------------------------------
    static async Task MultiProducerMultiConsumer()
    {
        Console.WriteLine("--- 2. 多生产者 / 多消费者 ---");

        using var bc = new BlockingCollection<int>(boundedCapacity: 20);
        int totalConsumed = 0;

        // 3 个生产者
        var producers = Enumerable.Range(0, 3).Select(i => Task.Run(() =>
        {
            for (int j = 0; j < 10; j++)
            {
                bc.Add(i * 100 + j);
            }
            Console.WriteLine($"  [生产者{i}] 完成");
        })).ToArray();

        // 等所有生产者完成后，标记 CompleteAdding
        // ✅ 用 Task.Run + await 显式协调，而不是 fire-and-forget 的 ContinueWith
        //    ContinueWith 的异常默认被吞掉，且 ConfigureAwait 行为不可控
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(producers);
            }
            finally
            {
                // BlockingCollection 用 CompleteAdding（不支持传递异常，与 Channel 不同）
                bc.CompleteAdding();
                Console.WriteLine("  [协调] CompleteAdding 已调用");
            }
        });

        // 2 个消费者
        var consumers = Enumerable.Range(0, 2).Select(i => Task.Run(() =>
        {
            int count = 0;
            foreach (int item in bc.GetConsumingEnumerable())
            {
                count++;
                Thread.Sleep(5);
            }
            Interlocked.Add(ref totalConsumed, count);
            Console.WriteLine($"  [消费者{i}] 消费了 {count} 个");
        })).ToArray();

        await Task.WhenAll(consumers);
        Console.WriteLine($"  总计消费: {totalConsumed} 个（预期 30 个）\n");
    }
}
