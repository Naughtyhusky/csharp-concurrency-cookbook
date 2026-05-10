namespace ExceptionHandling.Demos
{
    /// <summary>
    /// 演示 Task.WhenAll 的异常处理问题和解决方案
    /// </summary>
    public static class WhenAllExceptionHandling
    {
        /// <summary>
        /// 问题演示：WhenAll 只抛出第一个异常
        /// </summary>
        public static async Task DemoWhenAllFirstExceptionAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例2：Task.WhenAll 只抛出第一个异常");
            Console.WriteLine("========================================\n");

            var tasks = new[]
            {
                SimulateApiCallAsync(1, success: true),   // ✅ 成功
                SimulateApiCallAsync(2, success: false),  // ❌ 失败：404
                SimulateApiCallAsync(3, success: true),   // ✅ 成功
                SimulateApiCallAsync(4, success: false),  // ❌ 失败：500
                SimulateApiCallAsync(5, success: false),  // ❌ 失败：Timeout
            };

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 捕获异常: {ex.GetType().Name}");
                Console.WriteLine($"   消息: {ex.Message}");
                Console.WriteLine($"\n⚠️ 问题：只能看到第一个异常，其他失败信息丢失！");
            }
        }

        /// <summary>
        /// 解决方案1：手动检查 Task.Exception
        /// </summary>
        public static async Task DemoManualCheckExceptionAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例3：解决方案1 - 手动检查 Task.Exception");
            Console.WriteLine("========================================\n");

            var tasks = new[]
            {
                SimulateApiCallAsync(1, success: true),
                SimulateApiCallAsync(2, success: false),
                SimulateApiCallAsync(3, success: true),
                SimulateApiCallAsync(4, success: false),
                SimulateApiCallAsync(5, success: false),
            };

            var whenAllTask = Task.WhenAll(tasks);

            try
            {
                await whenAllTask;
                Console.WriteLine("✅ 所有任务成功完成");
            }
            catch (Exception firstEx)
            {
                Console.WriteLine($"第一个异常: {firstEx.Message}\n");

                // 从 Task.Exception 获取所有异常
                if (whenAllTask.Exception != null)
                {
                    Console.WriteLine($"总共 {whenAllTask.Exception.InnerExceptions.Count} 个任务失败:");

                    foreach (var ex in whenAllTask.Exception.InnerExceptions)
                    {
                        Console.WriteLine($"   - {ex.Message}");
                    }
                }
            }

            Console.WriteLine("\n✅ 优点：可以获取所有异常信息");
            Console.WriteLine("❌ 缺点：代码略显冗长");
        }

        /// <summary>
        /// 解决方案2：逐个 await
        /// </summary>
        public static async Task DemoAwaitIndividuallyAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例4：解决方案2 - 逐个 await");
            Console.WriteLine("========================================\n");

            var tasks = new[]
            {
                SimulateApiCallAsync(1, success: true),
                SimulateApiCallAsync(2, success: false),
                SimulateApiCallAsync(3, success: true),
                SimulateApiCallAsync(4, success: false),
                SimulateApiCallAsync(5, success: false),
            };

            // 先启动所有任务（并发执行）
            _ = Task.WhenAll(tasks);

            Console.WriteLine("逐个检查任务结果:\n");

            // 逐个 await，捕获每个任务的异常
            int successCount = 0;
            int failureCount = 0;

            for (int i = 0; i < tasks.Length; i++)
            {
                try
                {
                    await tasks[i];
                    Console.WriteLine($"✅ 任务 {i + 1} 成功");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 任务 {i + 1} 失败: {ex.Message}");
                    failureCount++;
                }
            }

            Console.WriteLine($"\n总结: ✅ 成功 {successCount} 个, ❌ 失败 {failureCount} 个");

            Console.WriteLine("\n✅ 优点：可以单独处理每个任务的异常，代码清晰");
            Console.WriteLine("✅ 优点：已完成的任务会立即返回，不会重新执行");
        }

        /// <summary>
        /// 模拟 API 调用
        /// </summary>
        private static async Task<string> SimulateApiCallAsync(int id, bool success)
        {
            await Task.Delay(Random.Shared.Next(100, 500));

            if (!success)
            {
                var errorMessages = new[]
                {
                    "Response status code does not indicate success: 404 (Not Found)",
                    "Response status code does not indicate success: 500 (Internal Server Error)",
                    "The request was canceled due to the configured HttpClient.Timeout"
                };

                throw new HttpRequestException(errorMessages[id % errorMessages.Length]);
            }

            return $"API {id} 数据";
        }
    }
}
