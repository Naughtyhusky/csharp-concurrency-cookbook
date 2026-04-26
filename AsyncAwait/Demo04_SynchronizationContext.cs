namespace AsyncAwait;

/// <summary>
/// Demo04: SynchronizationContext 与 ConfigureAwait
/// </summary>
public static class Demo04_SynchronizationContext
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo04: SynchronizationContext 与 ConfigureAwait ===\n");

        // 4.1 检查当前的 SynchronizationContext
        Console.WriteLine("--- 4.1 当前的 SynchronizationContext ---");
        CheckCurrentContext();

        // 4.2 默认行为（捕获上下文）
        Console.WriteLine("\n--- 4.2 默认行为（捕获上下文）---");
        await DefaultBehavior();

        // 4.3 ConfigureAwait(false)
        Console.WriteLine("\n--- 4.3 ConfigureAwait(false) ---");
        await ConfigureAwaitFalseExample();

        // 4.4 性能对比
        Console.WriteLine("\n--- 4.4 性能对比 ---");
        await PerformanceComparison();
    }

    static void CheckCurrentContext()
    {
        var context = SynchronizationContext.Current;
        if (context == null)
        {
            Console.WriteLine("✓ SynchronizationContext: null（控制台应用默认）");
            Console.WriteLine("  这意味着 await 后可能在任意 ThreadPool 线程上恢复");
        }
        else
        {
            Console.WriteLine($"✓ SynchronizationContext: {context.GetType().Name}");
        }
    }

    static async Task DefaultBehavior()
    {
        Console.WriteLine($"✓ [Before await] 线程: {Environment.CurrentManagedThreadId}, " +
                          $"Context: {SynchronizationContext.Current?.GetType().Name ?? "null"}");

        await Task.Delay(500); // 默认捕获上下文

        Console.WriteLine($"✓ [After await] 线程: {Environment.CurrentManagedThreadId}, " +
                          $"Context: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
        Console.WriteLine("  💡 在控制台应用中，上下文为 null，所以可能在不同线程上恢复");
    }

    static async Task ConfigureAwaitFalseExample()
    {
        Console.WriteLine($"✓ [Before await] 线程: {Environment.CurrentManagedThreadId}");

        await Task.Delay(500).ConfigureAwait(false); // ⭐ 不捕获上下文

        Console.WriteLine($"✓ [After await] 线程: {Environment.CurrentManagedThreadId}");
        Console.WriteLine("  💡 ConfigureAwait(false) 明确告诉运行时：不需要恢复到原始上下文");
    }

    static async Task PerformanceComparison()
    {
        const int iterations = 1000;

        // 测试 1：默认行为
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await Task.Delay(1); // 默认捕获上下文
        }
        sw1.Stop();
        Console.WriteLine($"✓ 默认行为（捕获上下文）: {sw1.ElapsedMilliseconds} ms");

        // 测试 2：ConfigureAwait(false)
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await Task.Delay(1).ConfigureAwait(false); // 不捕获上下文
        }
        sw2.Stop();
        Console.WriteLine($"✓ ConfigureAwait(false): {sw2.ElapsedMilliseconds} ms");

        Console.WriteLine($"\n  💡 性能提升: {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F2}x");
        Console.WriteLine("  💡 在控制台应用中差异不大，但在 UI 应用中会更明显");
    }
}

/// <summary>
/// 演示库代码中如何使用 ConfigureAwait(false)
/// </summary>
public class LibraryCodeExample
{
    private static readonly HttpClient _httpClient = new HttpClient();

    // ✅ 库代码：使用 ConfigureAwait(false)
    public async Task<string> GetDataAsync(string url)
    {
        // 所有 await 都使用 ConfigureAwait(false)
        var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return content;
    }

    // ⚠️ 如果这是 UI 代码，需要更新 UI，则不使用 ConfigureAwait(false)
    public async Task LoadDataToUIAsync(string url)
    {
        var data = await GetDataAsync(url); // 库方法使用了 ConfigureAwait(false)

        // ⚠️ 这里需要在 UI 线程上执行，所以不使用 ConfigureAwait(false)
        // 在实际 UI 应用中，这里会更新 UI 控件
        Console.WriteLine($"✓ 数据加载完成: {data.Length} 字节");
    }
}
