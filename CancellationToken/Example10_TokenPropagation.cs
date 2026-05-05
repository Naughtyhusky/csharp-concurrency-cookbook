using System.Net.Http;

namespace CancellationToken;

/// <summary>
/// 示例10：CancellationToken 的传递性 - 为什么必须一路传递
/// 
/// 博客章节：第 06 章 - 2.4 传递性的重要性
/// 
/// 学习目标：
/// 1. 理解不传递 Token 的严重后果
/// 2. 掌握正确的传递模式
/// 3. 学会在多层调用中保持传递链
/// 
/// 关键概念：
/// - Token 必须从顶层传递到底层
/// - 任何一个环节不传递，后续操作都无法取消
/// - 这会导致资源泄漏、业务逻辑错误
/// </summary>
public class Example10_TokenPropagation
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例10：CancellationToken 的传递性 ===\n");

        Console.WriteLine("演示1：错误示例 - 不传递 Token 的后果");
        await DemoBadPropagationAsync();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        Console.WriteLine("演示2：正确示例 - 一路传递 Token");
        await DemoGoodPropagationAsync();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        Console.WriteLine("演示3：多层调用中的传递链");
        await DemoMultiLevelPropagationAsync();
    }

    /// <summary>
    /// 演示1：错误示例 - 不传递 Token 导致的问题
    /// </summary>
    private static async Task DemoBadPropagationAsync()
    {
        Console.WriteLine("场景：处理订单，但忘记传递 Token\n");

        using var cts = new CancellationTokenSource();

        // 3秒后自动取消（模拟用户点击取消）
        Task.Run(async () =>
        {
            await Task.Delay(3000);
            Console.WriteLine("[系统] 用户点击了取消按钮");
            cts.Cancel();
        });

        try
        {
            var badService = new BadOrderService();
            await badService.ProcessOrderAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[结果] 第一层方法已取消");
        }

        // 等待一下，观察后续操作
        await Task.Delay(5000);
        Console.WriteLine("\n[总结] 问题：第一层退出了，但后续操作（支付、发邮件）还在继续！");
    }

    /// <summary>
    /// 演示2：正确示例 - 一路传递 Token
    /// </summary>
    private static async Task DemoGoodPropagationAsync()
    {
        Console.WriteLine("场景：正确传递 Token，所有操作都能取消\n");

        using var cts = new CancellationTokenSource();

        // 2秒后自动取消
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            Console.WriteLine("[系统] 用户点击了取消按钮");
            cts.Cancel();
        });

        try
        {
            var goodService = new GoodOrderService();
            await goodService.ProcessOrderAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[结果] 所有操作都已正确取消");
        }

        Console.WriteLine("\n[总结] 成功：Token 一路传递，所有操作都响应了取消");
    }

    /// <summary>
    /// 演示3：多层调用中的传递链
    /// </summary>
    private static async Task DemoMultiLevelPropagationAsync()
    {
        Console.WriteLine("场景：5层调用，展示完整的传递链\n");

        using var cts = new CancellationTokenSource();

        // 3秒后取消
        Task.Run(async () =>
        {
            await Task.Delay(3000);
            Console.WriteLine("\n[系统] 触发取消");
            cts.Cancel();
        });

        try
        {
            await Level1_WebAPIAsync("订单123", cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[结果] 取消信号成功从第1层传递到第5层");
        }

        Console.WriteLine("\n[总结] 完整的传递链保证了任意层级都能响应取消");
    }

    // ==================== 多层调用示例 ====================

    /// <summary>
    /// 第1层：Web API 控制器
    /// </summary>
    private static async Task Level1_WebAPIAsync(string orderId, System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[Layer 1] Web API 接收到请求");
        // 传递给第2层
        await Level2_ServiceAsync(orderId, cancellationToken);
        Console.WriteLine("[Layer 1] Web API 返回响应");
    }

    /// <summary>
    /// 第2层：业务服务层
    /// </summary>
    private static async Task Level2_ServiceAsync(string orderId, System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[Layer 2] 业务服务处理订单");
        // 传递给第3层
        await Level3_RepositoryAsync(orderId, cancellationToken);
        Console.WriteLine("[Layer 2] 业务服务完成");
    }

    /// <summary>
    /// 第3层：数据访问层
    /// </summary>
    private static async Task Level3_RepositoryAsync(string orderId, System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[Layer 3] 数据访问层查询数据库");
        // 传递给第4层
        await Level4_DatabaseAsync(cancellationToken);
        Console.WriteLine("[Layer 3] 数据访问层完成");
    }

    /// <summary>
    /// 第4层：数据库操作（模拟）
    /// </summary>
    private static async Task Level4_DatabaseAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[Layer 4] 数据库执行查询（模拟长时间操作）");
        // 传递给第5层（Task.Delay）
        await Level5_IOOperationAsync(cancellationToken);
        Console.WriteLine("[Layer 4] 数据库查询完成");
    }

    /// <summary>
    /// 第5层：底层 I/O 操作
    /// </summary>
    private static async Task Level5_IOOperationAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[Layer 5] 执行 I/O 操作（等待10秒）...");
        // 最底层：传递给 Task.Delay
        await Task.Delay(10000, cancellationToken);  // 如果不传递，无法取消！
        Console.WriteLine("[Layer 5] I/O 操作完成");
    }
}

// ==================== 错误示例：不传递 Token ====================

/// <summary>
/// ❌ 错误的订单服务：忘记传递 CancellationToken
/// </summary>
public class BadOrderService
{
    public async Task ProcessOrderAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[第1层] 开始处理订单");

        // 检查了 Token ✅
        cancellationToken.ThrowIfCancellationRequested();

        Console.WriteLine("[第2层] 调用支付服务...");
        // ❌ 错误：没有传递 cancellationToken
        await ChargePaymentAsync();  // 缺少 cancellationToken 参数

        Console.WriteLine("[第3层] 发送确认邮件...");
        // ❌ 错误：也没有传递
        await SendEmailAsync();  // 缺少 cancellationToken 参数

        Console.WriteLine("[完成] 订单处理完成");
    }

    // ❌ 这个方法没有 CancellationToken 参数
    private async Task ChargePaymentAsync()
    {
        Console.WriteLine("  [支付] 开始扣款...");
        await Task.Delay(4000);  // ❌ 无法取消！即使用户点了取消，还是会等4秒
        Console.WriteLine("  [支付] 扣款成功（💥 用户明明取消了，但还是扣款了！）");
    }

    // ❌ 这个方法也没有 CancellationToken 参数
    private async Task SendEmailAsync()
    {
        Console.WriteLine("  [邮件] 发送确认邮件...");
        await Task.Delay(2000);  // ❌ 无法取消
        Console.WriteLine("  [邮件] 邮件发送成功（💥 用户收到了邮件，但他明明取消了！）");
    }
}

// ==================== 正确示例：一路传递 Token ====================

/// <summary>
/// ✅ 正确的订单服务：CancellationToken 一路传递
/// </summary>
public class GoodOrderService
{
    public async Task ProcessOrderAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("[第1层] 开始处理订单");

        // 检查 Token
        cancellationToken.ThrowIfCancellationRequested();

        Console.WriteLine("[第2层] 调用支付服务...");
        // ✅ 正确：传递 cancellationToken
        await ChargePaymentAsync(cancellationToken);

        Console.WriteLine("[第3层] 发送确认邮件...");
        // ✅ 正确：也传递了
        await SendEmailAsync(cancellationToken);

        Console.WriteLine("[完成] 订单处理完成");
    }

    // ✅ 正确：接受 CancellationToken 参数
    private async Task ChargePaymentAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("  [支付] 开始扣款...");
        // ✅ 传递给 Task.Delay
        await Task.Delay(4000, cancellationToken);  // 可以取消！
        Console.WriteLine("  [支付] 扣款成功");
    }

    // ✅ 正确：接受 CancellationToken 参数
    private async Task SendEmailAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("  [邮件] 发送确认邮件...");
        // ✅ 传递给 Task.Delay
        await Task.Delay(2000, cancellationToken);  // 可以取消！
        Console.WriteLine("  [邮件] 邮件发送成功");
    }
}
