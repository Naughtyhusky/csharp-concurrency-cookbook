namespace CancellationToken.WebApi.Demo.Models;

/// <summary>
/// 订单实体
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// 订单状态
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    PaymentProcessing = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}

/// <summary>
/// 订单明细
/// </summary>
public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// 创建订单请求
/// </summary>
public class CreateOrderRequest
{
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>
    /// 模拟支付延迟（毫秒）
    /// 用于演示：设置较大的值（如30秒）可以测试超时
    /// </summary>
    public int SimulatedPaymentDelayMs { get; set; } = 5000;
}

/// <summary>
/// 订单响应
/// </summary>
public class OrderResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Order? Order { get; set; }
}
