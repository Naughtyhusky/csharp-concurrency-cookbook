using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Dataflow;

/// <summary>
/// 演示 TPL Dataflow 的背压控制（BoundedCapacity）
/// </summary>
public static class BackpressureDemo
{
    /// <summary>
    /// 问题演示：无限制缓冲导致内存压力
    /// </summary>
    public static async Task UnboundedBufferProblemDemo()
    {
        Console.WriteLine("=== 无限制缓冲问题演示 ===\n");
        Console.WriteLine("快速生产 1000 个数据，慢速消费（每个 10ms）...\n");

        var slowBlock = new ActionBlock<int>(async x =>
        {
            await Task.Delay(10); // 慢消费
        });

        var stopwatch = Stopwatch.StartNew();

        // 快速发送 1000 个数据
        for (int i = 1; i <= 1000; i++)
        {
            await slowBlock.SendAsync(i);
            if (i % 100 == 0)
            {
                Console.WriteLine($"已发送: {i} 个数据，缓冲区大小: {slowBlock.InputCount}");
            }
        }

        Console.WriteLine($"\n发送完成，耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"缓冲区积压: {slowBlock.InputCount} 个数据");
        Console.WriteLine("⚠️  警告：所有数据立即堆积在内存中！\n");

        slowBlock.Complete();
        await slowBlock.Completion;

        Console.WriteLine($"处理完成，总耗时: {stopwatch.ElapsedMilliseconds} ms\n");
    }

    /// <summary>
    /// 解决方案：使用 BoundedCapacity 限制缓冲区
    /// </summary>
    public static async Task BoundedCapacityDemo()
    {
        Console.WriteLine("=== BoundedCapacity 背压控制演示 ===\n");
        Console.WriteLine("设置缓冲区容量为 10...\n");

        var slowBlock = new ActionBlock<int>(
            async x =>
            {
                await Task.Delay(10); // 慢消费
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10 // 最多缓冲 10 个数据
            });

        var stopwatch = Stopwatch.StartNew();

        // 发送 100 个数据
        for (int i = 1; i <= 100; i++)
        {
            await slowBlock.SendAsync(i); // 当缓冲满时会阻塞等待

            if (i % 10 == 0)
            {
                Console.WriteLine($"已发送: {i} 个数据，缓冲区大小: {slowBlock.InputCount}，耗时: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        Console.WriteLine($"\n发送完成，耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("✅ 生产速度被自动节流，内存可控！\n");

        slowBlock.Complete();
        await slowBlock.Completion;

        Console.WriteLine($"处理完成，总耗时: {stopwatch.ElapsedMilliseconds} ms\n");
    }

    /// <summary>
    /// 对比演示：有无背压控制的性能差异
    /// </summary>
    public static async Task ComparisonDemo()
    {
        Console.WriteLine("=== 背压控制对比演示 ===\n");

        const int itemCount = 500;
        const int delayMs = 5;

        // 无限制缓冲
        Console.WriteLine("[测试1] 无限制缓冲...");
        var unboundedBlock = new ActionBlock<int>(async x => await Task.Delay(delayMs));

        var sw1 = Stopwatch.StartNew();
        for (int i = 1; i <= itemCount; i++)
        {
            await unboundedBlock.SendAsync(i);
        }
        sw1.Stop();

        Console.WriteLine($"  发送耗时: {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"  缓冲区大小: {unboundedBlock.InputCount}");

        unboundedBlock.Complete();
        var sw1Total = Stopwatch.StartNew();
        await unboundedBlock.Completion;
        sw1Total.Stop();

        Console.WriteLine($"  总耗时: {sw1Total.ElapsedMilliseconds} ms\n");

        // 有限制缓冲
        Console.WriteLine("[测试2] 限制缓冲容量为 10...");
        var boundedBlock = new ActionBlock<int>(
            async x => await Task.Delay(delayMs),
            new ExecutionDataflowBlockOptions { BoundedCapacity = 10 });

        var sw2 = Stopwatch.StartNew();
        for (int i = 1; i <= itemCount; i++)
        {
            await boundedBlock.SendAsync(i);
        }
        sw2.Stop();

        Console.WriteLine($"  发送耗时: {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"  缓冲区大小: {boundedBlock.InputCount}");

        boundedBlock.Complete();
        var sw2Total = Stopwatch.StartNew();
        await boundedBlock.Completion;
        sw2Total.Stop();

        Console.WriteLine($"  总耗时: {sw2Total.ElapsedMilliseconds} ms\n");

        Console.WriteLine("📊 结论：");
        Console.WriteLine($"  - 无限制模式：发送快（{sw1.ElapsedMilliseconds} ms），但内存压力大");
        Console.WriteLine($"  - 限制模式：发送慢（{sw2.ElapsedMilliseconds} ms），但内存可控\n");
    }

    /// <summary>
    /// 流水线背压控制：多阶段限流
    /// </summary>
    public static async Task PipelineBackpressureDemo()
    {
        Console.WriteLine("=== 流水线背压控制演示 ===\n");

        var options = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 5, // 每个块最多缓冲 5 个
            MaxDegreeOfParallelism = 2 // 并行度 2
        };

        var downloadBlock = new TransformBlock<string, byte[]>(
            async url =>
            {
                Console.WriteLine($"  [下载] {url}");
                await Task.Delay(100);
                return new byte[1024]; // 模拟下载 1KB 数据
            }, options);

        var compressBlock = new TransformBlock<byte[], byte[]>(
            async data =>
            {
                Console.WriteLine($"  [压缩] {data.Length} bytes");
                await Task.Delay(150);
                return data;
            }, options);

        var uploadBlock = new ActionBlock<byte[]>(
            async data =>
            {
                Console.WriteLine($"  [上传] {data.Length} bytes");
                await Task.Delay(200);
            }, options);

        // 组装流水线
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        downloadBlock.LinkTo(compressBlock, linkOptions);
        compressBlock.LinkTo(uploadBlock, linkOptions);

        // 发送 20 个任务
        Console.WriteLine("提交 20 个文件处理任务（每个块容量 5）...\n");

        var stopwatch = Stopwatch.StartNew();

        for (int i = 1; i <= 20; i++)
        {
            await downloadBlock.SendAsync($"file{i}.jpg");
            Console.WriteLine($"[提交] 任务 {i}，耗时: {stopwatch.ElapsedMilliseconds} ms");
        }

        Console.WriteLine($"\n所有任务提交完成，耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("📊 观察：前 5 个快速提交，之后逐步节流\n");

        downloadBlock.Complete();
        await uploadBlock.Completion;

        stopwatch.Stop();
        Console.WriteLine($"流水线处理完成，总耗时: {stopwatch.ElapsedMilliseconds} ms\n");
    }

    /// <summary>
    /// 动态调整背压：根据系统负载调整容量
    /// </summary>
    public static async Task DynamicBackpressureDemo()
    {
        Console.WriteLine("=== 动态背压调整演示 ===\n");

        // 模拟根据系统负载动态选择容量
        var systemLoad = 0.7; // 70% 负载
        var capacity = systemLoad switch
        {
            < 0.5 => 50,   // 低负载：大容量
            < 0.8 => 20,   // 中负载：中容量
            _ => 5         // 高负载：小容量
        };

        Console.WriteLine($"当前系统负载: {systemLoad:P0}");
        Console.WriteLine($"自动选择缓冲容量: {capacity}\n");

        var adaptiveBlock = new ActionBlock<int>(
            async x =>
            {
                await Task.Delay(10);
                Console.WriteLine($"  处理: {x}");
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = capacity
            });

        // 发送数据
        for (int i = 1; i <= 30; i++)
        {
            await adaptiveBlock.SendAsync(i);
        }

        adaptiveBlock.Complete();
        await adaptiveBlock.Completion;

        Console.WriteLine("\n动态背压演示完成！\n");
    }

    /// <summary>
    /// 拒绝策略：缓冲区满时的处理方式
    /// </summary>
    public static async Task RejectPolicyDemo()
    {
        Console.WriteLine("=== 缓冲区满时的拒绝策略演示 ===\n");

        var block = new ActionBlock<int>(
            async x =>
            {
                await Task.Delay(100); // 慢速处理
                Console.WriteLine($"  处理: {x}");
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 3 // 容量很小
            });

        // 快速发送数据
        Console.WriteLine("快速发送 10 个数据（容量只有 3）...\n");

        for (int i = 1; i <= 10; i++)
        {
            var accepted = await block.SendAsync(i);

            if (accepted)
            {
                Console.WriteLine($"[接受] 数据 {i} 已接受，缓冲区: {block.InputCount}");
            }
            else
            {
                Console.WriteLine($"[拒绝] 数据 {i} 被拒绝（缓冲区已满）");
            }

            await Task.Delay(20); // 稍微延迟
        }

        block.Complete();
        await block.Completion;

        Console.WriteLine("\n拒绝策略演示完成！\n");
    }

    /// <summary>
    /// 实战：模拟高并发 API 请求限流
    /// </summary>
    public static async Task ApiRateLimitingDemo()
    {
        Console.WriteLine("=== 实战：API 请求限流 ===\n");
        Console.WriteLine("场景：限制每秒最多 5 个 API 请求\n");

        var rateLimitedBlock = new ActionBlock<string>(
            async apiUrl =>
            {
                Console.WriteLine($"  [请求] {apiUrl}");
                await Task.Delay(200); // 模拟 API 调用耗时 200ms
                Console.WriteLine($"  [完成] {apiUrl}");
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,             // 最多缓冲 5 个
                MaxDegreeOfParallelism = 5       // 最多同时 5 个请求
            });

        // 模拟 20 个 API 请求
        var stopwatch = Stopwatch.StartNew();

        for (int i = 1; i <= 20; i++)
        {
            await rateLimitedBlock.SendAsync($"https://api.example.com/data/{i}");
            Console.WriteLine($"[提交] 请求 {i}，耗时: {stopwatch.ElapsedMilliseconds} ms");
        }

        rateLimitedBlock.Complete();
        await rateLimitedBlock.Completion;

        stopwatch.Stop();
        Console.WriteLine($"\n所有请求完成，总耗时: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine("✅ 通过背压控制，实现了请求限流！\n");
    }

    /// <summary>
    /// 运行所有背压控制示例
    /// </summary>
    public static async Task RunAllDemos()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   TPL Dataflow 背压控制示例演示            ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        await UnboundedBufferProblemDemo();
        await Task.Delay(500);

        await BoundedCapacityDemo();
        await Task.Delay(500);

        await ComparisonDemo();
        await Task.Delay(500);

        await PipelineBackpressureDemo();
        await Task.Delay(500);

        await DynamicBackpressureDemo();
        await Task.Delay(500);

        await RejectPolicyDemo();
        await Task.Delay(500);

        await ApiRateLimitingDemo();

        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   所有背压控制示例演示完成！               ║");
        Console.WriteLine("╚════════════════════════════════════════════╝");
    }
}
