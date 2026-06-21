namespace BackgroundService.Services;

/// <summary>
/// 并发控制示例服务
/// 演示如何使用 SemaphoreSlim 限制并发数
/// </summary>
public class ConcurrentTaskService(
    IBackgroundTaskQueue queue,
    ILogger<ConcurrentTaskService> logger,
    int maxConcurrency = 3) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IBackgroundTaskQueue _queue = queue;
    private readonly ILogger<ConcurrentTaskService> _logger = logger;
    private readonly SemaphoreSlim _concurrencyLimit = new(maxConcurrency);
    private readonly int _maxConcurrency = maxConcurrency;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("并发控制服务已启动，最大并发数：{Max}", _maxConcurrency);

        var tasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 从队列中取出任务
                var workItem = await _queue.DequeueAsync(stoppingToken);

                // 等待获取并发槽位
                await _concurrencyLimit.WaitAsync(stoppingToken);

                // 启动任务（不等待它完成）
                var task = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("开始执行任务，当前并发数：{Count}", _maxConcurrency - _concurrencyLimit.CurrentCount);
                        await workItem(stoppingToken);
                        _logger.LogInformation("任务执行成功");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "任务执行失败");
                    }
                    finally
                    {
                        _concurrencyLimit.Release();
                        _logger.LogInformation("释放并发槽位，剩余槽位：{Count}", _concurrencyLimit.CurrentCount);
                    }
                }, stoppingToken);

                tasks.Add(task);

                // 清理已完成的任务
                tasks.RemoveAll(t => t.IsCompleted);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // 等待所有任务完成
        _logger.LogInformation("等待 {Count} 个任务完成...", tasks.Count);
        await Task.WhenAll(tasks);
        _logger.LogInformation("所有任务已完成");
    }

    public override void Dispose()
    {
        _concurrencyLimit.Dispose();
        base.Dispose();
    }
}
