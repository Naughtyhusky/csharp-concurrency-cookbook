namespace CancellationToken;

/// <summary>
/// 示例11：异步编程模式中的 CancellationToken 最佳实践
/// 
/// 博客章节：第 06 章 - 2.5 异步编程模式下的最佳实践
/// 
/// 学习目标：
/// 1. ASP.NET Core 控制器中的使用
/// 2. 后台服务中的优雅停机
/// 3. 并发任务中的共享 Token
/// 4. 组合多个取消源
/// 5. 常见错误模式
/// 
/// 关键概念：
/// - ASP.NET Core 自动注入 CancellationToken
/// - BackgroundService 的 stoppingToken
/// - CreateLinkedTokenSource 组合多个取消源
/// </summary>
public class Example11_AsyncPatterns
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例11：异步编程模式中的最佳实践 ===\n");

        Console.WriteLine("演示1：后台服务模式（模拟 BackgroundService）");
        await DemoBackgroundServiceAsync();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        Console.WriteLine("演示2：并发任务共享 Token");
        await DemoConcurrentTasksAsync();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        Console.WriteLine("演示3：组合多个取消源");
        await DemoLinkedTokenSourceAsync();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        Console.WriteLine("演示4：常见错误模式");
        await DemoCommonMistakesAsync();
    }

    /// <summary>
    /// 演示1：后台服务模式（模拟 BackgroundService 的 ExecuteAsync）
    /// </summary>
    private static async Task DemoBackgroundServiceAsync()
    {
        Console.WriteLine("模拟后台订单处理服务\n");

        using var cts = new CancellationTokenSource();

        // 模拟后台服务
        var serviceTask = Task.Run(async () =>
        {
            await SimulateBackgroundServiceAsync(cts.Token);
        });

        // 5秒后模拟应用关闭
        await Task.Delay(5000);
        Console.WriteLine("\n[系统] 应用正在关闭，发送停止信号...");
        cts.Cancel();

        // 等待服务优雅停止
        try
        {
            await serviceTask;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[系统] 后台服务已优雅停止");
        }
    }

    /// <summary>
    /// 模拟后台服务（类似 BackgroundService.ExecuteAsync）
    /// </summary>
    private static async Task SimulateBackgroundServiceAsync(System.Threading.CancellationToken stoppingToken)
    {
        Console.WriteLine("[服务] 后台服务已启动");
        int batchNumber = 1;

        try
        {
            // 持续运行，直到收到停止信号
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[服务] 处理第 {batchNumber} 批订单...");

                // 模拟处理订单（可取消）
                await Task.Delay(2000, stoppingToken);

                Console.WriteLine($"[服务] 第 {batchNumber} 批订单处理完成");
                batchNumber++;

                // 等待一段时间再处理下一批
                Console.WriteLine("[服务] 等待3秒...");
                await Task.Delay(3000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[服务] 收到停止信号，正在退出...");
        }

        Console.WriteLine("[服务] 后台服务已停止");
    }

    /// <summary>
    /// 演示2：并发任务共享 CancellationToken
    /// </summary>
    private static async Task DemoConcurrentTasksAsync()
    {
        Console.WriteLine("同时下载3个URL，3秒后全部取消\n");

        using var cts = new CancellationTokenSource();

        // 3秒后取消
        Task.Run(async () =>
        {
            await Task.Delay(3000);
            Console.WriteLine("\n[系统] 触发取消，所有下载都会停止");
            cts.Cancel();
        });

        var urls = new List<string>
        {
            "https://example.com/file1.zip",
            "https://example.com/file2.zip",
            "https://example.com/file3.zip"
        };

        // 创建多个任务，共享同一个 Token
        var tasks = urls.Select((url, index) =>
            DownloadFileAsync(index + 1, url, cts.Token)
        ).ToList();

        try
        {
            // 等待所有任务完成（或取消）
            await Task.WhenAll(tasks);
            Console.WriteLine("\n[结果] 所有下载完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[结果] 所有下载已取消");
        }
    }

    /// <summary>
    /// 模拟下载文件
    /// </summary>
    private static async Task DownloadFileAsync(int fileNumber, string url, System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[文件{fileNumber}] 开始下载...");

            // 模拟下载（10秒）
            await Task.Delay(10000, cancellationToken);

            Console.WriteLine($"[文件{fileNumber}] 下载完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[文件{fileNumber}] 下载已取消");
            throw;
        }
    }

    /// <summary>
    /// 演示3：组合多个取消源（用户取消 + 超时 + 应用关闭）
    /// </summary>
    private static async Task DemoLinkedTokenSourceAsync()
    {
        Console.WriteLine("组合三个取消源：用户取消、超时、应用关闭\n");

        // 取消源1：用户手动取消
        using var userCts = new CancellationTokenSource();

        // 取消源2：5秒超时
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // 取消源3：应用关闭（模拟）
        using var appCts = new CancellationTokenSource();

        // 组合三个 Token：任意一个取消都会触发
        using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
            userCts.Token,
            timeoutCts.Token,
            appCts.Token);

        // 2秒后用户点击取消
        Task.Run(async () =>
        {
            await Task.Delay(2000);
            Console.WriteLine("[系统] 用户点击了取消按钮");
            userCts.Cancel();
        });

        try
        {
            Console.WriteLine("开始处理订单（最长5秒超时）...");
            await ProcessOrderAsync(linkedCts.Token);
            Console.WriteLine("订单处理成功");
        }
        catch (OperationCanceledException) when (userCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("\n[结果] 用户取消了操作");
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("\n[结果] 操作超时");
        }
        catch (OperationCanceledException) when (appCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("\n[结果] 应用关闭");
        }
    }

    /// <summary>
    /// 模拟订单处理
    /// </summary>
    private static async Task ProcessOrderAsync(System.Threading.CancellationToken cancellationToken)
    {
        Console.WriteLine("  [步骤1] 验证订单信息...");
        await Task.Delay(1000, cancellationToken);

        Console.WriteLine("  [步骤2] 调用支付接口...");
        await Task.Delay(10000, cancellationToken);  // 模拟长时间操作

        Console.WriteLine("  [步骤3] 更新订单状态...");
        await Task.Delay(500, cancellationToken);
    }

    /// <summary>
    /// 演示4：常见错误模式
    /// </summary>
    private static async Task DemoCommonMistakesAsync()
    {
        Console.WriteLine("展示3个常见错误\n");

        using var cts = new CancellationTokenSource();

        // 1秒后取消
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            cts.Cancel();
        });

        // 错误1：吞掉 OperationCanceledException
        Console.WriteLine("❌ 错误1：吞掉 OperationCanceledException");
        try
        {
            await Task.Delay(5000, cts.Token);
        }
        catch (Exception ex)  // ❌ 不要捕获所有异常
        {
            Console.WriteLine($"  捕获到异常: {ex.GetType().Name}");
            Console.WriteLine("  问题：OperationCanceledException 被当做普通异常处理了");
        }

        await Task.Delay(500);

        // 重置 Token
        cts.Dispose();
        var cts2 = new CancellationTokenSource();
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            cts2.Cancel();
        });

        // 错误2：忘记传递 Token 给 Task.Delay
        Console.WriteLine("\n❌ 错误2：忘记传递 Token");
        try
        {
            Console.WriteLine("  等待5秒（但无法取消）...");
            await Task.Delay(5000);  // ❌ 缺少 cancellationToken
            Console.WriteLine("  等待完成（💥 即使取消了，还是等了5秒）");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  这行代码永远不会执行");
        }

        cts2.Dispose();

        // 错误3：在 finally 中不传递 Token
        Console.WriteLine("\n❌ 错误3：在清理代码中忽略 Token");
        var cts3 = new CancellationTokenSource();
        Task.Run(async () =>
        {
            await Task.Delay(500);
            cts3.Cancel();
        });

        try
        {
            await Task.Delay(2000, cts3.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  操作已取消");
        }
        finally
        {
            Console.WriteLine("  清理：删除临时文件（模拟等待1秒）...");
            await Task.Delay(1000);  // ❌ 清理操作应该也能取消
            Console.WriteLine("  问题：即使操作已取消，清理代码还是执行了1秒");
        }

        cts3.Dispose();

        Console.WriteLine("\n✅ 正确做法总结：");
        Console.WriteLine("  1. 单独处理 OperationCanceledException，不要吞掉");
        Console.WriteLine("  2. 所有异步操作都传递 CancellationToken");
        Console.WriteLine("  3. 清理代码也应该支持取消（或用 CancellationToken.None）");
    }
}

// ==================== ASP.NET Core 控制器示例（仅供参考）====================

/// <summary>
/// 示例：ASP.NET Core 控制器中的使用
/// 注意：这只是示例代码，不能直接运行（需要 ASP.NET Core 环境）
/// </summary>
public class OrdersControllerExample
{
    /*
    // ✅ ASP.NET Core 会自动注入 CancellationToken
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)  // 自动注入！
    {
        try
        {
            // 场景1：客户端断开连接（用户关闭浏览器）
            // 场景2：请求超时（超过 Kestrel 配置的超时时间）
            // 场景3：应用关闭（IHostApplicationLifetime.ApplicationStopping）

            var order = await _orderService.CreateAsync(request, cancellationToken);
            return Ok(order);
        }
        catch (OperationCanceledException)
        {
            // 记录日志
            _logger.LogInformation("请求已取消");

            // 返回 499 Client Closed Request
            return StatusCode(499, "Client Closed Request");
        }
    }
    */
}

/// <summary>
/// 示例：BackgroundService 中的使用
/// 注意：这只是示例代码，展示正确的模式
/// </summary>
public class OrderProcessingBackgroundServiceExample
{
    /*
    public class OrderProcessingService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("订单处理服务已启动");

            // stoppingToken 会在应用关闭时自动取消
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 处理订单（传递 stoppingToken）
                    await ProcessPendingOrdersAsync(stoppingToken);

                    // 等待5秒（可取消）
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 应用正在关闭
                    _logger.LogInformation("收到停止信号");
                    break;
                }
            }

            _logger.LogInformation("订单处理服务已停止");
        }
    }
    */
}
