using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace Dataflow;

/// <summary>
/// 实战案例：图片处理流水线
/// </summary>
public static class ImagePipelineDemo
{
    /// <summary>
    /// 图片数据模型
    /// </summary>
    public record ImageData(string FileName, byte[] Data, int ProcessingStage);

    /// <summary>
    /// 完整的图片处理流水线
    /// </summary>
    public static async Task CompleteImagePipelineDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   实战：图片处理流水线                     ║");
        Console.WriteLine("║   加载 → 压缩 → 水印 → 上传 → 日志        ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        var processedCount = 0;
        var failedCount = 0;

        // === 阶段 1: 加载文件 ===
        var loadBlock = new TransformBlock<string, ImageData?>(
            async fileName =>
            {
                try
                {
                    Console.WriteLine($"[加载] {fileName}");
                    await Task.Delay(50); // 模拟文件读取
                    var data = new byte[1024 * 100]; // 模拟 100KB 图片
                    return new ImageData(fileName, data, 1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 加载失败: {fileName}, {ex.Message}");
                    Interlocked.Increment(ref failedCount);
                    return null;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 2
            });

        // === 阶段 2: 压缩 ===
        var compressBlock = new TransformBlock<ImageData?, ImageData?>(
            async imageData =>
            {
                if (imageData == null) return null;

                try
                {
                    Console.WriteLine($"[压缩] {imageData.FileName}");
                    await Task.Delay(100); // 模拟压缩耗时
                    var compressed = new byte[imageData.Data.Length / 2]; // 模拟压缩 50%
                    return imageData with { Data = compressed, ProcessingStage = 2 };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 压缩失败: {imageData.FileName}, {ex.Message}");
                    Interlocked.Increment(ref failedCount);
                    return null;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 4 // 压缩是 CPU 密集，多开几个
            });

        // === 阶段 3: 添加水印 ===
        var watermarkBlock = new TransformBlock<ImageData?, ImageData?>(
            async imageData =>
            {
                if (imageData == null) return null;

                try
                {
                    Console.WriteLine($"[水印] {imageData.FileName}");
                    await Task.Delay(80);
                    return imageData with { ProcessingStage = 3 };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 水印失败: {imageData.FileName}, {ex.Message}");
                    Interlocked.Increment(ref failedCount);
                    return null;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 3
            });

        // === 阶段 4: 上传到云存储 ===
        var uploadBlock = new TransformBlock<ImageData?, (string FileName, string? Url)>(
            async imageData =>
            {
                if (imageData == null) return (string.Empty, null);

                try
                {
                    Console.WriteLine($"[上传] {imageData.FileName}");
                    await Task.Delay(150); // 模拟网络上传
                    var url = $"https://cdn.example.com/{imageData.FileName}";
                    return (imageData.FileName, url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[错误] 上传失败: {imageData.FileName}, {ex.Message}");
                    Interlocked.Increment(ref failedCount);
                    return (imageData.FileName, null);
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10,
                MaxDegreeOfParallelism = 5 // 上传是 I/O 密集，可以多开
            });

        // === 阶段 5: 记录日志 ===
        var logBlock = new ActionBlock<(string FileName, string? Url)>(
            result =>
            {
                if (result.Url != null)
                {
                    Console.WriteLine($"[✓ 完成] {result.FileName} → {result.Url}");
                    Interlocked.Increment(ref processedCount);
                }
                else
                {
                    Console.WriteLine($"[✗ 失败] {result.FileName}");
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 10
            });

        // === 组装流水线 ===
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        loadBlock.LinkTo(compressBlock, linkOptions);
        compressBlock.LinkTo(watermarkBlock, linkOptions);
        watermarkBlock.LinkTo(uploadBlock, linkOptions);
        uploadBlock.LinkTo(logBlock, linkOptions);

        // === 发送任务 ===
        var fileCount = 20;
        var files = Enumerable.Range(1, fileCount).Select(i => $"image{i:D3}.jpg").ToList();

        Console.WriteLine($"开始处理 {fileCount} 个文件...\n");
        var stopwatch = Stopwatch.StartNew();

        foreach (var file in files)
        {
            await loadBlock.SendAsync(file);
        }

        // === 等待完成 ===
        loadBlock.Complete();
        await logBlock.Completion;

        stopwatch.Stop();

        Console.WriteLine($"\n╔════════════════════════════════════════════╗");
        Console.WriteLine($"║   流水线处理完成！                         ║");
        Console.WriteLine($"║   总文件数: {fileCount,4}                         ║");
        Console.WriteLine($"║   成功: {processedCount,4}                           ║");
        Console.WriteLine($"║   失败: {failedCount,4}                           ║");
        Console.WriteLine($"║   耗时: {stopwatch.ElapsedMilliseconds,4} ms                       ║");
        Console.WriteLine($"╚════════════════════════════════════════════╝\n");
    }

    /// <summary>
    /// 带错误注入的流水线（测试容错性）
    /// </summary>
    public static async Task FaultTolerantPipelineDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   容错测试：随机失败场景                   ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        var random = new Random(42); // 固定种子，结果可重现
        var processedCount = 0;
        var failedCount = 0;

        // 模拟随机失败的处理块
        var processBlock = new TransformBlock<string, (string FileName, bool Success)>(
            async fileName =>
            {
                await Task.Delay(50);

                // 30% 概率失败
                var shouldFail = random.Next(100) < 30;

                if (shouldFail)
                {
                    Console.WriteLine($"[✗ 失败] {fileName} - 模拟处理错误");
                    Interlocked.Increment(ref failedCount);
                    return (fileName, false);
                }
                else
                {
                    Console.WriteLine($"[✓ 成功] {fileName}");
                    Interlocked.Increment(ref processedCount);
                    return (fileName, true);
                }
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 5,
                MaxDegreeOfParallelism = 3
            });

        // 统计块
        var statsBlock = new ActionBlock<(string FileName, bool Success)>(result =>
        {
            // 只记录，不做额外处理
        });

        processBlock.LinkTo(statsBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // 发送数据
        var fileCount = 30;
        Console.WriteLine($"处理 {fileCount} 个文件（30% 随机失败率）...\n");

        for (int i = 1; i <= fileCount; i++)
        {
            await processBlock.SendAsync($"file{i:D2}.jpg");
        }

        processBlock.Complete();
        await statsBlock.Completion;

        Console.WriteLine($"\n╔════════════════════════════════════════════╗");
        Console.WriteLine($"║   容错测试完成                             ║");
        Console.WriteLine($"║   总文件数: {fileCount,4}                         ║");
        Console.WriteLine($"║   成功: {processedCount,4}                           ║");
        Console.WriteLine($"║   失败: {failedCount,4}                           ║");
        Console.WriteLine($"║   成功率: {(processedCount * 100.0 / fileCount):F1}%                         ║");
        Console.WriteLine($"╚════════════════════════════════════════════╝\n");
    }

    /// <summary>
    /// 批量处理优化：使用 BatchBlock
    /// </summary>
    public static async Task BatchProcessingDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   批量处理优化（BatchBlock）               ║");
        Console.WriteLine("║   场景：每 5 张图片批量上传一次            ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        // 生成图片数据
        var generateBlock = new TransformBlock<int, ImageData>(
            async id =>
            {
                await Task.Delay(20);
                Console.WriteLine($"[生成] image{id}.jpg");
                return new ImageData($"image{id}.jpg", new byte[1024], 0);
            });

        // 批量打包（每 5 个一批）
        var batchBlock = new BatchBlock<ImageData>(batchSize: 5);

        // 批量上传
        var uploadBatchBlock = new ActionBlock<ImageData[]>(
            async batch =>
            {
                Console.WriteLine($"\n[批量上传] {batch.Length} 个文件:");
                foreach (var img in batch)
                {
                    Console.WriteLine($"  - {img.FileName}");
                }

                await Task.Delay(200); // 模拟批量上传
                Console.WriteLine($"[完成] 批量上传成功\n");
            });

        // 组装流水线
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        generateBlock.LinkTo(batchBlock, linkOptions);
        batchBlock.LinkTo(uploadBatchBlock, linkOptions);

        // 发送任务
        var fileCount = 23; // 故意不是 5 的倍数
        Console.WriteLine($"生成 {fileCount} 个文件...\n");

        for (int i = 1; i <= fileCount; i++)
        {
            await generateBlock.SendAsync(i);
        }

        generateBlock.Complete();
        await uploadBatchBlock.Completion;

        Console.WriteLine("批量处理完成！最后一批不足 5 个也会上传\n");
    }

    /// <summary>
    /// 性能对比：串行 vs 并行 vs 流水线
    /// </summary>
    public static async Task PerformanceComparisonDemo()
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   性能对比测试                             ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");

        const int fileCount = 20;
        var files = Enumerable.Range(1, fileCount).Select(i => $"image{i}.jpg").ToList();

        // === 方式 1: 串行处理 ===
        Console.WriteLine("[方式1] 串行处理...");
        var sw1 = Stopwatch.StartNew();

        foreach (var file in files)
        {
            await Task.Delay(30);  // 模拟加载
            await Task.Delay(50);  // 模拟压缩
            await Task.Delay(40);  // 模拟上传
        }

        sw1.Stop();
        Console.WriteLine($"  耗时: {sw1.ElapsedMilliseconds} ms\n");

        // === 方式 2: 完全并行（Task.WhenAll）===
        Console.WriteLine("[方式2] 完全并行（Task.WhenAll）...");
        var sw2 = Stopwatch.StartNew();

        var tasks = files.Select(async file =>
        {
            await Task.Delay(30);  // 模拟加载
            await Task.Delay(50);  // 模拟压缩
            await Task.Delay(40);  // 模拟上传
        });

        await Task.WhenAll(tasks);

        sw2.Stop();
        Console.WriteLine($"  耗时: {sw2.ElapsedMilliseconds} ms\n");

        // === 方式 3: Dataflow 流水线 ===
        Console.WriteLine("[方式3] Dataflow 流水线（每阶段并行度 3）...");
        var sw3 = Stopwatch.StartNew();

        var options = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 10,
            MaxDegreeOfParallelism = 3
        };

        var loadBlock = new TransformBlock<string, string>(
            async file => { await Task.Delay(30); return file; }, options);

        var compressBlock = new TransformBlock<string, string>(
            async file => { await Task.Delay(50); return file; }, options);

        var uploadBlock = new ActionBlock<string>(
            async file => { await Task.Delay(40); }, options);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        loadBlock.LinkTo(compressBlock, linkOptions);
        compressBlock.LinkTo(uploadBlock, linkOptions);

        foreach (var file in files)
        {
            await loadBlock.SendAsync(file);
        }

        loadBlock.Complete();
        await uploadBlock.Completion;

        sw3.Stop();
        Console.WriteLine($"  耗时: {sw3.ElapsedMilliseconds} ms\n");

        // === 结果总结 ===
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║   性能对比结果                             ║");
        Console.WriteLine($"║   串行处理: {sw1.ElapsedMilliseconds,6} ms                       ║");
        Console.WriteLine($"║   完全并行: {sw2.ElapsedMilliseconds,6} ms (内存压力大)        ║");
        Console.WriteLine($"║   流水线:   {sw3.ElapsedMilliseconds,6} ms (平衡)              ║");
        Console.WriteLine($"║                                            ║");
        Console.WriteLine($"║   加速比（相对串行）：                     ║");
        Console.WriteLine($"║   完全并行: {sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds:F2}x                             ║");
        Console.WriteLine($"║   流水线:   {sw1.ElapsedMilliseconds / (double)sw3.ElapsedMilliseconds:F2}x (推荐)                     ║");
        Console.WriteLine("╚════════════════════════════════════════════╝\n");
    }

    /// <summary>
    /// 运行所有图片处理流水线示例
    /// </summary>
    public static async Task RunAllDemos()
    {
        await CompleteImagePipelineDemo();
        await Task.Delay(1000);

        await FaultTolerantPipelineDemo();
        await Task.Delay(1000);

        await BatchProcessingDemo();
        await Task.Delay(1000);

        await PerformanceComparisonDemo();
    }
}
