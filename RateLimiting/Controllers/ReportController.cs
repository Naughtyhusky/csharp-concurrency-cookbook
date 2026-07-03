using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimiting.Controllers;

/// <summary>
/// 实战案例 2：保护高成本接口
/// 限制报告生成接口的并发数，避免系统过载
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportController(ILogger<ReportController> logger) : ControllerBase
{
    private readonly ILogger<ReportController> _logger = logger;

    /// <summary>
    /// 生成销售报告 - 高成本操作
    /// 限制最多 5 个并发请求
    /// </summary>
    [HttpPost("generate-sales")]
    [EnableRateLimiting("report")]
    public async Task<IActionResult> GenerateSalesReport([FromBody] ReportRequest request)
    {
        _logger.LogInformation($"开始生成销售报告：{request.StartDate} ~ {request.EndDate}");

        // 模拟耗时的报告生成过程（5-10秒）
        var processingTime = Random.Shared.Next(5000, 10000);
        await Task.Delay(processingTime);

        _logger.LogInformation($"销售报告生成完成，耗时 {processingTime}ms");

        return Ok(new
        {
            success = true,
            message = "报告生成成功",
            reportId = Guid.NewGuid().ToString("N"),
            processingTime = processingTime,
            data = new
            {
                totalSales = Random.Shared.Next(100000, 1000000),
                orderCount = Random.Shared.Next(1000, 10000),
                period = $"{request.StartDate:yyyy-MM-dd} ~ {request.EndDate:yyyy-MM-dd}"
            }
        });
    }

    /// <summary>
    /// 生成用户报告 - 高成本操作
    /// 限制最多 5 个并发请求
    /// </summary>
    [HttpPost("generate-users")]
    [EnableRateLimiting("report")]
    public async Task<IActionResult> GenerateUserReport()
    {
        _logger.LogInformation("开始生成用户报告");

        // 模拟耗时操作
        await Task.Delay(Random.Shared.Next(3000, 6000));

        return Ok(new
        {
            success = true,
            message = "用户报告生成成功",
            reportId = Guid.NewGuid().ToString("N"),
            data = new
            {
                totalUsers = Random.Shared.Next(10000, 100000),
                activeUsers = Random.Shared.Next(5000, 50000)
            }
        });
    }

    /// <summary>
    /// 查询报告生成状态
    /// 不限流，允许用户随时查询
    /// </summary>
    [HttpGet("{reportId}/status")]
    [DisableRateLimiting]
    public IActionResult GetReportStatus(string reportId)
    {
        return Ok(new
        {
            reportId,
            status = "completed",
            message = "报告已生成完成"
        });
    }
}

public record ReportRequest(DateTime StartDate, DateTime EndDate);
