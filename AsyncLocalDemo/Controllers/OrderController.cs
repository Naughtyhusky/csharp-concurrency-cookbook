using AsyncLocalDemo.Infrastructure;
using AsyncLocalDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsyncLocalDemo.Controllers
{
    /// <summary>
    /// 订单控制器 —— 演示 AsyncLocal 全局上下文在真实 API 中的用法。
    ///
    /// 注意：Controller 层同样不需要注入 IHttpContextAccessor，
    /// 当前用户信息通过 CurrentUser 静态上下文直接获取。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController(IOrderService orderService, ILogger<OrderController> logger) : ControllerBase
    {
        /// <summary>
        /// 获取当前用户的订单列表（需要登录）。
        /// 为演示方便，这里没有加 [Authorize]，匿名访问会返回空列表。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyOrders(CancellationToken ct)
        {
            logger.LogInformation(
                "[TraceId={TraceId}] Controller.GetMyOrders 被调用，当前用户={User}",
                TraceContext.TraceId,
                CurrentUser.UserName);

            var orders = await orderService.GetMyOrdersAsync(ct);
            return Ok(new
            {
                TraceId = TraceContext.TraceId,
                User = CurrentUser.Info,
                Orders = orders
            });
        }

        /// <summary>
        /// 创建订单。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
        {
            logger.LogInformation(
                "[TraceId={TraceId}] Controller.CreateOrder 被调用，Product={Product}",
                TraceContext.TraceId,
                request.ProductName);

            try
            {
                var order = await orderService.CreateOrderAsync(request.ProductName, request.Amount, ct);
                return CreatedAtAction(nameof(GetMyOrders), new
                {
                    TraceId = TraceContext.TraceId,
                    Order = order
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Error = ex.Message, TraceId = TraceContext.TraceId });
            }
        }

        /// <summary>
        /// 查看当前 AsyncLocal 上下文状态（调试用接口）。
        /// 演示在 Controller 层随时可以取到 TraceId 和当前用户信息。
        /// </summary>
        [HttpGet("context")]
        public IActionResult GetCurrentContext()
        {
            return Ok(new
            {
                TraceId = TraceContext.TraceId,
                RequestStartTime = TraceContext.RequestStartTime,
                ElapsedMs = TraceContext.ElapsedMs,
                CurrentUser = new
                {
                    IsAuthenticated = CurrentUser.IsAuthenticated,
                    UserId = CurrentUser.UserId,
                    UserName = CurrentUser.UserName,
                    Info = CurrentUser.Info
                }
            });
        }
    }

    public sealed record CreateOrderRequest(string ProductName, decimal Amount);
}
