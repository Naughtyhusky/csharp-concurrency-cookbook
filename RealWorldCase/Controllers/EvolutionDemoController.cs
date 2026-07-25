using Microsoft.AspNetCore.Mvc;
using RealWorldCase.Models;
using RealWorldCase.Services;

namespace RealWorldCase.Controllers;

/// <summary>
/// 文件处理系统演进演示 API
/// 演示从 V0(Thread) → V4(生产级) 的每一个步骤
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EvolutionDemoController : ControllerBase
{
    private readonly FileProcessingEvolutions _service;
    private readonly ILogger<EvolutionDemoController> _logger;

    public EvolutionDemoController(
        FileProcessingEvolutions service,
        ILogger<EvolutionDemoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 运行演进演示
    /// 可选参数 version: V0, V1, V2, V3, V4（不传则运行全部）
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<EvolutionDemoResult>> RunDemo(
        [FromBody] EvolutionDemoRequest request)
    {
        if (request.FileCount < 1 || request.FileCount > 500)
            return BadRequest("文件数量应在 1-500 之间");

        if (request.FileContentLines < 10 || request.FileContentLines > 10000)
            return BadRequest("文件行数应在 10-10000 之间");

        _logger.LogInformation(
            "开始演进演示: Version={Version}, Files={Count}, Lines={Lines}",
            request.Version ?? "ALL", request.FileCount, request.FileContentLines);

        var result = await _service.RunEvolutionDemo(
            request.FileCount,
            request.FileContentLines,
            request.Version);

        _service.Cleanup();

        return Ok(result);
    }

    /// <summary>
    /// 快速测试：运行所有版本对比（10个文件，100行/文件）
    /// </summary>
    [HttpPost("quick-test")]
    public async Task<ActionResult<object>> QuickTest()
    {
        var files = 20;
        var lines = 500;

        _logger.LogInformation("快速对比测试: {Count} 文件, {Lines} 行/文件", files, lines);

        var results = new Dictionary<string, object>();

        // V0: Thread 版本
        var v0 = _service.ProcessV0_Thread(files, lines);
        results["V0_Thread"] = new { v0.Success, v0.Fail, ElapsedMs = v0.ElapsedMs };

        // V1: Task 版本
        var v1 = _service.ProcessV1_Task(files, lines);
        results["V1_Task"] = new { v1.Success, v1.Fail, ElapsedMs = v1.ElapsedMs };

        // V2: async/await
        var v2 = await _service.ProcessV2_Async(files, lines);
        results["V2_Async"] = new { v2.Success, v2.Fail, ElapsedMs = v2.ElapsedMs };

        // V3: Parallel
        var v3 = await _service.ProcessV3_Parallel(files, lines);
        results["V3_Parallel"] = new { v3.Success, v3.Fail, ElapsedMs = v3.ElapsedMs };

        // V4: Production
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var v4 = await _service.ProcessV4_Production(files, lines, cts.Token);
        results["V4_Production"] = new { v4.Success, v4.Fail, ElapsedMs = v4.ElapsedMs };

        _service.Cleanup();

        // 计算改善百分比
        var v0Ms = (double)v0.ElapsedMs;
        var improvements = new Dictionary<string, string>();
        foreach (var (key, value) in results)
        {
            var elapsed = ((dynamic)value).ElapsedMs;
            var pct = v0Ms > 0 ? ((v0Ms - (double)elapsed) / v0Ms * 100).ToString("F1") + "%" : "N/A";
            improvements[key] = pct;
        }

        return Ok(new
        {
            Results = results,
            Improvements = improvements,
            Summary = $"V0 → V4: {v0.ElapsedMs}ms → {v4.ElapsedMs}ms ({improvements["V4_Production"]} 改善)"
        });
    }

    /// <summary>
    /// 演示中间件请求体读取（发送 POST body 来测试）
    /// </summary>
    [HttpPost("test-middleware")]
    public ActionResult<object> TestMiddleware([FromBody] object body)
    {
        // 这个端点配合不同的中间件，可以观察不同中间件的日志输出
        return Ok(new
        {
            Message = "请求体已收到，请查看日志观察不同中间件的表现",
            ReceivedAt = DateTime.UtcNow,
            RequestLength = Request.ContentLength
        });
    }
}
