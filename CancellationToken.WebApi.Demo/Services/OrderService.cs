using CancellationToken.WebApi.Demo.Models;

namespace CancellationToken.WebApi.Demo.Services;

/// <summary>
/// 订单服务接口
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 创建订单（包含支付流程）
    /// </summary>
    Task<OrderResponse> CreateOrderAsync(
        CreateOrderRequest request, 
        System.Threading.CancellationToken cancellationToken);
}

/// <summary>
/// 订单服务实现
/// 
/// 演示场景：
/// 1. 用户提交订单
/// 2. 系统处理：验证 → 扣库存 → 调用支付接口 → 更新状态
/// 3. 支付接口可能需要 5-30 秒（第三方服务）
/// 4. 如果超过 10 秒，应该自动超时
/// 5. 用户可以手动取消订单
/// 
/// 关键点：
/// - 使用 LinkedTokenSource 组合用户取消 + 超时
/// - 在关键步骤检查取消（如：调用支付前）
/// - 取消时回滚已执行的操作（如：恢复库存）
/// - 区分取消原因（用户取消 vs 超时）
/// </summary>
public class OrderService(ILogger<OrderService> logger) : IOrderService
{
    private readonly ILogger<OrderService> _logger = logger;
    private static int _orderIdCounter = 1000;

    public async Task<OrderResponse> CreateOrderAsync(
        CreateOrderRequest request, 
        System.Threading.CancellationToken cancellationToken)
    {
        var orderId = Interlocked.Increment(ref _orderIdCounter);
        var orderNo = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{orderId}";

        _logger.LogInformation("开始创建订单：{OrderNo}", orderNo);

        // 创建订单实体
        var order = new Order
        {
            Id = orderId,
            OrderNo = orderNo,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items,
            TotalAmount = request.Items.Sum(i => i.Price * i.Quantity)
        };

        try
        {
            // 🔥 关键点1：在开始前检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 步骤1：验证订单
            _logger.LogInformation("[{OrderNo}] 步骤1：验证订单...", orderNo);
            await Task.Delay(500, cancellationToken);

            if (order.Items.Count == 0)
            {
                return new OrderResponse
                {
                    Success = false,
                    Message = "订单明细不能为空"
                };
            }

            // 🔥 关键点2：关键步骤前检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 步骤2：扣减库存
            _logger.LogInformation("[{OrderNo}] 步骤2：扣减库存...", orderNo);
            await Task.Delay(800, cancellationToken);
            order.Status = OrderStatus.Processing;

            // 🔥 关键点3：长时间操作前检查
            cancellationToken.ThrowIfCancellationRequested();

            // 步骤3：调用支付接口（长时间操作）
            _logger.LogInformation(
                "[{OrderNo}] 步骤3：调用支付接口（延迟 {Delay}ms）...", 
                orderNo, 
                request.SimulatedPaymentDelayMs);

            order.Status = OrderStatus.PaymentProcessing;

            // 🔥 关键点4：传递 CancellationToken 给长时间操作
            await ProcessPaymentAsync(order, request.SimulatedPaymentDelayMs, cancellationToken);

            // 步骤4：更新订单状态
            _logger.LogInformation("[{OrderNo}] 步骤4：更新订单状态...", orderNo);
            await Task.Delay(300, cancellationToken);

            order.Status = OrderStatus.Completed;
            order.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("[{OrderNo}] 订单创建成功", orderNo);

            return new OrderResponse
            {
                Success = true,
                Message = "订单创建成功",
                Order = order
            };
        }
        catch (OperationCanceledException)
        {
            // 🔥 关键点5：取消时执行清理逻辑
            _logger.LogWarning("[{OrderNo}] 订单创建已取消，正在回滚...", orderNo);

            // 回滚操作：恢复库存、取消支付等
            await RollbackOrderAsync(order);

            order.Status = OrderStatus.Cancelled;

            return new OrderResponse
            {
                Success = false,
                Message = "订单已取消",
                Order = order
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{OrderNo}] 订单创建失败", orderNo);

            order.Status = OrderStatus.Failed;

            return new OrderResponse
            {
                Success = false,
                Message = $"订单创建失败：{ex.Message}",
                Order = order
            };
        }
    }

    /// <summary>
    /// 处理支付（模拟调用第三方支付接口）
    /// </summary>
    private async Task ProcessPaymentAsync(
        Order order, 
        int delayMs, 
        System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{OrderNo}] 正在处理支付，金额：{Amount}", order.OrderNo, order.TotalAmount);

        // 模拟调用第三方支付接口（如：微信支付、支付宝）
        // 真实场景中，这里会使用 HttpClient 发送 HTTP 请求
        await Task.Delay(delayMs, cancellationToken);

        _logger.LogInformation("[{OrderNo}] 支付成功", order.OrderNo);
    }

    /// <summary>
    /// 回滚订单（恢复库存、取消支付等）
    /// </summary>
    private async Task RollbackOrderAsync(Order order)
    {
        _logger.LogInformation("[{OrderNo}] 开始回滚操作...", order.OrderNo);

        // 模拟回滚操作
        await Task.Delay(200);

        _logger.LogInformation("[{OrderNo}] 回滚完成", order.OrderNo);
    }
}
