namespace BackgroundService.Services;

/// <summary>
/// 优雅停止示例服务
/// 演示如何正确处理应用关闭时的清理工作
/// </summary>
public class GracefulShutdownService(ILogger<GracefulShutdownService> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ILogger<GracefulShutdownService> _logger = logger;
    private int _tasksProcessed = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("优雅停止示例服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTaskAsync(stoppingToken);
                _tasksProcessed++;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("收到停止信号，准备优雅退出");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务处理失败");
            }

            // 等待下一次执行
            try
            {
                await Task.Delay(3000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("等待期间收到停止信号");
                break;
            }
        }

        _logger.LogInformation("服务即将停止，共处理了 {Count} 个任务", _tasksProcessed);
    }

    private async Task ProcessTaskAsync(CancellationToken ct)
    {
        _logger.LogInformation("处理任务 #{Count}...", _tasksProcessed + 1);

        // 模拟耗时操作
        await Task.Delay(1000, ct);

        _logger.LogInformation("任务 #{Count} 处理完成", _tasksProcessed + 1);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在优雅停止服务...");
        _logger.LogInformation("等待当前任务完成...");

        // 调用基类的 StopAsync，会设置 stoppingToken 为 Cancelled
        await base.StopAsync(cancellationToken);

        _logger.LogInformation("服务已完全停止，总共处理 {Count} 个任务", _tasksProcessed);
    }
}
