namespace BackgroundService.Services;

/// <summary>
/// 简单的 IHostedService 示例
/// 使用 Timer 实现定时任务
/// </summary>
public class SimpleHostedService(ILogger<SimpleHostedService> logger) : IHostedService, IDisposable
{
    private readonly ILogger<SimpleHostedService> _logger = logger;
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SimpleHostedService 正在启动...");

        // 启动一个定时器，每秒触发一次
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        return Task.CompletedTask; // 注意：立即返回，不阻塞
    }

    private void DoWork(object? state)
    {
        _logger.LogInformation("后台任务执行中：{Time}", DateTime.Now);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SimpleHostedService 正在停止...");

        _timer?.Change(Timeout.Infinite, 0); // 停止定时器

        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
