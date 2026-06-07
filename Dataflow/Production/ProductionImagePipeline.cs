using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace Dataflow.Production;

/// <summary>
/// 处理结果模型（用于在流水线各阶段传递数据和状态）
/// </summary>
public class ProcessResult
{
    public string ImageUrl { get; init; } = string.Empty;
    public string ImageId { get; init; } = string.Empty;
    public byte[]? Data { get; set; }
    public string? OutputUrl { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }

    public TimeSpan ElapsedTime => (EndTime ?? DateTime.UtcNow) - StartTime;
}

/// <summary>
/// 流水线配置选项（生产环境由调用方通过配置文件注入）
/// </summary>
public class PipelineOptions
{
    /// <summary>下载块配置（I/O 密集，默认高并行）</summary>
    public ExecutionDataflowBlockOptions? DownloadOptions { get; set; }

    /// <summary>压缩块配置（CPU 密集，默认=核心数）</summary>
    public ExecutionDataflowBlockOptions? CompressOptions { get; set; }

    /// <summary>水印块配置（CPU 密集，默认=核心数）</summary>
    public ExecutionDataflowBlockOptions? WatermarkOptions { get; set; }

    /// <summary>上传块配置（I/O 密集，默认高并行）</summary>
    public ExecutionDataflowBlockOptions? UploadOptions { get; set; }

    /// <summary>日志块配置（默认单线程）</summary>
    public ExecutionDataflowBlockOptions? LogOptions { get; set; }

    /// <summary>HTTP 超时时间（默认30秒）</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>上传重试次数（默认3次）</summary>
    public int MaxUploadRetries { get; set; } = 3;

    /// <summary>监控报告间隔（默认5秒）</summary>
    public TimeSpan MonitorInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 获取默认配置
    /// </summary>
    public static PipelineOptions Default => new()
    {
        DownloadOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 10,
            BoundedCapacity = 20
        },
        CompressOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BoundedCapacity = 10
        },
        WatermarkOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BoundedCapacity = 10
        },
        UploadOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 10,
            BoundedCapacity = 10
        },
        LogOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity = 50
        }
    };
}

/// <summary>
/// 生产级图片处理流水线
/// 功能：下载 → 压缩 → 水印 → 上传 → 日志
/// 特性：
/// - 配置外部化（通过 PipelineOptions 注入）
/// - CancellationToken 全链路传递
/// - 异常重试（指数退避）
/// - 背压控制（防止内存溢出）
/// - 实时监控（进度、队列长度）
/// - 优雅关闭（Ctrl+C 时完成当前任务）
/// - 完整日志（成功、失败、性能指标）
/// </summary>
public class ProductionImagePipeline : IDisposable
{
    private readonly ILogger<ProductionImagePipeline> _logger;
    private readonly PipelineOptions _options;
    private readonly CancellationTokenSource _internalCts = new();

    // 流水线各阶段的块
    private readonly TransformBlock<ProcessResult, ProcessResult> _downloadBlock;
    private readonly TransformBlock<ProcessResult, ProcessResult> _compressBlock;
    private readonly TransformBlock<ProcessResult, ProcessResult> _watermarkBlock;
    private readonly TransformBlock<ProcessResult, ProcessResult> _uploadBlock;
    private readonly ActionBlock<ProcessResult> _logBlock;

    // 统计计数器（线程安全）
    private int _successCount;
    private int _failureCount;
    private int _totalCount;

    // 监控任务
    private CancellationTokenSource? _monitorCts;
    private Task? _monitorTask;

    /// <summary>
    /// 构造函数（支持配置注入）
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">流水线配置（如为 null 则使用默认配置）</param>
    public ProductionImagePipeline(
        ILogger<ProductionImagePipeline> logger,
        PipelineOptions? options = null)
    {
        _logger = logger;
        _options = options ?? PipelineOptions.Default;

        // 创建所有块（传入 CancellationToken）
        _downloadBlock = CreateDownloadBlock();
        _compressBlock = CreateCompressBlock();
        _watermarkBlock = CreateWatermarkBlock();
        _uploadBlock = CreateUploadBlock();
        _logBlock = CreateLogBlock();

        // 组装流水线（LinkTo）
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        _downloadBlock.LinkTo(_compressBlock, linkOptions);
        _compressBlock.LinkTo(_watermarkBlock, linkOptions);
        _watermarkBlock.LinkTo(_uploadBlock, linkOptions);
        _uploadBlock.LinkTo(_logBlock, linkOptions);

        // 监控块的故障状态
        MonitorBlockFaults();

        _logger.LogInformation("🚀 流水线已启动");
    }

    #region 创建各阶段的 Block

    /// <summary>
    /// 阶段1: 下载块（I/O 密集，高并行）
    /// </summary>
    private TransformBlock<ProcessResult, ProcessResult> CreateDownloadBlock()
    {
        var options = _options.DownloadOptions ?? PipelineOptions.Default.DownloadOptions!;

        // 重要：合并外部配置和内部 CancellationToken
        options.CancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(_internalCts.Token, options.CancellationToken)
            .Token;

        return new TransformBlock<ProcessResult, ProcessResult>(
            async result =>
            {
                try
                {
                    _logger.LogDebug("📥 开始下载: {ImageId} - {Url}", result.ImageId, result.ImageUrl);

                    // 实际下载逻辑（支持取消）
                    using var client = new HttpClient { Timeout = _options.HttpTimeout };
                    result.Data = await client.GetByteArrayAsync(result.ImageUrl, options.CancellationToken);

                    _logger.LogDebug("✓ 下载完成: {ImageId}, 大小: {Size} KB",
                        result.ImageId, result.Data.Length / 1024);

                    result.Success = true;
                    return result;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⚠️  下载已取消: {ImageId}", result.ImageId);
                    result.Success = false;
                    result.ErrorMessage = "下载已取消";
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ 下载失败: {ImageId}", result.ImageId);
                    result.Success = false;
                    result.ErrorMessage = $"下载失败: {ex.Message}";
                    return result; // ⚠️ 失败也传递下去，不影响其他文件
                }
            },
            options);
    }

    /// <summary>
    /// 阶段2: 压缩块（CPU 密集，适中并行）
    /// </summary>
    private TransformBlock<ProcessResult, ProcessResult> CreateCompressBlock()
    {
        var options = _options.CompressOptions ?? PipelineOptions.Default.CompressOptions!;
        options.CancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(_internalCts.Token, options.CancellationToken)
            .Token;

        return new TransformBlock<ProcessResult, ProcessResult>(
            async result =>
            {
                // ⚠️ 早期失败快速跳过
                if (!result.Success || result.Data == null)
                    return result;

                try
                {
                    _logger.LogDebug("🗜️  开始压缩: {ImageId}", result.ImageId);

                    // 实际压缩逻辑（这里模拟，支持取消）
                    await Task.Delay(50, options.CancellationToken); // 模拟 CPU 密集操作
                    var originalSize = result.Data.Length;
                    result.Data = new byte[originalSize / 2]; // 模拟压缩 50%

                    _logger.LogDebug("✓ 压缩完成: {ImageId}, {Original}KB → {Compressed}KB", 
                        result.ImageId, originalSize / 1024, result.Data.Length / 1024);

                    return result;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⚠️  压缩已取消: {ImageId}", result.ImageId);
                    result.Success = false;
                    result.ErrorMessage = "压缩已取消";
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ 压缩失败: {ImageId}", result.ImageId);
                    result.Success = false;
                    result.ErrorMessage = $"压缩失败: {ex.Message}";
                    return result;
                }
            },
            options);
    }

    /// <summary>
    /// 阶段3: 水印块（CPU 密集，适中并行）
    /// </summary>
    private TransformBlock<ProcessResult, ProcessResult> CreateWatermarkBlock()
    {
        var options = _options.WatermarkOptions ?? PipelineOptions.Default.WatermarkOptions!;
        options.CancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(_internalCts.Token, options.CancellationToken)
            .Token;

        return new TransformBlock<ProcessResult, ProcessResult>(
            async result =>
            {
                if (!result.Success || result.Data == null)
                    return result;

                try
                {
                    _logger.LogDebug("🖼️  添加水印: {ImageId}", result.ImageId);

                    // 实际水印逻辑（这里模拟）
                    await Task.Delay(40, options.CancellationToken);

                    _logger.LogDebug("✓ 水印完成: {ImageId}", result.ImageId);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⚠️  水印已取消: {ImageId}", result.ImageId);
                    result.Success = false;
                    result.ErrorMessage = "水印已取消";
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ 水印失败: {ImageId}", result.ImageId);
                    result.Success = false;
                    result.ErrorMessage = $"水印失败: {ex.Message}";
                    return result;
                }
            },
            options);
    }

    /// <summary>
    /// 阶段4: 上传块（I/O 密集，高并行，支持重试）
    /// </summary>
    private TransformBlock<ProcessResult, ProcessResult> CreateUploadBlock()
    {
        var options = _options.UploadOptions ?? PipelineOptions.Default.UploadOptions!;
        options.CancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(_internalCts.Token, options.CancellationToken)
            .Token;

        return new TransformBlock<ProcessResult, ProcessResult>(
            async result =>
            {
                if (!result.Success || result.Data == null)
                    return result;

                // ⚠️ 重试逻辑（生产环境必备）
                for (int retry = 0; retry <= _options.MaxUploadRetries; retry++)
                {
                    try
                    {
                        if (retry > 0)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, retry)); // 指数退避：2s, 4s, 8s
                            _logger.LogWarning("🔄 重试上传 ({Retry}/{Max}): {ImageId}, 等待 {Delay}s", 
                                retry, _options.MaxUploadRetries, result.ImageId, delay.TotalSeconds);

                            await Task.Delay(delay, options.CancellationToken);
                        }

                        _logger.LogDebug("📤 上传中: {ImageId}", result.ImageId);

                        // 实际上传逻辑（这里模拟）
                        await Task.Delay(100, options.CancellationToken);
                        result.OutputUrl = $"https://cdn.example.com/{result.ImageId}.jpg";

                        _logger.LogDebug("✓ 上传成功: {ImageId} → {Url}", 
                            result.ImageId, result.OutputUrl);

                        result.Success = true;
                        result.RetryCount = retry;
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("⚠️  上传已取消: {ImageId}", result.ImageId);
                        result.Success = false;
                        result.ErrorMessage = "上传已取消";
                        return result;
                    }
                    catch (Exception ex) when (retry < _options.MaxUploadRetries)
                    {
                        _logger.LogWarning(ex, "⚠️  上传失败，准备重试: {ImageId}", result.ImageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "✗ 上传最终失败: {ImageId}", result.ImageId);
                        result.Success = false;
                        result.ErrorMessage = $"上传失败（已重试 {_options.MaxUploadRetries} 次）: {ex.Message}";
                        result.RetryCount = _options.MaxUploadRetries;
                        return result;
                    }
                }

                return result;
            },
            options);
    }

    /// <summary>
    /// 阶段5: 日志块（单线程，记录最终结果）
    /// </summary>
    private ActionBlock<ProcessResult> CreateLogBlock()
    {
        var options = _options.LogOptions ?? PipelineOptions.Default.LogOptions!;
        options.CancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(_internalCts.Token, options.CancellationToken)
            .Token;

        return new ActionBlock<ProcessResult>(
            result =>
            {
                result.EndTime = DateTime.UtcNow;

                if (result.Success)
                {
                    _logger.LogInformation(
                        "✅ 处理成功: {ImageId}, 耗时: {Elapsed}ms, 重试: {Retry}, 输出: {Url}",
                        result.ImageId, 
                        result.ElapsedTime.TotalMilliseconds, 
                        result.RetryCount,
                        result.OutputUrl);

                    Interlocked.Increment(ref _successCount);
                }
                else
                {
                    _logger.LogError(
                        "❌ 处理失败: {ImageId}, 耗时: {Elapsed}ms, 错误: {Error}",
                        result.ImageId, 
                        result.ElapsedTime.TotalMilliseconds, 
                        result.ErrorMessage);

                    Interlocked.Increment(ref _failureCount);
                }
            },
            options);
    }

    #endregion

    #region 流水线控制

    /// <summary>
    /// 处理图片列表（支持外部取消令牌）
    /// </summary>
    /// <param name="imageUrls">图片URL列表</param>
    /// <param name="cancellationToken">外部取消令牌</param>
    public async Task ProcessAsync(IEnumerable<string> imageUrls, CancellationToken cancellationToken = default)
    {
        _totalCount = imageUrls.Count();
        _logger.LogInformation("📊 开始处理 {Count} 张图片", _totalCount);

        var stopwatch = Stopwatch.StartNew();

        // 启动监控任务（合并外部和内部取消令牌）
        _monitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);
        _monitorTask = MonitorProgressAsync(_monitorCts.Token);

        try
        {
            // 发送任务到流水线
            foreach (var url in imageUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = new ProcessResult
                {
                    ImageUrl = url,
                    ImageId = Guid.NewGuid().ToString("N")[..8] // 8位短ID
                };

                await _downloadBlock.SendAsync(result, cancellationToken);
            }

            // ⚠️ 标记完成并等待
            _downloadBlock.Complete();
            await _logBlock.Completion;

            stopwatch.Stop();

            // 输出最终统计
            var successRate = _totalCount > 0 ? (_successCount * 100.0 / _totalCount) : 0;
            var throughput = _totalCount / stopwatch.Elapsed.TotalSeconds;

            _logger.LogInformation(
                "🎉 流水线处理完成！\n" +
                "   总数: {Total}\n" +
                "   成功: {Success} ({SuccessRate:F1}%)\n" +
                "   失败: {Failure}\n" +
                "   耗时: {Elapsed:F2}s\n" +
                "   吞吐: {Throughput:F2} 张/秒",
                _totalCount, _successCount, successRate, _failureCount, 
                stopwatch.Elapsed.TotalSeconds, throughput);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️  流水线已被取消");
            throw;
        }
        finally
        {
            // 停止监控
            _monitorCts?.Cancel();
            if (_monitorTask != null)
            {
                try { await _monitorTask; } catch { /* 忽略取消异常 */ }
            }
        }
    }

    /// <summary>
    /// 优雅关闭（等待当前任务完成）
    /// </summary>
    public async Task StopAsync(TimeSpan timeout)
    {
        _logger.LogWarning("⚠️  收到停止信号，等待流水线完成当前任务（超时: {Timeout}s）...", 
            timeout.TotalSeconds);

        // 不再接受新任务，但等待现有任务完成
        _downloadBlock.Complete();

        try
        {
            await Task.WhenAny(
                _logBlock.Completion,
                Task.Delay(timeout)
            );

            if (!_logBlock.Completion.IsCompleted)
            {
                _logger.LogWarning("⚠️  超时，强制取消流水线");
                _internalCts.Cancel(); // 触发所有块的 CancellationToken
            }
            else
            {
                _logger.LogInformation("✅ 流水线已优雅关闭");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 流水线关闭异常");
        }
    }

    #endregion

    #region 监控和可观测性

    /// <summary>
    /// 实时监控流水线进度
    /// </summary>
    private async Task MonitorProgressAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.MonitorInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var processed = _successCount + _failureCount;
                var progress = _totalCount > 0 ? (processed * 100.0 / _totalCount) : 0;

                _logger.LogInformation(
                    "📈 进度: {Processed}/{Total} ({Progress:F1}%), " +
                    "成功: {Success}, 失败: {Failure}, " +
                    "队列: [下载:{D}, 压缩:{C}, 水印:{W}, 上传:{U}, 日志:{L}]",
                    processed, _totalCount, progress, _successCount, _failureCount,
                    _downloadBlock.InputCount, 
                    _compressBlock.InputCount, 
                    _watermarkBlock.InputCount, 
                    _uploadBlock.InputCount,
                    _logBlock.InputCount);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
    }

    /// <summary>
    /// 监控块的故障状态
    /// </summary>
    private void MonitorBlockFaults()
    {
        var blocks = new (string Name, IDataflowBlock Block)[]
        {
            ("下载块", _downloadBlock),
            ("压缩块", _compressBlock),
            ("水印块", _watermarkBlock),
            ("上传块", _uploadBlock),
            ("日志块", _logBlock)
        };

        foreach (var (name, block) in blocks)
        {
            block.Completion.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogCritical(task.Exception, 
                        "💥 {BlockName} 进入故障状态！", name);
                }
            }, TaskScheduler.Default);
        }
    }

    #endregion

    #region Block 公共方法（生产环境常用）

    /// <summary>
    /// 获取下载块的输入队列长度（用于监控背压）
    /// </summary>
    public int GetDownloadQueueLength() => _downloadBlock.InputCount;

    /// <summary>
    /// 获取所有块的队列长度（用于性能分析）
    /// </summary>
    public (int Download, int Compress, int Watermark, int Upload, int Log) GetAllQueueLengths()
    {
        return (
            _downloadBlock.InputCount,
            _compressBlock.InputCount,
            _watermarkBlock.InputCount,
            _uploadBlock.InputCount,
            _logBlock.InputCount
        );
    }

    /// <summary>
    /// 获取流水线完成状态（用于健康检查）
    /// </summary>
    public bool IsCompleted => _logBlock.Completion.IsCompleted;

    /// <summary>
    /// 获取流水线故障状态（用于告警）
    /// </summary>
    public bool IsFaulted => _logBlock.Completion.IsFaulted;

    /// <summary>
    /// 获取流水线统计信息（用于监控大盘）
    /// </summary>
    public (int Total, int Success, int Failure, double SuccessRate) GetStatistics()
    {
        var successRate = _totalCount > 0 ? (_successCount * 100.0 / _totalCount) : 0;
        return (_totalCount, _successCount, _failureCount, successRate);
    }

    #endregion

    public void Dispose()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _internalCts.Cancel();
        _internalCts.Dispose();
        _logger.LogInformation("🔌 流水线已释放资源");
    }
}
