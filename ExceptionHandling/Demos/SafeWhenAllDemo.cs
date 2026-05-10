using ExceptionHandling.Extensions;

namespace ExceptionHandling.Demos
{
    /// <summary>
    /// 演示 SafeWhenAll 扩展方法的使用
    /// </summary>
    public static class SafeWhenAllDemo
    {
        public static async Task DemoSafeWhenAllAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例5：解决方案3 - SafeWhenAll 扩展方法");
            Console.WriteLine("========================================\n");

            var tasks = new[]
            {
                SimulateApiCallAsync(1, success: true),
                SimulateApiCallAsync(2, success: false),
                SimulateApiCallAsync(3, success: true),
                SimulateApiCallAsync(4, success: false),
                SimulateApiCallAsync(5, success: false),
            };

            // 使用 SafeWhenAll 扩展方法
            var (successes, failures) = await tasks.SafeWhenAll();

            Console.WriteLine($"✅ 成功: {successes.Count} 个");
            Console.WriteLine($"❌ 失败: {failures.Count} 个\n");

            if (successes.Any())
            {
                Console.WriteLine("成功的结果:");
                foreach (var result in successes)
                {
                    Console.WriteLine($"   - {result}");
                }
                Console.WriteLine();
            }

            if (failures.Any())
            {
                Console.WriteLine("失败详情:");
                foreach (var ex in failures)
                {
                    Console.WriteLine($"   - {ex.GetType().Name}: {ex.Message}");
                }
            }

            Console.WriteLine("\n✅ 优点：封装良好，可复用");
            Console.WriteLine("✅ 优点：同时获取成功和失败的结果");
            Console.WriteLine("✅ 优点：不丢失任何异常信息");
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
