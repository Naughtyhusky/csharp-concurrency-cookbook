using System.Threading.Channels;

namespace ConcurrentCollections;

/// <summary>
/// 示例04：Channel&lt;T&gt; - 现代异步生产者-消费者
/// </summary>
public static class Demo04_Channel
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Demo04：Channel<T>（异步生产消费，现代首选） ===\n");

        await BasicChannelDemo();
        await BoundedChannelDemo();
        await AsyncLogPipelineDemo();
    }

    // ---------------------------------------------------------------
    // 1. 基础用法
    //    重点：生产者必须用 try/finally 确保 Complete 一定被调用。
    //    若生产者抛异常却没有 Complete，消费者的 ReadAllAsync 将永久挂起。
    // ---------------------------------------------------------------
    static async Task BasicChannelDemo()
    {
        Console.WriteLine("--- 1. 基础用法（无界 Channel） ---");

        var channel = Channel.CreateUnbounded<string>();

        var producer = Task.Run(async () =>
        {
            Exception? fault = null;
            try
            {
                for (int i = 1; i <= 5; i++)
                {
                    await channel.Writer.WriteAsync($"message-{i}");
                    Console.WriteLine($"  [写入] message-{i}");
                    await Task.Delay(30);
                }
            }
            catch (Exception ex)
            {
                fault = ex;
                throw;
            }
            finally
            {
                // ✅ 无论正常结束还是异常退出，都必须 Complete
                // TryComplete(fault)：正常时 fault=null 等同于 Complete()
                //                    异常时把异常传递给消费者，让 ReadAllAsync 重新抛出
                channel.Writer.TryComplete(fault);
            }
        });

        // 消费者：ReadAllAsync 自动等待，直到 Writer Complete（或收到传播来的异常）
        var consumer = Task.Run(async () =>
        {
            await foreach (string msg in channel.Reader.ReadAllAsync())
            {
                Console.WriteLine($"  [读取] {msg}");
                await Task.Delay(50);
            }
        });

        await Task.WhenAll(producer, consumer);
        Console.WriteLine();
    }

    // ---------------------------------------------------------------
    // 2. 有界 Channel + 背压策略
    //    重点：CancellationToken 要传入 WriteAsync/ReadAllAsync，
    //    取消时 finally 保证 Complete 被调用，消费者不会泄漏。
    // ---------------------------------------------------------------
    static async Task BoundedChannelDemo()
    {
        Console.WriteLine("--- 2. 有界 Channel（容量=3，展示背压效果） ---");

        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(capacity: 3)
        {
            FullMode = BoundedChannelFullMode.Wait // 满了生产者等待（背压）
        });

        using var cts = new CancellationTokenSource();

        // 快速生产者（每 20ms 产一个）
        var producer = Task.Run(async () =>
        {
            Exception? fault = null;
            try
            {
                for (int i = 1; i <= 10; i++)
                {
                    // ✅ 传入 CancellationToken：取消时 WriteAsync 会抛 OperationCanceledException
                    await channel.Writer.WriteAsync(i, cts.Token);
                    Console.WriteLine($"  [生产] item-{i:D2}  (Reader.Count≈{channel.Reader.Count})");
                    await Task.Delay(20, cts.Token);
                }
            }
            catch (OperationCanceledException ex)
            {
                fault = ex;
                throw;
            }
            catch (Exception ex)
            {
                fault = ex;
                throw;
            }
            finally
            {
                // ✅ 取消或异常时，把原因传给消费者，消费者的 ReadAllAsync 会感知到并退出
                channel.Writer.TryComplete(fault);
            }
        });

        // 慢速消费者（每 80ms 消费一个）
        var consumer = Task.Run(async () =>
        {
            // ✅ 传入 CancellationToken：取消时消费者也能及时退出，不会继续空等
            await foreach (int item in channel.Reader.ReadAllAsync(cts.Token))
            {
                Console.WriteLine($"  [消费] item-{item:D2}");
                await Task.Delay(80, cts.Token); // 比生产者慢 4 倍，Channel 反压生产者
            }
        });

        await Task.WhenAll(producer, consumer);
        Console.WriteLine();
    }

    // ---------------------------------------------------------------
    // 3. 实战：异步日志管道（多写 → Channel → 单读）
    //    重点：
    //    - 多个生产者时，需要等所有生产者都完成后再 Complete，
    //      用 WhenAll + try/finally 明确协调，而不是 fire-and-forget 的 ContinueWith
    //    - 任意生产者异常，都通过 TryComplete(fault) 通知消费者
    // ---------------------------------------------------------------
    static async Task AsyncLogPipelineDemo()
    {
        Console.WriteLine("--- 3. 实战：异步日志管道（多写 → Channel → 单读） ---");

        var logChannel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropWrite // 日志满了丢弃新写入，不阻塞业务
        });

        int written = 0;
        int dropped = 0;

        void WriteLog(string level, string message)
        {
            var entry = new LogEntry(DateTime.Now, level, message);
            if (!logChannel.Writer.TryWrite(entry))
                Interlocked.Increment(ref dropped);
        }

        // 5 个业务任务并发写日志
        var businessTasks = Enumerable.Range(0, 5).Select(threadId => Task.Run(async () =>
        {
            for (int i = 0; i < 30; i++)
            {
                string level = i % 5 == 0 ? "WARN" : "INFO";
                WriteLog(level, $"Thread-{threadId} message-{i}");
                await Task.Delay(5);
            }
        })).ToArray();

        // ✅ 用 try/finally 协调多生产者的 Complete，而不是 fire-and-forget 的 ContinueWith：
        //    ContinueWith 是"发射后不管"，它的异常默认会被吞掉；
        //    try/finally 明确：无论成功还是失败，Complete 一定被调用，且异常可以传播给消费者。
        var coordinator = Task.Run(async () =>
        {
            Exception? fault = null;
            try
            {
                await Task.WhenAll(businessTasks);
            }
            catch (Exception ex)
            {
                fault = ex;
                throw;
            }
            finally
            {
                logChannel.Writer.TryComplete(fault);
            }
        });

        // 消费者（单个日志写盘任务）
        var logWriter = Task.Run(async () =>
        {
            try
            {
                await foreach (LogEntry entry in logChannel.Reader.ReadAllAsync())
                {
                    // 实际项目中这里是写文件/写数据库
                    Console.WriteLine($"  [{entry.Level}] {entry.Time:HH:mm:ss.fff} {entry.Message}");
                    Interlocked.Increment(ref written);
                    await Task.Delay(2); // 模拟 IO 耗时
                }
            }
            catch (ChannelClosedException ex) when (ex.InnerException is not null)
            {
                // 生产者通过 TryComplete(fault) 传来了异常，在此处理
                Console.WriteLine($"  [日志消费者] 生产者发生异常，日志管道关闭：{ex.InnerException.Message}");
            }
        });

        await Task.WhenAll(coordinator, logWriter);

        Console.WriteLine($"\n  日志统计：写入 {written} 条，丢弃 {dropped} 条（因 Channel 满）\n");
    }

    record LogEntry(DateTime Time, string Level, string Message);
}
