namespace BackgroundService.Services;

/// <summary>
/// 定时数据同步服务
/// 演示如何使用 IServiceScopeFactory 访问 Scoped 服务
/// </summary>
public class DataSyncService(
    ILogger<DataSyncService> logger,
    IServiceScopeFactory scopeFactory) : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ILogger<DataSyncService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("数据同步服务已启动，每小时执行一次");

        // 启动时立即执行一次
        await SyncDataAsync(stoppingToken);

        // 之后每小时执行一次
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncDataAsync(stoppingToken);
        }
    }

    private async Task SyncDataAsync(CancellationToken ct)
    {
        _logger.LogInformation("开始同步数据：{Time}", DateTime.Now);

        try
        {
            // ⚠️ 关键：使用 Scope 创建作用域服务
            using var scope = _scopeFactory.CreateScope();

            // 这里可以获取 Scoped 服务，例如 DbContext
            // var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 模拟从外部 API 获取数据
            await Task.Delay(1000, ct);

            // 模拟更新数据库
            var syncedCount = Random.Shared.Next(10, 100);

            _logger.LogInformation("数据同步成功，更新了 {Count} 条记录", syncedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据同步失败");
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}
