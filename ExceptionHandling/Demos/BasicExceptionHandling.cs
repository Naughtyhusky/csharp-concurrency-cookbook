namespace ExceptionHandling.Demos
{
    /// <summary>
    /// 演示 await vs Wait/Result 的异常行为差异
    /// </summary>
    public static class BasicExceptionHandling
    {
        public static async Task DemoAwaitVsWaitAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例1：await vs Wait/Result 异常行为对比");
            Console.WriteLine("========================================\n");

            // 创建一个会抛出异常的任务
            var task = Task.Run(() =>
            {
                Thread.Sleep(500);
                throw new InvalidOperationException("这是一个模拟的异常");
            });

            // 方式1：使用 await（推荐）
            Console.WriteLine("【方式1】使用 await:");
            try
            {
                await task;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✅ 捕获到原始异常: {ex.GetType().Name}");
                Console.WriteLine($"   消息: {ex.Message}");
            }

            Console.WriteLine();

            // 重新创建任务
            task = Task.Run(() =>
            {
                Thread.Sleep(500);
                throw new InvalidOperationException("这是一个模拟的异常");
            });

            // 方式2：使用 Wait()（不推荐）
            Console.WriteLine("【方式2】使用 Wait():");
            try
            {
                task.Wait();
            }
            catch (AggregateException aggEx)
            {
                Console.WriteLine($"✅ 捕获到 AggregateException");
                Console.WriteLine($"   InnerExceptions 数量: {aggEx.InnerExceptions.Count}");
                Console.WriteLine($"   第一个内部异常: {aggEx.InnerException?.GetType().Name}");
                Console.WriteLine($"   消息: {aggEx.InnerException?.Message}");
            }

            Console.WriteLine();

            // 重新创建任务（带返回值）
            var taskWithResult = Task.Run(async () =>
            {
                await Task.Delay(500);
                return "成功";
            });

            // 方式3：使用 .Result（不推荐）
            Console.WriteLine("【方式3】使用 .Result:");
            try
            {
                string result = taskWithResult.Result;
                Console.WriteLine($"✅ 成功获取结果: {result}");
            }
            catch (AggregateException)
            {
                Console.WriteLine($"✅ 捕获到 AggregateException");
            }

            Console.WriteLine("\n📌 总结:");
            Console.WriteLine("   • await: 直接抛出原始异常，代码简洁");
            Console.WriteLine("   • Wait()/Result: 包装成 AggregateException，需要解包");
            Console.WriteLine("   • 推荐: 优先使用 await");
        }

        /// <summary>
        /// 演示多个异常的情况
        /// </summary>
        public static void DemoMultipleExceptions()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例：多个任务同时失败");
            Console.WriteLine("========================================\n");

            var task1 = Task.Run(() => throw new InvalidOperationException("任务1失败"));
            var task2 = Task.Run(() => throw new ArgumentException("任务2失败"));
            var task3 = Task.Run(() => throw new IOException("任务3失败"));

            try
            {
                Task.WaitAll(task1, task2, task3);
            }
            catch (AggregateException aggEx)
            {
                Console.WriteLine($"✅ 捕获了 {aggEx.InnerExceptions.Count} 个异常:");

                foreach (var ex in aggEx.InnerExceptions)
                {
                    Console.WriteLine($"   - {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}
