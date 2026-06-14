using System.Runtime.CompilerServices;

namespace AsyncEnumerable;

/// <summary>
/// 高级示例：演示常见的生产场景
/// </summary>
public class AdvancedExamples
{
    /// <summary>
    /// 示例1：数据库分页查询（模拟 EF Core 的 AsAsyncEnumerable）
    /// </summary>
    public async IAsyncEnumerable<User> GetUsersFromDatabaseAsync(
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int page = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 模拟数据库查询
            var users = await FetchUserPageAsync(page, pageSize, cancellationToken);

            if (users.Count == 0)
                break; // 没有更多数据

            foreach (var user in users)
            {
                yield return user;
            }

            page++;
        }
    }

    private async Task<List<User>> FetchUserPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        // 模拟数据库延迟
        await Task.Delay(50, cancellationToken);

        var users = new List<User>();
        for (int i = 0; i < pageSize; i++)
        {
            var userId = page * pageSize + i;
            if (userId >= 10000) // 模拟只有 10000 条记录
                break;

            users.Add(new User
            {
                Id = userId,
                Name = $"User{userId}",
                Email = $"user{userId}@example.com",
                IsActive = userId % 3 != 0 // 约 2/3 的用户是活跃的
            });
        }

        return users;
    }

    /// <summary>
    /// 示例2：多数据源合并
    /// </summary>
    public async IAsyncEnumerable<Product> MergeMultipleSourcesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 从多个 API 获取产品列表
        var sources = new[]
        {
            FetchProductsFromApiAsync("api1", cancellationToken),
            FetchProductsFromApiAsync("api2", cancellationToken),
            FetchProductsFromApiAsync("api3", cancellationToken)
        };

        // 逐个从每个数据源获取数据
        foreach (var source in sources)
        {
            await foreach (var product in source)
            {
                yield return product;
            }
        }
    }

    private async IAsyncEnumerable<Product> FetchProductsFromApiAsync(
        string apiName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken); // 模拟 API 延迟

        for (int i = 1; i <= 10; i++)
        {
            yield return new Product
            {
                Id = $"{apiName}-{i}",
                Name = $"Product from {apiName} #{i}",
                Price = 10.0m * i
            };
        }
    }

    /// <summary>
    /// 示例3：数据转换管道（类似 Dataflow 的简化版）
    /// </summary>
    public async IAsyncEnumerable<ProcessedData> DataPipelineAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 阶段1：生成原始数据
        var rawData = GenerateRawDataAsync(cancellationToken);

        // 阶段2：清洗数据
        var cleanedData = CleanDataAsync(rawData, cancellationToken);

        // 阶段3：转换数据
        var transformedData = TransformDataAsync(cleanedData, cancellationToken);

        // 阶段4：聚合数据
        await foreach (var data in transformedData)
        {
            yield return data;
        }
    }

    private async IAsyncEnumerable<string> GenerateRawDataAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= 100; i++)
        {
            await Task.Delay(10, cancellationToken);
            yield return $"RawData-{i}";
        }
    }

    private async IAsyncEnumerable<string> CleanDataAsync(
        IAsyncEnumerable<string> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 模拟清洗逻辑（去除空白、验证格式等）
            var cleaned = item.Trim().ToUpper();
            yield return cleaned;
        }
    }

    private async IAsyncEnumerable<ProcessedData> TransformDataAsync(
        IAsyncEnumerable<string> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 模拟转换逻辑
            await Task.Delay(5, cancellationToken);

            yield return new ProcessedData
            {
                OriginalValue = item,
                ProcessedValue = $"Processed-{item}",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 示例4：错误重试策略（简化版）
    /// 注意：由于 C# 限制，不能在 try-catch 中使用 yield return
    /// 这里提供一个替代方案：收集到 List 再返回
    /// </summary>
    public async Task<List<T>> WithRetryAsync<T>(
        Func<IAsyncEnumerable<T>> sourceFactory,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                var result = new List<T>();
                await foreach (var item in sourceFactory().WithCancellation(cancellationToken))
                {
                    result.Add(item);
                }
                return result; // 成功完成
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;

                if (retryCount <= maxRetries)
                {
                    Console.WriteLine($"⚠️  发生错误，重试第 {retryCount} 次: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                }
            }
        }

        // 达到最大重试次数，抛出异常
        throw lastException ?? new Exception("操作失败");
    }

    /// <summary>
    /// 示例5：限流（控制处理速度）
    /// </summary>
    public async IAsyncEnumerable<T> WithThrottleAsync<T>(
        IAsyncEnumerable<T> source,
        int itemsPerSecond,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var delayBetweenItems = TimeSpan.FromMilliseconds(1000.0 / itemsPerSecond);

        await foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;

            // 限制速度
            await Task.Delay(delayBetweenItems, cancellationToken);
        }
    }

    /// <summary>
    /// 示例6：缓存最近的 N 个元素
    /// </summary>
    public async IAsyncEnumerable<T> WithBufferAsync<T>(
        IAsyncEnumerable<T> source,
        int bufferSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new Queue<T>(bufferSize);

        await foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            buffer.Enqueue(item);

            if (buffer.Count > bufferSize)
                buffer.Dequeue();

            yield return item;
        }
    }
}

/// <summary>
/// 用户数据模型
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// 产品数据模型
/// </summary>
public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// 处理后的数据
/// </summary>
public class ProcessedData
{
    public string OriginalValue { get; set; } = string.Empty;
    public string ProcessedValue { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
