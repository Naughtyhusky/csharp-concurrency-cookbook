namespace AsyncLocalDemo.Services
{
    public sealed record OrderDto(
        string OrderId,
        string UserId,
        string UserName,
        string ProductName,
        decimal Amount,
        DateTime CreatedAt,
        string TraceId
    );

    public interface IOrderService
    {
        /// <summary>
        /// 获取当前登录用户的订单列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(CancellationToken ct = default);

        /// <summary>
        /// 创建订单（演示 Service 层直接读取 CurrentUser，无需传参）
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="amount"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OrderDto> CreateOrderAsync(string productName, decimal amount, CancellationToken ct = default);
    }
}
