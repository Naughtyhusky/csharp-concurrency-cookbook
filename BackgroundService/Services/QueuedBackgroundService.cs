namespace BackgroundService.Services;

/// <summary>
/// 队列处理后台服务
/// 从队列中取出任务并执行
/// </summary>
public class QueuedBackgroundService(
    IBackgroundTaskQueue taskQueue,
    ILogger<QueuedBackgroundService> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
    private readonly ILogger<QueuedBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("队列处理服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 从队列中取出任务
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                // 执行任务
                await workItem(stoppingToken);

                _logger.LogInformation("队列任务执行成功");
            }
            catch (OperationCanceledException)
            {
                // 应用正在关闭
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行队列任务时发生错误");
            }
        }

        _logger.LogInformation("队列处理服务已停止");
    }
}
