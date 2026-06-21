using BackgroundService.Models;
using BackgroundService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackgroundService.Controllers;

/// <summary>
/// 订单控制器
/// 演示如何使用后台队列处理非核心任务
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrderController(
    IBackgroundTaskQueue queue,
    ILogger<OrderController> logger) : ControllerBase
{
    private readonly IBackgroundTaskQueue _queue = queue;
    private readonly ILogger<OrderController> _logger = logger;

    /// <summary>
    /// 创建订单
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderRequest request)
    {
        // 核心业务逻辑（同步）
        _logger.LogInformation("创建订单：{OrderId}", request.OrderId);

        // 发送邮件（后台异步）
        await _queue.QueueAsync(async ct =>
        {
            _logger.LogInformation("开始发送邮件：{Email}", request.Email);
            await Task.Delay(2000, ct); // 模拟发送邮件
            _logger.LogInformation("邮件发送成功：{Email}", request.Email);
        });

        // 推送物流通知（后台异步）
        await _queue.QueueAsync(async ct =>
        {
            _logger.LogInformation("开始推送物流通知：{OrderId}", request.OrderId);
            await Task.Delay(1000, ct); // 模拟推送
            _logger.LogInformation("物流通知推送成功：{OrderId}", request.OrderId);
        });

        return Ok(new { Message = "订单创建成功", OrderId = request.OrderId });
    }

    /// <summary>
    /// 批量创建订单
    /// 演示高并发场景下的队列处理
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatchOrders([FromBody] List<OrderRequest> requests)
    {
        _logger.LogInformation("开始批量创建 {Count} 个订单", requests.Count);

        foreach (var request in requests)
        {
            // 每个订单的邮件发送任务都放入队列
            await _queue.QueueAsync(async ct =>
            {
                _logger.LogInformation("处理订单 {OrderId} 的邮件发送", request.OrderId);
                await Task.Delay(500, ct);
                _logger.LogInformation("订单 {OrderId} 邮件发送完成", request.OrderId);
            });
        }

        return Ok(new { Message = $"批量订单创建成功", Count = requests.Count });
    }
}
