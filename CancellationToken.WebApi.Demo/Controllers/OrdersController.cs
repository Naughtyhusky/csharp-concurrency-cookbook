using CancellationToken.WebApi.Demo.Models;
using CancellationToken.WebApi.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CancellationToken.WebApi.Demo.Controllers;

/// <summary>
/// 订单控制器
/// 
/// 演示场景：长时间操作 + 超时控制 + 用户取消
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService,ILogger<OrdersController> logger) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private readonly ILogger<OrdersController> _logger = logger;

    /// <summary>
    /// 创建订单（包含支付流程）
    /// </summary>
    /// <remarks>
    /// 演示场景：
    /// 1. 用户提交订单（可能需要 5-30 秒处理）
    /// 2. 系统设置 30 秒超时（防止支付接口长时间无响应）
    /// 3. 用户可以手动取消订单
    /// 4. 取消时会回滚已执行的操作（恢复库存等）
    /// 
    /// 测试方法：
    /// 
    /// 测试1：正常完成
    /// POST /api/orders
    /// { "items": [...], "simulatedPaymentDelayMs": 3000 }
    /// 预期：3秒后返回成功
    /// 
    /// 测试2：超时取消
    /// POST /api/orders
    /// { "items": [...], "simulatedPaymentDelayMs": 35000 }
    /// 预期：30秒后自动超时，返回"订单已取消"
    /// 
    /// 测试3：用户取消
    /// POST /api/orders (设置 10 秒延迟)
    /// 在 10 秒内取消请求
    /// 预期：立即返回"订单已取消"，日志显示回滚操作
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 408)] // 408 = Request Timeout
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到创建订单请求");

            // 🔥 关键点1：创建超时 Token（30秒）
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // 🔥 关键点2：组合用户取消 + 超时
            // 任意一个取消都会触发
            using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,      // 用户取消（客户端断开）
                timeoutCts.Token);      // 超时

            // 🔥 关键点3：传递组合后的 Token
            var result = await _orderService.CreateOrderAsync(request, linkedCts.Token);

            if (result.Success)
            {
                return Ok(ApiResponse<OrderResponse>.SuccessResult(result, "订单创建成功"));
            }
            else
            {
                return BadRequest(ApiResponse<OrderResponse>.FailResult(result.Message));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 🔥 关键点4：使用 when 子句区分取消原因
            _logger.LogWarning("订单创建被用户取消");
            return StatusCode(499, ApiResponse<object>.CancelledResult("订单已取消"));
        }
        catch (OperationCanceledException)
        {
            // 超时取消
            _logger.LogWarning("订单创建超时（30秒）");
            return StatusCode(408, ApiResponse<object>.FailResult("订单处理超时，请稍后重试", "TIMEOUT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建订单失败");
            return StatusCode(500, ApiResponse<object>.FailResult("服务器内部错误", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 批量创建订单（演示并发任务）
    /// </summary>
    /// <remarks>
    /// 演示场景：
    /// 1. 批量创建多个订单
    /// 2. 所有订单共享同一个 CancellationToken
    /// 3. 一旦取消，所有订单都停止
    /// 
    /// 测试方法：
    /// POST /api/orders/batch
    /// { "orders": [{ "items": [...] }, { "items": [...] }] }
    /// 在处理过程中取消请求
    /// 预期：所有订单都停止处理
    /// </remarks>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderResponse>>), 200)]
    public async Task<IActionResult> CreateBatch(
        [FromBody] List<CreateOrderRequest> orders,
        System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("收到批量创建订单请求，共 {Count} 个订单", orders.Count);

            // 🔥 关键点：所有任务共享同一个 CancellationToken
            var tasks = orders.Select(order => 
                _orderService.CreateOrderAsync(order, cancellationToken)
            ).ToList();

            // 等待所有任务完成
            var results = await Task.WhenAll(tasks);

            return Ok(ApiResponse<List<OrderResponse>>.SuccessResult(
                [.. results], 
                $"批量创建完成，成功 {results.Count(r => r.Success)} 个"));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("批量创建订单被取消");
            return StatusCode(499, ApiResponse<object>.CancelledResult("批量创建已取消"));
        }
    }
}
