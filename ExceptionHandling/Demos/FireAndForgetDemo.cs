using ExceptionHandling.Extensions;

namespace ExceptionHandling.Demos
{
    /// <summary>
    /// 演示后台任务（Fire-and-Forget）的异常处理
    /// </summary>
    public static class FireAndForgetDemo
    {
        public static async Task DemoFireAndForgetAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例7：后台任务异常处理（Fire-and-Forget）");
            Console.WriteLine("========================================\n");

            // 错误示例1：异常会被吞掉
            Console.WriteLine("【错误示例】异常会被吞掉:");
            _ = BadFireAndForgetAsync();
            await Task.Delay(1500);
            Console.WriteLine("   ⚠️ 你看不到任何异常信息，但任务实际上已经失败了！\n");

            // 正确示例1：使用 SafeFireAndForget
            Console.WriteLine("【正确示例1】使用 SafeFireAndForget:");
            GoodFireAndForgetAsync().SafeFireAndForget(ex =>
            {
                Console.WriteLine($"   ✅ 捕获到后台任务异常: {ex.Message}");
            });
            await Task.Delay(1500);

            Console.WriteLine();

            // 正确示例2：TaskScheduler.UnobservedTaskException
            Console.WriteLine("【正确示例2】使用全局异常处理器:");
            Console.WriteLine("   (需要在程序启动时注册 TaskScheduler.UnobservedTaskException)\n");

            // 正确示例3：BackgroundService（只展示说明）
            Console.WriteLine("【正确示例3】使用 BackgroundService:");
            Console.WriteLine("   适用于长期运行的后台任务（如定时任务、消息队列消费者）");
            Console.WriteLine("   可以在 ExecuteAsync 中捕获异常，服务继续运行\n");

            Console.WriteLine("📌 总结:");
            Console.WriteLine("   • 后台任务的异常容易被吞掉");
            Console.WriteLine("   • 使用 SafeFireAndForget 扩展方法");
            Console.WriteLine("   • 或使用 BackgroundService（.NET Core）");
            Console.WriteLine("   • 注册全局的 UnobservedTaskException 处理器");
        }

        /// <summary>
        /// 错误示例：异常会被吞掉
        /// </summary>
        private static async Task BadFireAndForgetAsync()
        {
            await Task.Delay(1000);
            throw new InvalidOperationException("这个异常会被吞掉");
        }

        /// <summary>
        /// 正确示例：使用 SafeFireAndForget
        /// </summary>
        private static async Task GoodFireAndForgetAsync()
        {
            await Task.Delay(1000);
            throw new InvalidOperationException("这个异常会被正确捕获");
        }

        /// <summary>
        /// 演示 TaskScheduler.UnobservedTaskException
        /// </summary>
        public static void RegisterGlobalExceptionHandler()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.WriteLine($"❌ 未观察到的异常: {e.Exception.Message}");

                // 标记为已观察，防止程序崩溃
                e.SetObserved();
            };

            Console.WriteLine("✅ 已注册全局异常处理器");
        }
    }

    /// <summary>
    /// BackgroundService 示例（仅供参考，不运行）
    /// </summary>
    /// <remarks>
    /// 在实际项目中，使用 Microsoft.Extensions.Hosting.BackgroundService
    /// </remarks>
    public class ExampleBackgroundService
    {
        private readonly CancellationTokenSource _cts = new();

        public async Task StartAsync()
        {
            Console.WriteLine("后台服务启动");

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    // ✅ 异常会被记录，服务继续运行
                    Console.WriteLine($"❌ 后台任务执行失败: {ex.Message}");

                    // 等待一段时间后重试
                    await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                }
            }
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            // 业务逻辑
            await Task.Delay(5000, cancellationToken);
        }

        public void Stop()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
