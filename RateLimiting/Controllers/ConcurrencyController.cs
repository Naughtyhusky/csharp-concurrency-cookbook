using Microsoft.AspNetCore.Mvc;
using RateLimiting.Services;

namespace RateLimiting.Controllers;

/// <summary>
/// 演示使用 SemaphoreSlim 进行并发控制
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConcurrencyController(DatabaseService databaseService, ILogger<ConcurrencyController> logger) : ControllerBase
{
    private readonly DatabaseService _databaseService = databaseService;
    private readonly ILogger<ConcurrencyController> _logger = logger;

    /// <summary>
    /// 单个数据库查询（受并发限制）
    /// </summary>
    [HttpGet("query")]
    public async Task<IActionResult> Query([FromQuery] string sql = "SELECT * FROM users")
    {
        try
        {
            var result = await _databaseService.QueryAsync(sql);
            return Ok(new
            {
                result,
                activeConnections = _databaseService.GetActiveConnections()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 批量数据库查询（自动控制并发数）
    /// 测试：发起 50 个查询，但同时最多只有 10 个在执行
    /// </summary>
    [HttpPost("batch-query")]
    public async Task<IActionResult> BatchQuery([FromBody] List<string> sqls)
    {
        if (sqls == null || sqls.Count == 0)
        {
            return BadRequest(new { error = "SQL列表不能为空" });
        }

        try
        {
            var startTime = DateTime.UtcNow;

            var results = await _databaseService.BatchQueryAsync(sqls);

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalSeconds;

            return Ok(new
            {
                totalQueries = sqls.Count,
                duration = duration,
                results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量查询失败");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前活跃的数据库连接数
    /// </summary>
    [HttpGet("active-connections")]
    public IActionResult GetActiveConnections()
    {
        return Ok(new
        {
            activeConnections = _databaseService.GetActiveConnections(),
            maxConnections = 10
        });
    }
}
