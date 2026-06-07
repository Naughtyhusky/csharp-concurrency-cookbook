using System.Threading.Tasks.Dataflow;

namespace Dataflow;

/// <summary>
/// 演示 TPL Dataflow 的 LinkTo 数据传递和路由
/// </summary>
public static class LinkToDemo
{
    /// <summary>
    /// 基本 LinkTo 连接示例
    /// </summary>
    public static async Task BasicLinkToDemo()
    {
        Console.WriteLine("=== 基本 LinkTo 连接示例 ===\n");

        var step1 = new TransformBlock<int, int>(x =>
        {
            var result = x * 2;
            Console.WriteLine($"[步骤1] {x} × 2 = {result}");
            return result;
        });

        var step2 = new TransformBlock<int, string>(x =>
        {
            var result = $"结果: {x}";
            Console.WriteLine($"[步骤2] {x} → {result}");
            return result;
        });

        var step3 = new ActionBlock<string>(str =>
        {
            Console.WriteLine($"[步骤3] 最终输出: {str}\n");
        });

        // 组装流水线（自动传播完成状态）
        step1.LinkTo(step2, new DataflowLinkOptions { PropagateCompletion = true });
        step2.LinkTo(step3, new DataflowLinkOptions { PropagateCompletion = true });

        // 发送数据到头部
        Console.WriteLine("发送数据 1-3...\n");
        for (int i = 1; i <= 3; i++)
        {
            await step1.SendAsync(i);
        }

        // 只需标记头部完成，会自动传播到尾部
        step1.Complete();
        await step3.Completion;

        Console.WriteLine("流水线处理完成！\n");
    }

    /// <summary>
    /// 条件路由：数据分流示例
    /// </summary>
    public static async Task ConditionalRoutingDemo()
    {
        Console.WriteLine("=== 条件路由：奇偶数分流 ===\n");

        var splitter = new TransformBlock<int, int>(x => x);

        var evenBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [偶数通道] {x}"));

        var oddBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [奇数通道] {x}"));

        // 设置路由条件（Predicate）
        splitter.LinkTo(evenBlock, x => x % 2 == 0); // 偶数
        splitter.LinkTo(oddBlock, x => x % 2 != 0);  // 奇数

        // 发送数据
        Console.WriteLine("发送数据 1-10...\n");
        for (int i = 1; i <= 10; i++)
        {
            await splitter.SendAsync(i);
        }

        splitter.Complete();
        await Task.WhenAll(evenBlock.Completion, oddBlock.Completion);

        Console.WriteLine("\n分流处理完成！\n");
    }

    /// <summary>
    /// 条件路由：带兜底处理的分流
    /// </summary>
    public static async Task ConditionalRoutingWithFallbackDemo()
    {
        Console.WriteLine("=== 条件路由：带兜底处理 ===\n");

        var splitter = new TransformBlock<int, int>(x => x);

        var smallBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [小数字 ≤ 5] {x}"));

        var largeBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [大数字 > 5] {x}"));

        var discardBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [兜底通道] 未匹配: {x}"));

        // 先设置具体条件
        splitter.LinkTo(smallBlock, x => x <= 5);
        splitter.LinkTo(largeBlock, x => x > 5);

        // 最后设置兜底（无条件 = 匹配所有）
        splitter.LinkTo(discardBlock);

        // 发送数据
        Console.WriteLine("发送数据 1-10...\n");
        for (int i = 1; i <= 10; i++)
        {
            await splitter.SendAsync(i);
        }

        splitter.Complete();
        await Task.WhenAll(smallBlock.Completion, largeBlock.Completion, discardBlock.Completion);

        Console.WriteLine("\n带兜底的分流处理完成！\n");
    }

    /// <summary>
    /// 多级路由：复杂分流示例
    /// </summary>
    public static async Task MultiLevelRoutingDemo()
    {
        Console.WriteLine("=== 多级路由：数字分类处理 ===\n");

        var source = new TransformBlock<int, int>(x => x);

        // 第一级：奇偶分流
        var evenSplitter = new TransformBlock<int, int>(x => x);
        var oddSplitter = new TransformBlock<int, int>(x => x);

        // 第二级：按大小分流
        var smallEvenBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [偶数 且 ≤ 50] {x}"));

        var largeEvenBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [偶数 且 > 50] {x}"));

        var smallOddBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [奇数 且 ≤ 50] {x}"));

        var largeOddBlock = new ActionBlock<int>(x =>
            Console.WriteLine($"  [奇数 且 > 50] {x}"));

        // 第一级路由
        source.LinkTo(evenSplitter, x => x % 2 == 0);
        source.LinkTo(oddSplitter, x => x % 2 != 0);

        // 第二级路由
        evenSplitter.LinkTo(smallEvenBlock, x => x <= 50);
        evenSplitter.LinkTo(largeEvenBlock, x => x > 50);

        oddSplitter.LinkTo(smallOddBlock, x => x <= 50);
        oddSplitter.LinkTo(largeOddBlock, x => x > 50);

        // 发送数据
        var numbers = new[] { 10, 25, 42, 55, 60, 71, 88, 99 };
        Console.WriteLine($"发送数据: [{string.Join(", ", numbers)}]\n");

        foreach (var num in numbers)
        {
            await source.SendAsync(num);
        }

        source.Complete();

        await Task.WhenAll(
            smallEvenBlock.Completion,
            largeEvenBlock.Completion,
            smallOddBlock.Completion,
            largeOddBlock.Completion);

        Console.WriteLine("\n多级路由处理完成！\n");
    }

    /// <summary>
    /// 扇出（Fan-Out）模式：一个数据源，多个处理器
    /// </summary>
    public static async Task FanOutDemo()
    {
        Console.WriteLine("=== 扇出模式：并行处理 ===\n");

        var source = new BroadcastBlock<int>(x => x);

        // 三个独立的处理器
        var processor1 = new ActionBlock<int>(async x =>
        {
            await Task.Delay(100);
            Console.WriteLine($"  [处理器1] 完成处理: {x}");
        });

        var processor2 = new ActionBlock<int>(async x =>
        {
            await Task.Delay(150);
            Console.WriteLine($"  [处理器2] 完成处理: {x}");
        });

        var processor3 = new ActionBlock<int>(async x =>
        {
            await Task.Delay(200);
            Console.WriteLine($"  [处理器3] 完成处理: {x}");
        });

        // 连接到三个处理器
        source.LinkTo(processor1);
        source.LinkTo(processor2);
        source.LinkTo(processor3);

        // 发送数据
        Console.WriteLine("发送数据 1-5（每个都会被三个处理器处理）...\n");
        for (int i = 1; i <= 5; i++)
        {
            await source.SendAsync(i);
            Console.WriteLine($"[数据源] 发送: {i}");
        }

        source.Complete();
        await Task.WhenAll(processor1.Completion, processor2.Completion, processor3.Completion);

        Console.WriteLine("\n扇出处理完成！\n");
    }

    /// <summary>
    /// 扇入（Fan-In）模式：多个数据源，一个处理器
    /// </summary>
    public static async Task FanInDemo()
    {
        Console.WriteLine("=== 扇入模式：数据聚合 ===\n");

        // 三个数据源
        var source1 = new TransformBlock<int, string>(x => $"源1: {x}");
        var source2 = new TransformBlock<int, string>(x => $"源2: {x}");
        var source3 = new TransformBlock<int, string>(x => $"源3: {x}");

        // 一个聚合处理器
        var aggregator = new ActionBlock<string>(msg =>
            Console.WriteLine($"  [聚合器] 收到: {msg}"));

        // 连接所有数据源到聚合器
        source1.LinkTo(aggregator, new DataflowLinkOptions { PropagateCompletion = false });
        source2.LinkTo(aggregator, new DataflowLinkOptions { PropagateCompletion = false });
        source3.LinkTo(aggregator, new DataflowLinkOptions { PropagateCompletion = false });

        // 发送数据
        Console.WriteLine("从三个源发送数据...\n");

        var task1 = Task.Run(async () =>
        {
            for (int i = 1; i <= 3; i++)
            {
                await source1.SendAsync(i);
                await Task.Delay(50);
            }
            source1.Complete();
        });

        var task2 = Task.Run(async () =>
        {
            for (int i = 1; i <= 3; i++)
            {
                await source2.SendAsync(i);
                await Task.Delay(70);
            }
            source2.Complete();
        });

        var task3 = Task.Run(async () =>
        {
            for (int i = 1; i <= 3; i++)
            {
                await source3.SendAsync(i);
                await Task.Delay(90);
            }
            source3.Complete();
        });

        // 等待所有源完成
        await Task.WhenAll(task1, task2, task3);

        // 等待所有数据被聚合器处理完
        await Task.WhenAll(source1.Completion, source2.Completion, source3.Completion);

        // 手动标记聚合器完成
        aggregator.Complete();
        await aggregator.Completion;

        Console.WriteLine("\n扇入处理完成！\n");
    }

    /// <summary>
    /// 运行所有 LinkTo 示例
    /// </summary>
    public static async Task RunAllDemos()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   TPL Dataflow LinkTo 数据传递示例         ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        await BasicLinkToDemo();
        await Task.Delay(500);

        await ConditionalRoutingDemo();
        await Task.Delay(500);

        await ConditionalRoutingWithFallbackDemo();
        await Task.Delay(500);

        await MultiLevelRoutingDemo();
        await Task.Delay(500);

        await FanOutDemo();
        await Task.Delay(500);

        await FanInDemo();

        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   所有 LinkTo 示例演示完成！               ║");
        Console.WriteLine("╚════════════════════════════════════════════╝");
    }
}
