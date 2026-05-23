using System.Threading.Channels;

namespace MemoryLeaks.Leaks;

/// <summary>
/// 泄漏场景 5：Channel 使用不当导致 Reader 永久挂起
///
/// Channel 是 .NET 推荐的生产者-消费者通信原语。
/// 但有几个常见的"软泄漏"场景：
///
/// 1. Writer 没有调用 Complete() → Reader 的 ReadAllAsync 永远等待
///    → 消费者 Task 永远不结束，持有所有关联资源
///
/// 2. Writer 抛出异常后没有 TryComplete(exception) → Reader 收不到错误，死等
///
/// 3. 无界 Channel（Unbounded）被生产者疯狂写入，消费者跟不上 → 无限堆积
/// </summary>
public static class ChannelLeakDemo
{
    // ❌ 反模式 1：Writer 忘记调用 Complete，Reader 永久挂起
    public static async Task LeakByForgettingComplete()
    {
        Console.WriteLine("\n  [泄漏] Writer 不调用 Complete，Reader 将永久等待...");

        var channel = Channel.CreateUnbounded<string>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 启动消费者（会永远等待）
        var consumerTask = Task.Run(async () =>
        {
            var count = 0;
            // ❌ 如果 Writer 不 Complete，这个循环永远不会结束
            await foreach (var item in reader.ReadAllAsync().ConfigureAwait(false))
            {
                count++;
                Console.WriteLine($"  [消费者] 收到: {item}（已消费 {count} 条）");
            }
            Console.WriteLine("  [消费者] ReadAllAsync 结束，消费者退出");
        });

        // 生产者写入几条消息后"消失"（忘记 Complete）
        await writer.WriteAsync("消息1").ConfigureAwait(false);
        await writer.WriteAsync("消息2").ConfigureAwait(false);
        await writer.WriteAsync("消息3").ConfigureAwait(false);

        // ❌ 没有调用 writer.Complete()
        Console.WriteLine("  [泄漏] 生产者已完成写入，但忘记调用 Complete()");
        Console.WriteLine("  [泄漏] 消费者的 ReadAllAsync 将永远挂在这里，持有所有资源...");

        // 等一下让消费者处理完已有消息
        await Task.Delay(200).ConfigureAwait(false);
        Console.WriteLine($"  [泄漏] consumerTask 状态: {consumerTask.Status}（应该是 Running，不是 Completed）");

        // 演示后手动补救
        writer.Complete();
        await consumerTask.ConfigureAwait(false);
        Console.WriteLine("  [修复] 补调 Complete() 后，消费者正常退出");
    }

    // ❌ 反模式 2：Writer 抛异常，没有传播给 Reader
    public static async Task LeakByExceptionNotPropagated()
    {
        Console.WriteLine("\n  [泄漏] Writer 抛异常，Reader 不知道，继续死等...");

        var channel = Channel.CreateUnbounded<int>();

        var consumerTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    Console.WriteLine($"  [消费者] 处理: {item}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [消费者] 收到异常通知: {ex.Message}");
            }
        });

        // 模拟生产者写几条后出错
        await channel.Writer.WriteAsync(1).ConfigureAwait(false);
        await channel.Writer.WriteAsync(2).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        try
        {
            throw new InvalidOperationException("生产者数据源崩了！");
        }
        catch (Exception ex)
        {
            // ✅ 正确：把异常传递给 Channel，Reader 会收到
            channel.Writer.TryComplete(ex);
            Console.WriteLine($"  [生产者] 出错，已通过 TryComplete 通知 Reader: {ex.Message}");
        }

        await consumerTask.ConfigureAwait(false);
    }

    // ❌ 反模式 3：无界 Channel，生产者比消费者快，消息无限堆积
    public static async Task LeakByUnboundedChannelOverflow()
    {
        Console.WriteLine("\n  [泄漏] 无界 Channel，生产快消费慢，消息堆积...");

        // ❌ Unbounded Channel：队列无上限，内存无上限
        var channel = Channel.CreateUnbounded<byte[]>();

        // 快速生产者：每 10ms 写入一个 100KB 的消息
        var producerTask = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                var bigMessage = new byte[100 * 1024]; // 100KB
                await channel.Writer.WriteAsync(bigMessage).ConfigureAwait(false);
                Console.WriteLine($"  [生产者] 写入消息 #{i + 1}（100KB），队列长度≈{channel.Reader.Count}");
                await Task.Delay(10).ConfigureAwait(false);
            }
            channel.Writer.Complete();
        });

        // 慢速消费者：每 100ms 处理一条
        var consumerTask = Task.Run(async () =>
        {
            await foreach (var msg in channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await Task.Delay(100).ConfigureAwait(false); // 故意慢
                Console.WriteLine($"  [消费者] 处理了一条，队列剩余≈{channel.Reader.Count}");
            }
        });

        await Task.WhenAll(producerTask, consumerTask).ConfigureAwait(false);
        Console.WriteLine("  [演示] 无界 Channel 在生产速度 >> 消费速度时会撑爆内存");
    }
}
