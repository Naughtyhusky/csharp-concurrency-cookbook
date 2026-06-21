using Microsoft.AspNetCore.Mvc;

namespace BackgroundService.Controllers;

/// <summary>
/// 监控控制器
/// 提供后台服务的监控接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitorController(ILogger<MonitorController> logger) : ControllerBase
{
    private readonly ILogger<MonitorController> _logger = logger;

    /// <summary>
    /// 获取服务状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = new
        {
            Status = "Running",
            Time = DateTime.Now,
            Uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime,
            Services = new[]
            {
                new { Name = "TimedBackgroundService", Status = "Running" },
                new { Name = "QueuedBackgroundService", Status = "Running" },
                new { Name = "DataSyncService", Status = "Running" }
            }
        };

        return Ok(status);
    }

    /// <summary>
    /// 触发手动同步（演示如何从 API 触发后台任务）
    /// </summary>
    [HttpPost("trigger-sync")]
    public IActionResult TriggerSync()
    {
        _logger.LogInformation("手动触发数据同步");

        // 实际应用中，可以通过某种机制通知后台服务立即执行
        // 比如使用 Channel、Event 等

        return Ok(new { Message = "同步任务已触发" });
    }
}
