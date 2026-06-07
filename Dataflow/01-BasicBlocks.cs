using System.Threading.Tasks.Dataflow;

namespace Dataflow;

/// <summary>
/// 演示 TPL Dataflow 的基本块用法
/// </summary>
public static class BasicBlocksDemo
{
    /// <summary>
    /// ActionBlock 示例：消费者块
    /// </summary>
    public static async Task ActionBlockDemo()
    {
        Console.WriteLine("=== ActionBlock 示例 ===\n");

        // 创建一个简单的消费者块
        var actionBlock = new ActionBlock<int>(async num =>
        {
            await Task.Delay(100); // 模拟耗时操作
            Console.WriteLine($"处理数字: {num}");
        });

        // 发送数据
        Console.WriteLine("发送数据 1-5...");
        for (int i = 1; i <= 5; i++)
        {
            await actionBlock.SendAsync(i);
        }

        // 标记完成并等待
        actionBlock.Complete();
        await actionBlock.Completion;

        Console.WriteLine("所有数据处理完成！\n");
    }

    /// <summary>
    /// ActionBlock 并行处理示例
    /// </summary>
    public static async Task ActionBlockParallelDemo()
    {
        Console.WriteLine("=== ActionBlock 并行处理示例 ===\n");

        var actionBlock = new ActionBlock<int>(
            async num =>
            {
                var threadId = Environment.CurrentManagedThreadId;
                Console.WriteLine($"[线程 {threadId}] 开始处理 {num}");
                await Task.Delay(500);
                Console.WriteLine($"[线程 {threadId}] 完成处理 {num}");
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 3 // 最多 3 个并行
            });

        Console.WriteLine("发送 10 个任务（最多 3 个并行）...\n");
        for (int i = 1; i <= 10; i++)
        {
            await actionBlock.SendAsync(i);
        }

        actionBlock.Complete();
        await actionBlock.Completion;

        Console.WriteLine("\n所有任务完成！\n");
    }

    /// <summary>
    /// TransformBlock 示例：转换块
    /// </summary>
    public static async Task TransformBlockDemo()
    {
        Console.WriteLine("=== TransformBlock 示例 ===\n");

        // 创建转换块：数字 × 2
        var transformBlock = new TransformBlock<int, int>(num =>
        {
            var result = num * 2;
            Console.WriteLine($"转换: {num} → {result}");
            return result;
        });

        // 创建消费块：打印结果
        var printBlock = new ActionBlock<int>(num =>
        {
            Console.WriteLine($"最终结果: {num}");
        });

        // 连接两个块
        transformBlock.LinkTo(printBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // 发送数据
        Console.WriteLine("发送数据 1-5...\n");
        for (int i = 1; i <= 5; i++)
        {
            await transformBlock.SendAsync(i);
        }

        // 只需标记头部完成，会自动传播到尾部
        transformBlock.Complete();
        await printBlock.Completion;

        Console.WriteLine("\n流水线处理完成！\n");
    }

    /// <summary>
    /// TransformBlock 链式处理示例
    /// </summary>
    public static async Task TransformBlockChainDemo()
    {
        Console.WriteLine("=== TransformBlock 链式处理示例 ===\n");

        // 阶段 1: 数字 × 2
        var multiplyBlock = new TransformBlock<int, int>(num =>
        {
            var result = num * 2;
            Console.WriteLine($"[阶段1] {num} × 2 = {result}");
            return result;
        });

        // 阶段 2: 转换为字符串
        var toStringBlock = new TransformBlock<int, string>(num =>
        {
            var result = $"数字: {num}";
            Console.WriteLine($"[阶段2] {num} → \"{result}\"");
            return result;
        });

        // 阶段 3: 添加前缀
        var addPrefixBlock = new TransformBlock<string, string>(str =>
        {
            var result = $"【结果】{str}";
            Console.WriteLine($"[阶段3] 添加前缀 → \"{result}\"");
            return result;
        });

        // 阶段 4: 打印
        var printBlock = new ActionBlock<string>(str =>
        {
            Console.WriteLine($"[最终] {str}\n");
        });

        // 组装流水线
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        multiplyBlock.LinkTo(toStringBlock, linkOptions);
        toStringBlock.LinkTo(addPrefixBlock, linkOptions);
        addPrefixBlock.LinkTo(printBlock, linkOptions);

        // 发送数据
        Console.WriteLine("发送数据 1-3...\n");
        for (int i = 1; i <= 3; i++)
        {
            await multiplyBlock.SendAsync(i);
        }

        multiplyBlock.Complete();
        await printBlock.Completion;

        Console.WriteLine("链式处理完成！\n");
    }

    /// <summary>
    /// BufferBlock 示例：缓冲队列
    /// </summary>
    public static async Task BufferBlockDemo()
    {
        Console.WriteLine("=== BufferBlock 示例 ===\n");

        var bufferBlock = new BufferBlock<int>();

        // 生产者：快速发送数据
        var producer = Task.Run(async () =>
        {
            for (int i = 1; i <= 10; i++)
            {
                await bufferBlock.SendAsync(i);
                Console.WriteLine($"[生产者] 生产: {i}");
                await Task.Delay(50); // 快速生产
            }
            bufferBlock.Complete();
            Console.WriteLine("[生产者] 生产完成");
        });

        // 消费者：慢速消费数据
        var consumer = Task.Run(async () =>
        {
            while (await bufferBlock.OutputAvailableAsync())
            {
                var item = await bufferBlock.ReceiveAsync();
                Console.WriteLine($"  [消费者] 消费: {item}");
                await Task.Delay(200); // 慢速消费
            }
            Console.WriteLine("  [消费者] 消费完成");
        });

        await Task.WhenAll(producer, consumer);

        Console.WriteLine("\nBufferBlock 演示完成！\n");
    }

    /// <summary>
    /// BatchBlock 示例：批量打包
    /// </summary>
    public static async Task BatchBlockDemo()
    {
        Console.WriteLine("=== BatchBlock 示例 ===\n");

        // 每 3 个数据打包一次
        var batchBlock = new BatchBlock<int>(batchSize: 3);

        var printBlock = new ActionBlock<int[]>(batch =>
        {
            Console.WriteLine($"处理批次: [{string.Join(", ", batch)}]");
        });

        batchBlock.LinkTo(printBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // 发送 10 个数据
        Console.WriteLine("发送 10 个数据（每 3 个打包一次）...\n");
        for (int i = 1; i <= 10; i++)
        {
            await batchBlock.SendAsync(i);
            Console.WriteLine($"发送: {i}");
        }

        batchBlock.Complete();
        await printBlock.Completion;

        Console.WriteLine("\nBatchBlock 演示完成！\n");
    }

    /// <summary>
    /// BroadcastBlock 示例：广播块
    /// </summary>
    public static async Task BroadcastBlockDemo()
    {
        Console.WriteLine("=== BroadcastBlock 示例 ===\n");

        var broadcastBlock = new BroadcastBlock<int>(x => x);

        var consumer1 = new ActionBlock<int>(x =>
            Console.WriteLine($"  [消费者1] 收到: {x}"));

        var consumer2 = new ActionBlock<int>(x =>
            Console.WriteLine($"  [消费者2] 收到: {x}"));

        var consumer3 = new ActionBlock<int>(x =>
            Console.WriteLine($"  [消费者3] 收到: {x}"));

        // 连接三个消费者
        broadcastBlock.LinkTo(consumer1);
        broadcastBlock.LinkTo(consumer2);
        broadcastBlock.LinkTo(consumer3);

        // 发送数据（每个消费者都会收到）
        Console.WriteLine("广播数据 1-5...\n");
        for (int i = 1; i <= 5; i++)
        {
            await broadcastBlock.SendAsync(i);
            Console.WriteLine($"[广播] {i}");
            await Task.Delay(100); // 延迟一下方便观察
        }

        Console.WriteLine("\nBroadcastBlock 演示完成！\n");
    }

    /// <summary>
    /// 运行所有基本块示例
    /// </summary>
    public static async Task RunAllDemos()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   TPL Dataflow 基本块示例演示              ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        await ActionBlockDemo();
        await Task.Delay(500);

        await ActionBlockParallelDemo();
        await Task.Delay(500);

        await TransformBlockDemo();
        await Task.Delay(500);

        await TransformBlockChainDemo();
        await Task.Delay(500);

        await BufferBlockDemo();
        await Task.Delay(500);

        await BatchBlockDemo();
        await Task.Delay(500);

        await BroadcastBlockDemo();

        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   所有基本块示例演示完成！                 ║");
        Console.WriteLine("╚════════════════════════════════════════════╝");
    }
}
