using Microsoft.Extensions.Logging;

namespace Dataflow.Production;

/// <summary>
/// 生产级流水线使用示例
/// </summary>
public static class ProductionPipelineDemo
{
    /// <summary>
    /// 运行生产级图片处理流水线
    /// </summary>
    public static async Task RunProductionPipelineDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║   🏭 生产级图片处理流水线演示                              ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("║   功能特性：                                               ║");
        Console.WriteLine("║   ✅ 异常重试（最多3次，指数退避）                         ║");
        Console.WriteLine("║   ✅ 背压控制（防止内存溢出）                              ║");
        Console.WriteLine("║   ✅ 实时监控（进度、队列长度）                            ║");
        Console.WriteLine("║   ✅ 优雅关闭（Ctrl+C 完成当前任务）                       ║");
        Console.WriteLine("║   ✅ 完整日志（成功、失败、性能指标）                      ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        // 创建 Logger
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole();
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();

        // 生成测试数据（50张图片）
        var imageUrls = Enumerable.Range(1, 50)
            .Select(i => $"https://example.com/images/photo{i:D3}.jpg")
            .ToList();

        // 创建流水线
        using var pipeline = new ProductionImagePipeline(logger);

        // 设置 Ctrl+C 处理（优雅关闭）
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n⚠️  收到 Ctrl+C，准备优雅关闭...");
            cts.Cancel();
        };

        try
        {
            // 启动流水线
            await pipeline.ProcessAsync(imageUrls, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 等待优雅关闭
            await pipeline.StopAsync(TimeSpan.FromSeconds(30));
        }

        Console.WriteLine("\n✅ 演示完成！按任意键返回主菜单...");
        Console.ReadKey();
    }

    /// <summary>
    /// 对比：传统方式 vs Dataflow 流水线
    /// </summary>
    public static async Task RunComparisonDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   📊 性能对比：传统方式 vs Dataflow 流水线                ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        var imageCount = 30;
        var imageUrls = Enumerable.Range(1, imageCount)
            .Select(i => $"https://example.com/images/photo{i:D3}.jpg")
            .ToList();

        Console.WriteLine($"测试场景：处理 {imageCount} 张图片\n");

        // === 方式1: 串行处理 ===
        Console.WriteLine("【方式1】串行处理（一个接一个）");
        var sw1 = System.Diagnostics.Stopwatch.StartNew();

        foreach (var url in imageUrls)
        {
            await Task.Delay(30);  // 模拟下载
            await Task.Delay(50);  // 模拟压缩
            await Task.Delay(40);  // 模拟水印
            await Task.Delay(100); // 模拟上传
        }

        sw1.Stop();
        Console.WriteLine($"  耗时: {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"  优点: 代码简单，内存占用低");
        Console.WriteLine($"  缺点: 太慢了！\n");

        // === 方式2: 完全并行 ===
        Console.WriteLine("【方式2】完全并行（Task.WhenAll）");
        var sw2 = System.Diagnostics.Stopwatch.StartNew();

        var tasks = imageUrls.Select(async url =>
        {
            await Task.Delay(30);  // 模拟下载
            await Task.Delay(50);  // 模拟压缩
            await Task.Delay(40);  // 模拟水印
            await Task.Delay(100); // 模拟上传
        });

        await Task.WhenAll(tasks);

        sw2.Stop();
        Console.WriteLine($"  耗时: {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"  优点: 很快！");
        Console.WriteLine($"  缺点: {imageCount} 个 Task 同时跑，内存可能爆炸（数量多时）\n");

        // === 方式3: Dataflow 流水线 ===
        Console.WriteLine("【方式3】Dataflow 流水线（推荐）");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning); // 只显示警告和错误
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();
        using var pipeline = new ProductionImagePipeline(logger);

        var sw3 = System.Diagnostics.Stopwatch.StartNew();
        await pipeline.ProcessAsync(imageUrls);
        sw3.Stop();

        Console.WriteLine($"  耗时: {sw3.ElapsedMilliseconds} ms");
        Console.WriteLine($"  优点: 性能好、内存可控、代码优雅、易维护");
        Console.WriteLine($"  缺点: 需要理解 Dataflow 概念\n");

        // === 结果总结 ===
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   📈 性能对比结果                                          ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║   串行处理:   {sw1.ElapsedMilliseconds,6} ms   ⭐                             ║");
        Console.WriteLine($"║   完全并行:   {sw2.ElapsedMilliseconds,6} ms   ⭐⭐⭐⭐⭐ (内存风险)       ║");
        Console.WriteLine($"║   流水线:     {sw3.ElapsedMilliseconds,6} ms   ⭐⭐⭐⭐ (推荐)             ║");
        Console.WriteLine("║                                                            ║");
        Console.WriteLine($"║   加速比（相对串行）：                                     ║");
        Console.WriteLine($"║   完全并行: {sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds:F2}x                                            ║");
        Console.WriteLine($"║   流水线:   {sw1.ElapsedMilliseconds / (double)sw3.ElapsedMilliseconds:F2}x (在内存可控的前提下)               ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        Console.WriteLine("💡 结论：Dataflow 在性能和资源之间取得了最佳平衡\n");
        Console.WriteLine("按任意键返回主菜单...");
        Console.ReadKey();
    }

    /// <summary>
    /// 容错测试：模拟部分失败场景
    /// </summary>
    public static async Task RunFaultToleranceDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   🛡️  容错测试：模拟部分失败场景                           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddConsole(options =>
                {
                    options.TimestampFormat = "[HH:mm:ss] ";
                });
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();

        // 故意构造一些无效 URL（模拟失败）
        var imageUrls = new List<string>();

        for (int i = 1; i <= 20; i++)
        {
            if (i % 5 == 0)
            {
                // 每 5 个有 1 个无效
                imageUrls.Add($"https://invalid-url-{i}.com/404.jpg");
            }
            else
            {
                imageUrls.Add($"https://example.com/images/photo{i:D3}.jpg");
            }
        }

        Console.WriteLine($"📊 测试数据：{imageUrls.Count} 张图片（其中 {imageUrls.Count / 5} 张故意失败）\n");

        using var pipeline = new ProductionImagePipeline(logger);
        await pipeline.ProcessAsync(imageUrls);

        Console.WriteLine("\n💡 观察：");
        Console.WriteLine("  - 失败的图片被正确记录");
        Console.WriteLine("  - 失败不影响其他图片的处理");
        Console.WriteLine("  - 流水线继续运行直到所有任务完成\n");

        Console.WriteLine("按任意键返回主菜单...");
        Console.ReadKey();
    }

    /// <summary>
    /// 背压演示：快速生产 vs 慢速消费
    /// </summary>
    public static async Task RunBackpressureDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   🎚️  背压控制演示                                         ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        Console.WriteLine("场景：快速提交 100 个任务，观察背压机制\n");
        Console.WriteLine("预期：");
        Console.WriteLine("  1. 前 20 个任务快速提交（缓冲区容量）");
        Console.WriteLine("  2. 之后提交速度变慢（等待处理完成）");
        Console.WriteLine("  3. 内存占用始终可控\n");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();

        var imageUrls = Enumerable.Range(1, 100)
            .Select(i => $"https://example.com/images/photo{i:D3}.jpg")
            .ToList();

        using var pipeline = new ProductionImagePipeline(logger);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await pipeline.ProcessAsync(imageUrls);
        stopwatch.Stop();

        Console.WriteLine($"\n✅ 处理完成，总耗时: {stopwatch.ElapsedMilliseconds} ms\n");
        Console.WriteLine("💡 通过 BoundedCapacity，流水线自动节流，内存始终可控\n");

        Console.WriteLine("按任意键返回主菜单...");
        Console.ReadKey();
    }
}
