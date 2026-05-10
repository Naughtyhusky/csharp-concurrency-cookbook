namespace ExceptionHandling.Demos
{
    /// <summary>
    /// 演示 AggregateException 的高级用法
    /// </summary>
    public static class AggregateExceptionAdvanced
    {
        /// <summary>
        /// 演示 Flatten() 扁平化嵌套异常
        /// </summary>
        public static void DemoFlatten()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例8：AggregateException.Flatten() 用法");
            Console.WriteLine("========================================\n");

            var task1 = Task.Run(() => throw new InvalidOperationException("任务1失败"));
            var task2 = Task.Run(() => throw new ArgumentException("任务2失败"));

            // 嵌套的 Task.WhenAll
            var outerTask = Task.Run(() =>
            {
                Task.WaitAll(task1, task2); // 内层 AggregateException
            });

            try
            {
                outerTask.Wait(); // 外层 AggregateException
            }
            catch (AggregateException aggEx)
            {
                Console.WriteLine("【嵌套的异常结构】:");
                Console.WriteLine($"外层 AggregateException.InnerExceptions 数量: {aggEx.InnerExceptions.Count}");

                if (aggEx.InnerException is AggregateException innerAggEx)
                {
                    Console.WriteLine($"内层 AggregateException.InnerExceptions 数量: {innerAggEx.InnerExceptions.Count}");
                }

                Console.WriteLine("\n【使用 Flatten() 扁平化】:");
                var flattenedEx = aggEx.Flatten();

                Console.WriteLine($"扁平化后的 InnerExceptions 数量: {flattenedEx.InnerExceptions.Count}");

                foreach (var ex in flattenedEx.InnerExceptions)
                {
                    Console.WriteLine($"   - {ex.GetType().Name}: {ex.Message}");
                }

                Console.WriteLine("\n✅ Flatten() 自动处理了嵌套的 AggregateException");
            }
        }

        /// <summary>
        /// 演示 Handle() 选择性处理异常
        /// </summary>
        public static void DemoHandle()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例9：AggregateException.Handle() 用法");
            Console.WriteLine("========================================\n");

            var task1 = Task.Run(() => throw new InvalidOperationException("任务1失败"));
            var task2 = Task.Run(() => throw new TaskCanceledException("任务2被取消"));
            var task3 = Task.Run(() => throw new ArgumentException("任务3失败"));

            try
            {
                Task.WaitAll(task1, task2, task3);
            }
            catch (AggregateException aggEx)
            {
                Console.WriteLine($"捕获了 {aggEx.InnerExceptions.Count} 个异常\n");

                try
                {
                    // 使用 Handle() 选择性处理
                    aggEx.Handle(ex =>
                    {
                        // 如果是 TaskCanceledException，忽略它
                        if (ex is TaskCanceledException)
                        {
                            Console.WriteLine($"✅ 忽略取消异常: {ex.Message}");
                            return true; // 标记为已处理
                        }

                        // 其他异常不处理
                        Console.WriteLine($"❌ 未处理异常: {ex.GetType().Name}: {ex.Message}");
                        return false;
                    });

                    Console.WriteLine("\n所有异常都已处理");
                }
                catch (AggregateException remainingEx)
                {
                    Console.WriteLine($"\n还有 {remainingEx.InnerExceptions.Count} 个未处理的异常");

                    foreach (var ex in remainingEx.InnerExceptions)
                    {
                        Console.WriteLine($"   - {ex.GetType().Name}: {ex.Message}");
                    }
                }

                Console.WriteLine("\n📌 Handle() 的行为:");
                Console.WriteLine("   • 返回 true: 异常被处理，不会重新抛出");
                Console.WriteLine("   • 返回 false: 异常未处理，会重新抛出为新的 AggregateException");
            }
        }

        /// <summary>
        /// 演示实际应用场景：重试逻辑
        /// </summary>
        public static async Task DemoRetryWithHandle()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("实际应用：结合 Handle() 实现重试逻辑");
            Console.WriteLine("========================================\n");

            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var tasks = new[]
                    {
                        SimulateApiCallAsync(1),
                        SimulateApiCallAsync(2),
                        SimulateApiCallAsync(3),
                    };

                    Task.WaitAll(tasks);

                    Console.WriteLine("✅ 所有任务成功");
                    break;
                }
                catch (AggregateException aggEx)
                {
                    retryCount++;

                    aggEx.Handle(ex =>
                    {
                        if (ex is HttpRequestException && retryCount < maxRetries)
                        {
                            Console.WriteLine($"⚠️ 网络错误，第 {retryCount} 次重试");
                            return true; // 可以重试的异常
                        }

                        return false; // 不可重试的异常
                    });
                }
            }
        }

        private static async Task SimulateApiCallAsync(int id)
        {
            await Task.Delay(Random.Shared.Next(100, 500));

            if (Random.Shared.Next(100) < 50)
            {
                throw new HttpRequestException($"API {id} 调用失败");
            }
        }
    }
}
