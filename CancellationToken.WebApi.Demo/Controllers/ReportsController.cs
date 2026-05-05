using CancellationToken.WebApi.Demo.Models;
using CancellationToken.WebApi.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CancellationToken.WebApi.Demo.Controllers;

/// <summary>
/// 报表控制器
/// 
/// 演示场景：CPU 密集型任务 + 取消
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController(
    IReportService reportService,
    ILogger<ReportsController> logger) : ControllerBase
{
    private readonly IReportService _reportService = reportService;
    private readonly ILogger<ReportsController> _logger = logger;

    /// <summary>
    /// 生成报表（CPU 密集型任务）
    /// </summary>
    /// <remarks>
    /// 演示场景：
    /// 1. 用户请求生成销售报表（需要分析大量数据）
    /// 2. 报表生成可能需要几十秒（CPU 密集型）
    /// 3. 用户可以取消报表生成
    /// 
    /// 测试方法：
    /// 
    /// 测试1：正常生成
    /// POST /api/reports/generate
    /// {
    ///   "reportType": "Sales",
    ///   "startDate": "2024-01-01",
    ///   "endDate": "2024-12-31",
    ///   "simulatedDelayMs": 5000
    /// }
    /// 预期：5秒后返回报表数据
    /// 
    /// 测试2：用户取消
    /// POST /api/reports/generate (设置 20 秒延迟)
    /// 在 20 秒内取消请求
    /// 预期：立即停止，日志显示"报表生成已取消"
    /// 
    /// 测试3：观察进度
    /// POST /api/reports/generate (设置 30 秒延迟)
    /// 观察日志输出：应该看到进度（10%, 20%, ...）
    /// </remarks>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ApiResponse<ReportData>), 200)]
    public async Task<IActionResult> Generate(
        [FromBody] ReportRequest request,
        System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "收到生成报表请求：Type={Type}, Period={Start}~{End}", 
                request.ReportType, 
                request.StartDate.ToString("yyyy-MM-dd"), 
                request.EndDate.ToString("yyyy-MM-dd"));

            // 🔥 关键点：传递 CancellationToken 给 CPU 密集型任务
            var report = await _reportService.GenerateReportAsync(request, cancellationToken);

            return Ok(ApiResponse<ReportData>.SuccessResult(report, "报表生成成功"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("报表生成被取消");
            return StatusCode(499, ApiResponse<object>.CancelledResult("报表生成已取消"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报表生成失败");
            return StatusCode(500, ApiResponse<object>.FailResult("报表生成失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 生成多个报表（并发任务）
    /// </summary>
    /// <remarks>
    /// 演示场景：
    /// 1. 同时生成多个报表（销售、库存、客户）
    /// 2. 所有报表共享同一个 CancellationToken
    /// 3. 取消时，所有报表生成都停止
    /// 
    /// 测试方法：
    /// POST /api/reports/generate-multiple
    /// [
    ///   { "reportType": "Sales", ... },
    ///   { "reportType": "Inventory", ... },
    ///   { "reportType": "Customer", ... }
    /// ]
    /// 在处理过程中取消请求
    /// 预期：所有报表生成都停止
    /// </remarks>
    [HttpPost("generate-multiple")]
    [ProducesResponseType(typeof(ApiResponse<List<ReportData>>), 200)]
    public async Task<IActionResult> GenerateMultiple(
        [FromBody] List<ReportRequest> requests,
        System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到生成多个报表请求，共 {Count} 个", requests.Count);

            // 🔥 关键点：所有任务共享同一个 CancellationToken
            var tasks = requests.Select(request => 
                _reportService.GenerateReportAsync(request, cancellationToken)
            ).ToList();

            // 等待所有任务完成
            var results = await Task.WhenAll(tasks);

            return Ok(ApiResponse<List<ReportData>>.SuccessResult(
                [.. results], 
                $"成功生成 {results.Length} 个报表"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("批量报表生成被取消");
            return StatusCode(499, ApiResponse<object>.CancelledResult("批量报表生成已取消"));
        }
    }
}
