using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimiting.Controllers;

/// <summary>
/// 演示 ASP.NET Core 内置限流中间件的使用
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RateLimitDemoController(ILogger<RateLimitDemoController> logger) : ControllerBase
{
    private readonly ILogger<RateLimitDemoController> _logger = logger;

    /// <summary>
    /// 固定窗口限流：每10秒最多100次请求
    /// </summary>
    [HttpGet("fixed")]
    [EnableRateLimiting("fixed")]
    public IActionResult FixedWindow()
    {
        _logger.LogInformation("固定窗口限流接口被调用");
        return Ok(new
        {
            message = "固定窗口限流：每10秒最多100次请求",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 滑动窗口限流：每10秒最多100次请求（更平滑）
    /// </summary>
    [HttpGet("sliding")]
    [EnableRateLimiting("sliding")]
    public IActionResult SlidingWindow()
    {
        _logger.LogInformation("滑动窗口限流接口被调用");
        return Ok(new
        {
            message = "滑动窗口限流：每10秒最多100次请求",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 令牌桶限流：容量100，每秒补充10个令牌
    /// </summary>
    [HttpGet("token")]
    [EnableRateLimiting("token")]
    public IActionResult TokenBucket()
    {
        _logger.LogInformation("令牌桶限流接口被调用");
        return Ok(new
        {
            message = "令牌桶限流：容量100，每秒补充10个令牌",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 并发限流：最多10个并发请求
    /// </summary>
    [HttpGet("concurrency")]
    [EnableRateLimiting("concurrency")]
    public async Task<IActionResult> Concurrency()
    {
        _logger.LogInformation("并发限流接口被调用");

        // 模拟耗时操作
        await Task.Delay(2000);

        return Ok(new
        {
            message = "并发限流：最多10个并发请求",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 按IP限流：每个IP每分钟最多10次
    /// </summary>
    [HttpGet("per-ip")]
    [EnableRateLimiting("perIp")]
    public IActionResult PerIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _logger.LogInformation($"按IP限流接口被调用，IP: {ip}");

        return Ok(new
        {
            message = "按IP限流：每个IP每分钟最多10次",
            ip = ip,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 按用户限流：VIP用户和普通用户不同限额
    /// </summary>
    [HttpGet("per-user")]
    [EnableRateLimiting("perUser")]
    public IActionResult PerUser()
    {
        var userId = User.Identity?.Name ?? "anonymous";
        var isVip = User.IsInRole("VIP");

        _logger.LogInformation($"按用户限流接口被调用，用户: {userId}, VIP: {isVip}");

        return Ok(new
        {
            message = $"按用户限流：VIP用户每分钟1000次，普通用户100次",
            userId = userId,
            isVip = isVip,
            limit = isVip ? 1000 : 100,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 无限流：用于健康检查等场景
    /// </summary>
    [HttpGet("health")]
    [DisableRateLimiting]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 模拟高成本操作：生成报告
    /// </summary>
    [HttpGet("report/generate")]
    [EnableRateLimiting("concurrency")]
    public async Task<IActionResult> GenerateReport()
    {
        _logger.LogInformation("开始生成报告");

        // 模拟耗时操作（5秒）
        await Task.Delay(5000);

        _logger.LogInformation("报告生成完成");

        return Ok(new
        {
            message = "报告生成成功",
            timestamp = DateTime.UtcNow
        });
    }
}
