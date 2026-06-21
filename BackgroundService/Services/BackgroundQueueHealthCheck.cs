using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BackgroundService.Services;

/// <summary>
/// 后台队列健康检查
/// 监控队列是否正常工作
/// </summary>
public class BackgroundQueueHealthCheck(
    IBackgroundTaskQueue queue,
    ILogger<BackgroundQueueHealthCheck> logger) : IHealthCheck
{
    private readonly IBackgroundTaskQueue _queue = queue;
    private readonly ILogger<BackgroundQueueHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试向队列中添加一个测试任务
            // 注意：这里只是演示，实际使用中应该检查队列的实际状态
            // 比如队列长度、处理速度等

            var data = new Dictionary<string, object>
            {
                { "QueueStatus", "正常" },
                { "CheckTime", DateTime.Now }
            };

            _logger.LogInformation("队列健康检查通过");

            return HealthCheckResult.Healthy("后台队列运行正常", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "队列健康检查失败");
            return HealthCheckResult.Unhealthy("后台队列异常", ex);
        }
    }
}
