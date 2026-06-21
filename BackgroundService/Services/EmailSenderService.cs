namespace BackgroundService.Services;

/// <summary>
/// 邮件发送后台服务
/// 带重试机制的队列处理
/// </summary>
public class EmailSenderService(
    IBackgroundTaskQueue queue,
    ILogger<EmailSenderService> logger,
    IServiceScopeFactory scopeFactory) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IBackgroundTaskQueue _queue = queue;
    private readonly ILogger<EmailSenderService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("邮件发送服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);
                await ExecuteWithRetryAsync(workItem, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("邮件发送服务已停止");
    }

    private async Task ExecuteWithRetryAsync(
        Func<CancellationToken, ValueTask> workItem,
        CancellationToken ct)
    {
        const int maxRetries = 3;
        var retryDelays = new[] { 1000, 5000, 15000 }; // 1s, 5s, 15s

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await workItem(ct);
                _logger.LogInformation("邮件发送成功");
                return; // 成功，退出
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                _logger.LogWarning(ex,
                    "邮件发送失败（尝试 {Attempt}/{Max}），{Delay}ms 后重试",
                    attempt + 1, maxRetries, retryDelays[attempt]);

                await Task.Delay(retryDelays[attempt], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "邮件发送失败，已达最大重试次数");
                // 可以记录到数据库或死信队列
            }
        }
    }
}
