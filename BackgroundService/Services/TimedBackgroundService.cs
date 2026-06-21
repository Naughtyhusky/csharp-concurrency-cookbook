namespace BackgroundService.Services;

/// <summary>
/// 使用 PeriodicTimer 实现的定时后台服务
/// .NET 6+ 推荐方式
/// </summary>
public class TimedBackgroundService(ILogger<TimedBackgroundService> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ILogger<TimedBackgroundService> _logger = logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("定时后台服务已启动");

        try
        {
            // PeriodicTimer 的 WaitForNextTickAsync 会等待下一个时间点
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("定时后台服务已停止");
        }
    }

    private async Task DoWorkAsync(CancellationToken ct)
    {
        _logger.LogInformation("执行定时任务：{Time}", DateTime.Now);

        // 模拟耗时操作
        await Task.Delay(2000, ct);

        _logger.LogInformation("任务完成：{Time}", DateTime.Now);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}
