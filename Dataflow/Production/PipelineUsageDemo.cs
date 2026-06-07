using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace Dataflow.Production;

/// <summary>
/// 演示如何在生产环境中使用 ProductionImagePipeline
/// </summary>
public static class PipelineUsageDemo
{
    /// <summary>
    /// Demo 1: 使用默认配置（最简单）
    /// </summary>
    public static async Task RunWithDefaultOptions()
    {
        Console.WriteLine("\n=== Demo 1: 使用默认配置 ===\n");

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();

        // ✅ 不传配置，使用默认值
        using var pipeline = new ProductionImagePipeline(logger);

        var urls = Enumerable.Range(1, 20)
            .Select(i => $"https://example.com/image{i}.jpg");

        await pipeline.ProcessAsync(urls);
    }

    /// <summary>
    /// Demo 2: 自定义配置（生产环境推荐）
    /// </summary>
    public static async Task RunWithCustomOptions()
    {
        Console.WriteLine("\n=== Demo 2: 自定义配置（读取配置文件） ===\n");

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();

        // ✅ 在生产环境中，这些参数应该从 appsettings.json 读取
        var options = new PipelineOptions
        {
            DownloadOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 20,  // 网络好的话可以多开
                BoundedCapacity = 50
            },
            CompressOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 8,   // 假设是 8 核服务器
                BoundedCapacity = 20
            },
            UploadOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 15,
                BoundedCapacity = 20
            },
            HttpTimeout = TimeSpan.FromSeconds(60),  // 大文件下载时间长
            MaxUploadRetries = 5,                     // 网络不稳定时多重试几次
            MonitorInterval = TimeSpan.FromSeconds(3) // 更频繁的监控
        };

        using var pipeline = new ProductionImagePipeline(logger, options);

        var urls = Enumerable.Range(1, 50)
            .Select(i => $"https://example.com/image{i}.jpg");

        await pipeline.ProcessAsync(urls);
    }

    /// <summary>
    /// Demo 3: 支持外部取消（Ctrl+C）
    /// </summary>
    public static async Task RunWithCancellation()
    {
        Console.WriteLine("\n=== Demo 3: 支持外部取消（按 Ctrl+C 停止）===\n");

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();
        using var pipeline = new ProductionImagePipeline(logger);

        // ✅ 注册 Ctrl+C 取消
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("\n⚠️  检测到 Ctrl+C，准备取消...");
            e.Cancel = true; // 阻止立即退出
            cts.Cancel();
        };

        var urls = Enumerable.Range(1, 100)
            .Select(i => $"https://example.com/image{i}.jpg");

        try
        {
            // ✅ 传递 CancellationToken
            await pipeline.ProcessAsync(urls, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 任务已被取消");
        }
    }

    /// <summary>
    /// Demo 4: 实时监控队列长度（用于性能调优）
    /// </summary>
    public static async Task RunWithMonitoring()
    {
        Console.WriteLine("\n=== Demo 4: 实时监控队列长度 ===\n");

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();
        using var pipeline = new ProductionImagePipeline(logger);

        var urls = Enumerable.Range(1, 100)
            .Select(i => $"https://example.com/image{i}.jpg");

        // ✅ 启动一个独立的监控任务
        using var cts = new CancellationTokenSource();
        var monitorTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var queues = pipeline.GetAllQueueLengths();
                var stats = pipeline.GetStatistics();

                Console.WriteLine(
                    $"[监控] 队列: 下载={queues.Download}, 压缩={queues.Compress}, " +
                    $"上传={queues.Upload} | 统计: {stats.Success}/{stats.Total} 成功");

                await Task.Delay(2000, cts.Token);
            }
        }, cts.Token);

        await pipeline.ProcessAsync(urls);

        cts.Cancel();
        try { await monitorTask; } catch { }
    }

    /// <summary>
    /// Demo 5: 优雅关闭（等待当前任务完成）
    /// </summary>
    public static async Task RunWithGracefulShutdown()
    {
        Console.WriteLine("\n=== Demo 5: 优雅关闭（3秒后自动停止）===\n");

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning); // 减少日志输出
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();
        using var pipeline = new ProductionImagePipeline(logger);

        var urls = Enumerable.Range(1, 1000) // 故意设置很多任务
            .Select(i => $"https://example.com/image{i}.jpg");

        // ✅ 在后台启动处理
        var processTask = Task.Run(async () =>
        {
            await pipeline.ProcessAsync(urls);
        });

        // 3秒后触发优雅关闭
        await Task.Delay(3000);
        Console.WriteLine("\n⏰ 3秒已到，触发优雅关闭...\n");

        await pipeline.StopAsync(TimeSpan.FromSeconds(10)); // 最多等10秒

        try
        {
            await processTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 流水线异常: {ex.Message}");
        }
    }

    /// <summary>
    /// Demo 6: 从配置文件读取参数（模拟生产环境）
    /// </summary>
    public static async Task RunWithConfigFile()
    {
        Console.WriteLine("\n=== Demo 6: 从配置文件读取参数（模拟）===\n");

        // ✅ 在真实项目中，这些参数应该从 IConfiguration 读取
        // var config = hostBuilder.Configuration.GetSection("Pipeline");
        var config = new
        {
            Download = new { Parallelism = 20, Capacity = 50 },
            Compress = new { Parallelism = 8, Capacity = 20 },
            Upload = new { Parallelism = 15, Capacity = 20 },
            HttpTimeoutSeconds = 60,
            MaxRetries = 5
        };

        var options = new PipelineOptions
        {
            DownloadOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = config.Download.Parallelism,
                BoundedCapacity = config.Download.Capacity
            },
            CompressOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = config.Compress.Parallelism,
                BoundedCapacity = config.Compress.Capacity
            },
            UploadOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = config.Upload.Parallelism,
                BoundedCapacity = config.Upload.Capacity
            },
            HttpTimeout = TimeSpan.FromSeconds(config.HttpTimeoutSeconds),
            MaxUploadRetries = config.MaxRetries
        };

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();
        using var pipeline = new ProductionImagePipeline(logger, options);

        Console.WriteLine($"📝 配置参数: 下载并行={config.Download.Parallelism}, " +
                          $"压缩并行={config.Compress.Parallelism}, " +
                          $"超时={config.HttpTimeoutSeconds}秒");

        var urls = Enumerable.Range(1, 30)
            .Select(i => $"https://example.com/image{i}.jpg");

        await pipeline.ProcessAsync(urls);
    }

    /// <summary>
    /// Demo 7: 健康检查（用于 Kubernetes 或监控系统）
    /// </summary>
    public static async Task RunWithHealthCheck()
    {
        Console.WriteLine("\n=== Demo 7: 健康检查（模拟监控系统）===\n");

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<ProductionImagePipeline>();
        using var pipeline = new ProductionImagePipeline(logger);

        var urls = Enumerable.Range(1, 50)
            .Select(i => $"https://example.com/image{i}.jpg");

        // ✅ 启动健康检查任务
        using var cts = new CancellationTokenSource();
        var healthCheckTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // 模拟监控系统定期检查
                var isHealthy = !pipeline.IsFaulted && 
                                (pipeline.IsCompleted || pipeline.GetDownloadQueueLength() < 100);

                var status = isHealthy ? "✅ HEALTHY" : "❌ UNHEALTHY";
                Console.WriteLine($"[健康检查] {status} | 故障={pipeline.IsFaulted}, " +
                                  $"完成={pipeline.IsCompleted}");

                await Task.Delay(2000, cts.Token);
            }
        }, cts.Token);

        await pipeline.ProcessAsync(urls);

        cts.Cancel();
        try { await healthCheckTask; } catch { }

        // 最终健康状态
        var finalStats = pipeline.GetStatistics();
        Console.WriteLine($"\n📊 最终统计: 成功率={finalStats.SuccessRate:F1}%");
    }
}
