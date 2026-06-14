namespace AsyncEnumerable;

/// <summary>
/// 常见陷阱示例：演示使用 IAsyncEnumerable 时容易犯的错误
/// </summary>
public class CommonPitfalls
{
    /// <summary>
    /// 陷阱1：多次遍历会重新执行
    /// </summary>
    public static async Task Pitfall1_MultipleEnumeration()
    {
        Console.WriteLine("【陷阱1】多次遍历会重新执行");

        var stream = GetNumbersAsync(5);

        // 第一次遍历
        Console.WriteLine("  第一次遍历:");
        await foreach (var num in stream)
            Console.WriteLine($"    {num}");

        // 第二次遍历 ❌ 会重新执行整个方法！
        Console.WriteLine("  第二次遍历:");
        await foreach (var num in stream)
            Console.WriteLine($"    {num}");

        Console.WriteLine("  ⚠️  注意：两次遍历都生成了数据，不是从缓存读取！");
        Console.WriteLine();

        // 正确做法：如果需要多次遍历，先转换为 List
        Console.WriteLine("  ✅ 正确做法：先转换为 List");
        var list = await GetNumbersAsync(5).ToListAsync();

        Console.WriteLine("  第一次遍历:");
        foreach (var num in list)
            Console.WriteLine($"    {num}");

        Console.WriteLine("  第二次遍历:");
        foreach (var num in list)
            Console.WriteLine($"    {num}");

        Console.WriteLine("  ✅ List 可以多次遍历，不会重新执行");
    }

    /// <summary>
    /// 陷阱2：在 yield return 之前加载全部数据
    /// </summary>
    public static async Task Pitfall2_LoadAllBeforeYield()
    {
        Console.WriteLine("【陷阱2】在 yield return 之前加载全部数据");

        // ❌ 错误示范
        Console.WriteLine("  ❌ 错误做法（失去了流式的意义）:");
        await foreach (var item in GetDataWrongWayAsync())
        {
            Console.WriteLine($"    处理: {item}");
        }

        Console.WriteLine();

        // ✅ 正确示范
        Console.WriteLine("  ✅ 正确做法（真正的流式处理）:");
        await foreach (var item in GetDataRightWayAsync())
        {
            Console.WriteLine($"    处理: {item}");
        }
    }

    // ❌ 错误：在 yield 之前全部加载
    private static async IAsyncEnumerable<string> GetDataWrongWayAsync()
    {
        Console.WriteLine("    [开始加载所有数据...]");
        var allData = await LoadAllDataAsync(); // 这里就全部加载了！
        Console.WriteLine("    [所有数据加载完成]");

        foreach (var data in allData)
        {
            yield return data; // 失去了"流式"的意义
        }
    }

    // ✅ 正确：边加载边返回
    private static async IAsyncEnumerable<string> GetDataRightWayAsync()
    {
        for (int i = 1; i <= 5; i++)
        {
            Console.WriteLine($"    [加载数据 {i}]");
            await Task.Delay(100); // 模拟异步加载
            yield return $"Data-{i}"; // 立即返回
        }
    }

    private static async Task<List<string>> LoadAllDataAsync()
    {
        await Task.Delay(500); // 模拟加载时间
        return new List<string> { "Data-1", "Data-2", "Data-3", "Data-4", "Data-5" };
    }

    /// <summary>
    /// 陷阱3：忘记 await foreach
    /// </summary>
    public static void Pitfall3_ForgotAwaitForEach()
    {
        Console.WriteLine("【陷阱3】忘记 await foreach");

        // ❌ 错误：忘记 await
        // foreach (var item in GetNumbersAsync(5)) // 编译错误！
        // {
        //     Console.WriteLine(item);
        // }

        Console.WriteLine("  ⚠️  编译错误：foreach 不能直接遍历 IAsyncEnumerable");
        Console.WriteLine("  ✅ 正确做法：使用 await foreach");
    }

    /// <summary>
    /// 陷阱4：没有正确传递 CancellationToken
    /// </summary>
    public static async Task Pitfall4_MissingCancellationToken()
    {
        Console.WriteLine("【陷阱4】没有正确传递 CancellationToken");

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        try
        {
            // ❌ 错误：没有传递 token
            Console.WriteLine("  ❌ 错误做法（无法取消）:");
            await foreach (var num in GetNumbersWithoutCancellationAsync(100))
            {
                Console.WriteLine($"    {num}");
                // 即使 cts 已经取消，这里也不会停止
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  ❌ 不会到达这里，因为没有传递 token");
        }

        Console.WriteLine();

        // 重置 token
        cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        try
        {
            // ✅ 正确：传递 token
            Console.WriteLine("  ✅ 正确做法（支持取消）:");
            await foreach (var num in GetNumbersWithCancellationAsync(100, cts.Token))
            {
                Console.WriteLine($"    {num}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  ✅ 成功取消！");
        }
    }

    // ❌ 错误：没有支持取消
    private static async IAsyncEnumerable<int> GetNumbersWithoutCancellationAsync(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Delay(100);
            yield return i;
        }
    }

    // ✅ 正确：支持取消
    private static async IAsyncEnumerable<int> GetNumbersWithCancellationAsync(
        int count,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            yield return i;
        }
    }

    /// <summary>
    /// 陷阱5：在 yield 块中捕获了大对象
    /// </summary>
    public static async Task Pitfall5_CapturingLargeObjects()
    {
        Console.WriteLine("【陷阱5】在 yield 块中捕获了大对象");

        // ❌ 错误：捕获了大对象
        Console.WriteLine("  ❌ 错误做法（内存泄漏风险）:");
        await foreach (var item in ProcessWithLargeObjectAsync())
        {
            Console.WriteLine($"    {item}");
        }

        Console.WriteLine();

        // ✅ 正确：避免捕获大对象
        Console.WriteLine("  ✅ 正确做法（避免捕获）:");
        await foreach (var item in ProcessWithoutCapturingAsync())
        {
            Console.WriteLine($"    {item}");
        }
    }

    // ❌ 错误：捕获了大对象
    private static async IAsyncEnumerable<string> ProcessWithLargeObjectAsync()
    {
        var largeData = new byte[1024 * 1024 * 10]; // 10 MB
        // 这个数组会一直被引用，直到迭代完成

        for (int i = 1; i <= 5; i++)
        {
            await Task.Delay(100);
            yield return $"Item-{i} (持有 {largeData.Length} 字节)";
        }
        // largeData 在整个迭代期间都不会被 GC
    }

    // ✅ 正确：避免捕获大对象
    private static async IAsyncEnumerable<string> ProcessWithoutCapturingAsync()
    {
        for (int i = 1; i <= 5; i++)
        {
            // 在每次迭代中创建临时数据
            var tempData = new byte[1024 * 1024 * 10]; // 10 MB
            await Task.Delay(100);
            yield return $"Item-{i} (临时数据 {tempData.Length} 字节)";
            // tempData 在 yield 后可以被 GC
        }
    }

    /// <summary>
    /// 陷阱6：异常处理不当
    /// </summary>
    public static async Task Pitfall6_ImproperExceptionHandling()
    {
        Console.WriteLine("【陷阱6】异常处理不当");

        // ❌ 错误：异常会终止整个流
        Console.WriteLine("  ❌ 错误做法（一个失败，全部停止）:");
        try
        {
            await foreach (var item in ProcessItemsWithExceptionAsync())
            {
                Console.WriteLine($"    成功: {item}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 流已中断: {ex.Message}");
        }

        Console.WriteLine();

        // ✅ 正确：在 yield 之前处理异常
        Console.WriteLine("  ✅ 正确做法（跳过失败的项）:");
        await foreach (var item in ProcessItemsWithGracefulHandlingAsync())
        {
            Console.WriteLine($"    成功: {item}");
        }
    }

    // ❌ 错误：异常会终止流
    private static async IAsyncEnumerable<string> ProcessItemsWithExceptionAsync()
    {
        for (int i = 1; i <= 5; i++)
        {
            await Task.Delay(50);

            if (i == 3)
                throw new Exception("处理失败！"); // 整个流中断

            yield return $"Item-{i}";
        }
    }

    // ✅ 正确：处理异常，继续流
    private static async IAsyncEnumerable<string> ProcessItemsWithGracefulHandlingAsync()
    {
        for (int i = 1; i <= 5; i++)
        {
            await Task.Delay(50);

            // 在 yield 之前处理异常
            string? result = null;
            try
            {
                if (i == 3)
                    throw new Exception("处理失败！");

                result = $"Item-{i}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠️  跳过 Item-{i}: {ex.Message}");
                // 继续处理下一项
            }

            // 只有成功的才返回
            if (result != null)
                yield return result;
        }
    }

    // 辅助方法
    private static async IAsyncEnumerable<int> GetNumbersAsync(int count)
    {
        Console.WriteLine($"  [生成 {count} 个数字]");
        for (int i = 1; i <= count; i++)
        {
            await Task.Delay(50);
            yield return i;
        }
    }
}
