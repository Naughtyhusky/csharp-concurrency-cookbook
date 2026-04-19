namespace TaskAPI;

/// <summary>
/// 示例 4: 任务延续 - ContinueWith vs await
/// </summary>
public static class Demo04_TaskContinuation
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n--- 4.1 ContinueWith - 传统方式 ---");
        ContinueWithExample();

        Console.WriteLine("\n--- 4.2 await - 现代推荐方式 ---");
        await AwaitExample();

        Console.WriteLine("\n--- 4.3 异常处理对比 ---");
        await ExceptionHandlingComparison();
    }

    static void ContinueWithExample()
    {
        Console.WriteLine("⏳ 使用 ContinueWith:");

        Task.Run(() =>
        {
            Thread.Sleep(1000);
            return 42;
        })
        .ContinueWith(antecedent =>
        {
            int result = antecedent.Result;
            Console.WriteLine($"✓ ContinueWith 结果: {result}");
        })
        .Wait();

        // 复杂的异常处理
        Task.Run<int>(() =>
        {
            throw new InvalidOperationException("模拟错误");
#pragma warning disable CS0162 // 检测到无法访问的代码
            return 0;
#pragma warning restore CS0162
        })
        .ContinueWith(antecedent =>
        {
            if (antecedent.IsFaulted)
            {
                Console.WriteLine($"⚠️ ContinueWith 捕获异常: {antecedent.Exception?.InnerException?.Message}");
            }
            else if (antecedent.IsCanceled)
            {
                Console.WriteLine("⚠️ 任务被取消");
            }
            else
            {
                Console.WriteLine($"✓ 结果: {antecedent.Result}");
            }
        })
        .Wait();
    }

    static async Task AwaitExample()
    {
        Console.WriteLine("⏳ 使用 await:");

        try
        {
            int result = await Task.Run(() =>
            {
                Thread.Sleep(1000);
                return 42;
            });

            Console.WriteLine($"✓ await 结果: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ await 捕获异常: {ex.Message}");
        }
    }

    static async Task ExceptionHandlingComparison()
    {
        // ContinueWith 方式（嵌套复杂）
        Console.WriteLine("📝 ContinueWith 异常处理:");
        Task.Run(() =>
        {
            throw new InvalidOperationException("ContinueWith 错误");
        })
        .ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                // 需要访问 Exception.InnerException
                Console.WriteLine($"  ⚠️ 异常: {t.Exception?.InnerException?.Message}");
            }
        })
        .Wait();

        // await 方式（简洁清晰）
        Console.WriteLine("\n📝 await 异常处理:");
        try
        {
            await Task.Run(() =>
            {
                throw new InvalidOperationException("await 错误");
            });
        }
        catch (InvalidOperationException ex)
        {
            // 直接捕获原始异常
            Console.WriteLine($"  ⚠️ 异常: {ex.Message}");
        }

        // 对比总结
        Console.WriteLine("\n📊 对比总结:");
        Console.WriteLine("  ContinueWith: ❌ 嵌套回调，异常处理复杂");
        Console.WriteLine("  await:        ✅ 线性代码，try-catch 自然");
    }
}
