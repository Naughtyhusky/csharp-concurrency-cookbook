using System.Threading.Channels;

namespace MemoryLeaks.Fixes;

/// <summary>
/// 修复方案 5：Channel 的正确使用姿势
/// </summary>
public static class ChannelFixDemo
{
    // ✅ 修复 1：try-finally 确保 Writer.Complete() 一定被调用
    public static async Task FixWithTryFinally()
    {
        Console.WriteLine("\n  [修复] try-finally 确保 Writer.Complete() 被调用...");

        var channel = Channel.CreateUnbounded<string>();

        var consumerTask = Task.Run(async () =>
        {
            await foreach (var item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                Console.WriteLine($"  [消费者] 收到: {item}");
            }
            Console.WriteLine("  [消费者] ReadAllAsync 正常结束，退出");
        });

        try
        {
            await channel.Writer.WriteAsync("消息1").ConfigureAwait(false);
            await channel.Writer.WriteAsync("消息2").ConfigureAwait(false);
            await channel.Writer.WriteAsync("消息3").ConfigureAwait(false);
            // 模拟可能出错的操作
        }
        finally
        {
            // ✅ 无论成功还是异常，finally 确保 Complete 一定被调用
            channel.Writer.TryComplete();
            Console.WriteLine("  [修复] Writer.Complete() 在 finally 中调用，消费者可以正常退出");
        }

        await consumerTask.ConfigureAwait(false);
    }

    // ✅ 修复 2：异常时用 TryComplete(exception) 通知 Reader
    public static async Task FixWithExceptionPropagation()
    {
        Console.WriteLine("\n  [修复] 异常通过 TryComplete(ex) 传播给 Reader...");

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
            catch (InvalidOperationException ex)
            {
                // ✅ 能捕获到生产者传来的异常
                Console.WriteLine($"  [消费者] 收到异常通知，优雅退出: {ex.Message}");
            }
        });

        try
        {
            await channel.Writer.WriteAsync(1).ConfigureAwait(false);
            await channel.Writer.WriteAsync(2).ConfigureAwait(false);
            await Task.Delay(50).ConfigureAwait(false);
            throw new InvalidOperationException("数据源崩了！");
        }
        catch (Exception ex)
        {
            // ✅ 把异常传递给 Channel，Reader 的 ReadAllAsync 会重新抛出
            channel.Writer.TryComplete(ex);
        }

        await consumerTask.ConfigureAwait(false);
    }

    // ✅ 修复 3：用有界 Channel 防止无限堆积（背压机制）
    public static async Task FixWithBoundedChannel()
    {
        Console.WriteLine("\n  [修复] 有界 Channel，生产过快时自动背压...");

        // ✅ 设置容量上限为 5，FullMode 为 Wait（生产者等待，直到消费者消费）
        var options = new BoundedChannelOptions(5)
        {
            FullMode = BoundedChannelFullMode.Wait, // 队列满时，WriteAsync 会等待
            SingleReader = false,
            SingleWriter = false
        };
        var channel = Channel.CreateBounded<byte[]>(options);

        var producerTask = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                var msg = new byte[100 * 1024]; // 100KB
                // ✅ 队列满时，这里会自动等待，不会无限堆积
                await channel.Writer.WriteAsync(msg).ConfigureAwait(false);
                Console.WriteLine($"  [生产者] 写入 #{i + 1}，当前队列: {channel.Reader.Count}");
            }
            channel.Writer.Complete();
        });

        var consumerTask = Task.Run(async () =>
        {
            await foreach (var msg in channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await Task.Delay(80).ConfigureAwait(false); // 慢消费
                Console.WriteLine($"  [消费者] 消费完毕，队列剩余: {channel.Reader.Count}");
            }
        });

        await Task.WhenAll(producerTask, consumerTask).ConfigureAwait(false);
        Console.WriteLine("  [修复] 有界 Channel 通过背压机制，内存占用始终可控");
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== Channel 泄漏修复方案 ===");
        await FixWithTryFinally();
        await FixWithExceptionPropagation();
        await FixWithBoundedChannel();
    }
}
