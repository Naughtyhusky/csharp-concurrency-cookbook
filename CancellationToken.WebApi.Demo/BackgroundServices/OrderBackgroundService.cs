namespace CancellationToken.WebApi.Demo.BackgroundServices;

/// <summary>
/// 订单后台处理服务
/// 
/// 演示场景：
/// 1. 后台服务持续运行，定期处理待处理的订单
/// 2. 应用关闭时（Ctrl+C），服务应该优雅停止
/// 3. 当前正在处理的订单应该完成，而不是强制中断
/// 
/// 关键点：
/// - 使用 BackgroundService 基类
/// - ExecuteAsync 方法接收 stoppingToken
/// - stoppingToken 会在应用关闭时自动取消
/// - 循环中检查 stoppingToken.IsCancellationRequested
/// - 捕获 OperationCanceledException 并优雅退出
/// </summary>
public class OrderBackgroundService(ILogger<OrderBackgroundService> logger) : BackgroundService
{
    private readonly ILogger<OrderBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
    {
        _logger.LogInformation("订单后台处理服务已启动");

        try
        {
            // 🔥 关键点1：使用 while 循环持续运行
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("开始处理待处理订单...");

                    // 🔥 关键点2：传递 stoppingToken 给业务逻辑
                    await ProcessPendingOrdersAsync(stoppingToken);

                    _logger.LogInformation("本批订单处理完成，等待下一批...");

                    // 🔥 关键点3：等待时也传递 stoppingToken
                    // 这样应用关闭时不需要等待完整的 5 秒
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 🔥 关键点4：内层循环的取消异常，继续外层循环
                    // 外层循环会检查 stoppingToken 并退出
                    _logger.LogInformation("当前批次处理已取消");
                }
                catch (Exception ex)
                {
                    // 业务异常不应该导致服务停止
                    _logger.LogError(ex, "处理订单时发生错误");

                    // 等待一段时间后重试
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 🔥 关键点5：外层捕获取消异常，记录日志
            _logger.LogInformation("收到停止信号，正在退出...");
        }
        finally
        {
            _logger.LogInformation("订单后台处理服务已停止");
        }
    }

    /// <summary>
    /// 处理待处理的订单（模拟）
    /// </summary>
    private async Task ProcessPendingOrdersAsync(System.Threading.CancellationToken stoppingToken)
    {
        // 模拟查询待处理订单
        var pendingOrders = GetPendingOrders();

        _logger.LogInformation("发现 {Count} 个待处理订单", pendingOrders.Count);

        foreach (var orderId in pendingOrders)
        {
            // 🔥 关键点：每个订单处理前检查取消
            // 这样可以在应用关闭时，完成当前订单后立即退出
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogInformation("  处理订单 {OrderId}...", orderId);

            // 模拟订单处理（2秒）
            await Task.Delay(2000, stoppingToken);

            _logger.LogInformation("  订单 {OrderId} 处理完成", orderId);
        }
    }

    /// <summary>
    /// 获取待处理订单列表（模拟）
    /// </summary>
    private static List<int> GetPendingOrders() =>
        // 实际场景中，这里会查询数据库
        // 这里模拟返回 3 个待处理订单
        [1001, 1002, 1003];
}
