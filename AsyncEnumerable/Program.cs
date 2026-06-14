using System.Diagnostics;

namespace AsyncEnumerable;

/// <summary>
/// IAsyncEnumerable 异步流示例程序
/// 博客对应章节：16-IAsyncEnumerable异步流
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== IAsyncEnumerable 异步流示例 ===\n");

        // 示例1：基础对比 - Task<List<T>> vs IAsyncEnumerable<T>
        await Example1_BasicComparison();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 示例2：GitHub API 分页加载
        // await Example2_GitHubIssueFetcher();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 示例3：取消支持
        await Example3_CancellationSupport();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 示例4：LINQ 操作
        await Example4_LinqOperations();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 示例5：批量处理
        await Example5_BatchProcessing();

        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 示例6：数据库查询示例（模拟）
        await Example6_DatabaseQuery();

        Console.WriteLine("\n=== 所有示例执行完成 ===");
    }

    /// <summary>
    /// 示例1：对比 Task<List<T>> 和 IAsyncEnumerable<T> 的性能差异
    /// </summary>
    static async Task Example1_BasicComparison()
    {
        Console.WriteLine("【示例1】传统方式 vs 流式加载");

        // 方案1：传统方式（一次性加载）
        Console.WriteLine("\n方案1：Task<List<T>>（一次性加载）");
        var sw = Stopwatch.StartNew();
        var data = await GetDataTraditionalAsync();
        Console.WriteLine($"  ⏱️  开始处理第一条数据，已过 {sw.ElapsedMilliseconds}ms");

        int count = 0;
        foreach (var item in data)
        {
            // 模拟处理
            if (count++ < 3)
                Console.WriteLine($"  处理数据: {item}");
        }
        sw.Stop();
        Console.WriteLine($"  ✅ 处理完成，总耗时 {sw.ElapsedMilliseconds}ms，共 {data.Count} 条");

        // 方案2：流式加载
        Console.WriteLine("\n方案2：IAsyncEnumerable<T>（流式加载）");
        sw.Restart();
        bool firstItemProcessed = false;
        count = 0;

        await foreach (var item in GetDataStreamAsync())
        {
            if (!firstItemProcessed)
            {
                Console.WriteLine($"  ⚡ 开始处理第一条数据，仅过 {sw.ElapsedMilliseconds}ms！");
                firstItemProcessed = true;
            }

            // 模拟处理
            if (count++ < 3)
                Console.WriteLine($"  处理数据: {item}");
        }
        sw.Stop();
        Console.WriteLine($"  ✅ 处理完成，总耗时 {sw.ElapsedMilliseconds}ms，共 {count} 条");
    }

    /// <summary>
    /// 传统方式：一次性加载所有数据
    /// </summary>
    static async Task<List<int>> GetDataTraditionalAsync()
    {
        var result = new List<int>();
        for (int page = 1; page <= 5; page++)
        {
            await Task.Delay(200); // 模拟 API 延迟
            for (int i = 1; i <= 10; i++)
                result.Add(page * 10 + i);
        }
        return result;
    }

    /// <summary>
    /// 流式方式：边加载边返回
    /// </summary>
    static async IAsyncEnumerable<int> GetDataStreamAsync()
    {
        for (int page = 1; page <= 5; page++)
        {
            await Task.Delay(200); // 模拟 API 延迟
            for (int i = 1; i <= 10; i++)
                yield return page * 10 + i;
        }
    }

    /// <summary>
    /// 示例2：GitHub API 分页加载（需要配置 token）
    /// </summary>
    static async Task Example2_GitHubIssueFetcher()
    {
        Console.WriteLine("【示例2】GitHub API 分页加载");
        Console.WriteLine("提示：需要配置 GitHub Token 才能运行此示例");

        // 从环境变量读取 token（为了安全，不要硬编码）
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("⚠️  未配置 GITHUB_TOKEN 环境变量，跳过此示例");
            return;
        }

        var fetcher = new GitHubIssueFetcher(token);

        // 示例：获取前 20 条 Issue
        int count = 0;
        await foreach (var issue in fetcher.GetAllIssuesAsync("dotnet", "runtime"))
        {
            Console.WriteLine($"  #{issue.Number}: {issue.Title}");
            if (++count >= 20)
                break;
        }

        Console.WriteLine($"✅ 已处理 {count} 条 Issue");
    }

    /// <summary>
    /// 示例3：取消支持
    /// </summary>
    static async Task Example3_CancellationSupport()
    {
        Console.WriteLine("【示例3】取消支持");

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1)); // 1 秒后自动取消

        try
        {
            int count = 0;
            await foreach (var num in GetNumbersWithCancellationAsync(100, cts.Token))
            {
                if (count++ < 5)
                    Console.WriteLine($"  处理数字: {num}");
            }
            Console.WriteLine($"✅ 全部处理完成，共 {count} 个");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⏱️  操作已取消（1秒超时）");
        }
    }

    /// <summary>
    /// 支持取消的异步流
    /// </summary>
    static async IAsyncEnumerable<int> GetNumbersWithCancellationAsync(
        int count,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken); // 模拟异步操作
            yield return i;
        }
    }

    /// <summary>
    /// 示例4：LINQ 操作（过滤、转换、限制）
    /// </summary>
    static async Task Example4_LinqOperations()
    {
        Console.WriteLine("【示例4】LINQ 操作");

        // 注意：需要安装 System.Linq.Async NuGet 包才能使用完整的 LINQ 扩展
        // 这里演示手动实现的简单 LINQ 操作

        Console.WriteLine("\n  过滤偶数 + 转换 + 取前5个:");
        int count = 0;
        await foreach (var item in GetNumbersAsync(20)
            .Where(x => x % 2 == 0)      // 过滤偶数
            .Select(x => $"数字: {x}")    // 转换为字符串
            .Take(5))                     // 只取前5个
        {
            Console.WriteLine($"    {item}");
            count++;
        }

        Console.WriteLine($"  ✅ 共处理 {count} 条数据");
    }

    static async IAsyncEnumerable<int> GetNumbersAsync(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Delay(50);
            yield return i;
        }
    }

    /// <summary>
    /// 示例5：批量处理
    /// </summary>
    static async Task Example5_BatchProcessing()
    {
        Console.WriteLine("【示例5】批量处理（每10条一批）");

        int batchNum = 0;
        await foreach (var batch in GetNumbersAsync(45).Batch(10))
        {
            Console.WriteLine($"  批次 {++batchNum}: {batch.Count} 条数据 [{string.Join(", ", batch.Take(3))}...]");
        }
    }

    /// <summary>
    /// 示例6：模拟数据库查询（大数据集流式处理）
    /// </summary>
    static async Task Example6_DatabaseQuery()
    {
        Console.WriteLine("【示例6】模拟数据库查询（10万条记录）");

        // 模拟传统方式：一次性加载
        Console.WriteLine("\n  方案1：一次性加载到内存");
        var sw = Stopwatch.StartNew();
        var allUsers = await LoadAllUsersTraditionalAsync();
        Console.WriteLine($"    加载完成: {sw.ElapsedMilliseconds}ms，内存占用: {allUsers.Count} 条记录");

        int processedCount = 0;
        foreach (var user in allUsers)
        {
            await ProcessUserAsync(user);
            processedCount++;
        }
        sw.Stop();
        Console.WriteLine($"    ✅ 处理完成: {sw.ElapsedMilliseconds}ms");

        // 模拟流式方式
        Console.WriteLine("\n  方案2：流式处理（IAsyncEnumerable）");
        sw.Restart();
        processedCount = 0;

        await foreach (var user in LoadAllUsersStreamAsync())
        {
            await ProcessUserAsync(user);
            processedCount++;

            if (processedCount == 1)
                Console.WriteLine($"    首条记录处理完成: {sw.ElapsedMilliseconds}ms");
        }
        sw.Stop();
        Console.WriteLine($"    ✅ 处理完成: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"    📊 总结: 流式方式响应更快，内存占用更低");
    }

    static async Task<List<User>> LoadAllUsersTraditionalAsync()
    {
        var users = new List<User>();

        // 模拟分页加载
        for (int page = 0; page < 100; page++)
        {
            await Task.Delay(10); // 模拟数据库查询
            for (int i = 0; i < 1000; i++)
            {
                users.Add(new User { Id = page * 1000 + i, Name = $"User{page * 1000 + i}" });
            }
        }

        return users;
    }

    static async IAsyncEnumerable<User> LoadAllUsersStreamAsync()
    {
        // 模拟分页加载
        for (int page = 0; page < 100; page++)
        {
            await Task.Delay(10); // 模拟数据库查询

            for (int i = 0; i < 1000; i++)
            {
                yield return new User { Id = page * 1000 + i, Name = $"User{page * 1000 + i}" };
            }
        }
    }

    static async Task ProcessUserAsync(User user)
    {
        // 模拟处理逻辑（非常轻量）
        await Task.Yield();
    }

    class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
