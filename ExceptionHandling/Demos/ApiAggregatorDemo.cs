using ExceptionHandling.Extensions;
using System.Text.Json;

namespace ExceptionHandling.Demos
{
    /// <summary>
    /// 实战场景：API 聚合器
    /// 并发调用多个 API，支持容错处理
    /// </summary>
    public static class ApiAggregatorDemo
    {
        public static async Task DemoApiAggregatorAsync()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("示例6：实战场景 - API 聚合器");
            Console.WriteLine("========================================\n");

            var aggregator = new ApiAggregator();

            var apiUrls = new[]
            {
                "https://api.example.com/users/1",
                "https://api.example.com/users/2",
                "https://api.example.com/users/3",
                "https://api.example.com/users/4",
                "https://api.example.com/users/5",
            };

            try
            {
                Console.WriteLine("开始并发调用 API...\n");

                var result = await aggregator.GetAggregatedDataAsync(
                    apiUrls,
                    minSuccessCount: 3);

                Console.WriteLine($"\n✅ 成功获取 {result.Successes.Count} 个数据");
                Console.WriteLine($"❌ 失败 {result.FailureCount} 个请求\n");

                if (result.Successes.Any())
                {
                    Console.WriteLine("成功的数据:");
                    foreach (var data in result.Successes)
                    {
                        Console.WriteLine($"   - User ID: {data.UserId}, Name: {data.Name}");
                    }
                }

                if (result.Errors.Any())
                {
                    Console.WriteLine("\n失败的请求:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error}");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"\n❌ {ex.Message}");
            }

            Console.WriteLine("\n📌 这个示例展示了:");
            Console.WriteLine("   • 并发调用多个 API");
            Console.WriteLine("   • 容错处理（允许部分失败）");
            Console.WriteLine("   • 记录所有失败信息");
            Console.WriteLine("   • 满足最低成功数量要求");
        }
    }

    /// <summary>
    /// API 聚合器
    /// </summary>
    public class ApiAggregator
    {
        public async Task<AggregatedResult> GetAggregatedDataAsync(
            IEnumerable<string> apiUrls,
            int minSuccessCount = 1,
            CancellationToken cancellationToken = default)
        {
            var tasks = apiUrls.Select(url => CallApiAsync(url, cancellationToken)).ToList();

            var (successes, failures) = await tasks.SafeWhenAll();

            // 检查是否满足最低成功数量
            if (successes.Count < minSuccessCount)
            {
                throw new InvalidOperationException(
                    $"API 调用失败过多：期望至少 {minSuccessCount} 个成功，实际只有 {successes.Count} 个成功");
            }

            return new AggregatedResult
            {
                Successes = successes,
                FailureCount = failures.Count,
                Errors = failures.Select(ex => ex.Message).ToList()
            };
        }

        private async Task<ApiResponse> CallApiAsync(string url, CancellationToken cancellationToken)
        {
            // 模拟 API 调用
            await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);

            // 模拟随机失败
            if (Random.Shared.Next(100) < 40) // 40% 失败率
            {
                var errorMessages = new[]
                {
                    "Response status code does not indicate success: 404 (Not Found)",
                    "Response status code does not indicate success: 500 (Internal Server Error)",
                    "Connection timeout"
                };

                Console.WriteLine($"❌ API 调用失败: {url}");
                throw new HttpRequestException(errorMessages[Random.Shared.Next(errorMessages.Length)]);
            }

            Console.WriteLine($"✅ API 调用成功: {url}");

            // 提取 User ID
            var userId = int.Parse(url.Split('/').Last());

            return new ApiResponse
            {
                UserId = userId,
                Name = $"User {userId}",
                Email = $"user{userId}@example.com"
            };
        }
    }

    /// <summary>
    /// API 响应模型
    /// </summary>
    public class ApiResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// 聚合结果
    /// </summary>
    public class AggregatedResult
    {
        public List<ApiResponse> Successes { get; set; } = new();
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
