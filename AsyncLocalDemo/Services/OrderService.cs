using AsyncLocalDemo.Infrastructure;

namespace AsyncLocalDemo.Services
{
    /// <summary>
    /// 订单 Service —— 演示"零注入 IHttpContextAccessor"方案。
    ///
    /// 传统写法（每个 Service 都要注入）：
    ///   public OrderService(IHttpContextAccessor httpContextAccessor, ILogger&lt;OrderService&gt; logger)
    ///       : base(httpContextAccessor) { }
    ///
    /// 现在只需要业务相关的依赖，用户信息和 TraceId 直接从 AsyncLocal 全局上下文读取：
    ///   public OrderService(ILogger&lt;OrderService&gt; logger) { }
    /// </summary>
    public sealed class OrderService(ILogger<OrderService> logger) : IOrderService
    {
        // 模拟内存数据库
        private static readonly List<OrderDto> _db =
        [
            new("ORD-0001", "user-001", "Alice", "MacBook Pro", 12999m, DateTime.UtcNow.AddDays(-5), "N/A"),
            new("ORD-0002", "user-001", "Alice", "AirPods Pro", 1599m,  DateTime.UtcNow.AddDays(-3), "N/A"),
            new("ORD-0003", "user-002", "Bob",   "iPhone 18",   7999m,  DateTime.UtcNow.AddDays(-1), "N/A"),
        ];

        public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(CancellationToken ct = default)
        {
            // ✅ 直接从 AsyncLocal 上下文读取当前用户，无需注入、无需传参
            var userId = CurrentUser.UserId;
            var traceId = TraceContext.TraceId;

            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("[TraceId={TraceId}] GetMyOrders: 未认证的请求", traceId);
                return [];
            }

            logger.LogInformation(
                "[TraceId={TraceId}] GetMyOrders: 查询用户 {UserId} 的订单",
                traceId, userId);

            // 模拟数据库 I/O（await 之后 AsyncLocal 值依然保持）
            await Task.Delay(10, ct);

            // await 之后，CurrentUser 和 TraceContext 的值仍然正确
            logger.LogInformation(
                "[TraceId={TraceId}] GetMyOrders: await 之后 UserId={UserId} 仍然有效（ExecutionContext 流转的魔法）",
                TraceContext.TraceId, CurrentUser.UserId);

            var result = _db.Where(o => o.UserId == userId).ToList().AsReadOnly();

            logger.LogInformation(
                "[TraceId={TraceId}] GetMyOrders: 查询完成，共 {Count} 条订单",
                TraceContext.TraceId, result.Count);

            return result;
        }

        public async Task<OrderDto> CreateOrderAsync(
            string productName, decimal amount, CancellationToken ct = default)
        {
            // ✅ 同样直接读取 AsyncLocal，无需参数传递
            var userId = CurrentUser.UserId
                ?? throw new UnauthorizedAccessException("必须登录才能创建订单");
            var userName = CurrentUser.UserName;
            var traceId = TraceContext.TraceId;

            logger.LogInformation(
                "[TraceId={TraceId}] CreateOrder: 用户 {UserId}({UserName}) 创建订单，商品={Product}，金额={Amount}",
                traceId, userId, userName, productName, amount);

            // 模拟数据库写入
            await Task.Delay(20, ct);

            var order = new OrderDto(
                OrderId: $"ORD-{Guid.NewGuid():N}"[..12].ToUpper(),
                UserId: userId,
                UserName: userName,
                ProductName: productName,
                Amount: amount,
                CreatedAt: DateTime.UtcNow,
                TraceId: TraceContext.TraceId   // 订单记录也附带 TraceId，方便日后问题排查
            );

            _db.Add(order);

            logger.LogInformation(
                "[TraceId={TraceId}] CreateOrder: 订单创建成功，OrderId={OrderId}",
                TraceContext.TraceId, order.OrderId);

            return order;
        }
    }
}
