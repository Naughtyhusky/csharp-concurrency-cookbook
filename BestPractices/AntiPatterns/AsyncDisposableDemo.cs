namespace BestPractices.AntiPatterns;

/// <summary>
/// 反模式演示：async using 资源泄漏
/// 在异步代码中错误使用 using 导致资源未被释放
/// </summary>
public class AsyncDisposableDemo
{
    // 模拟一个异步可释放的资源（比如数据库连接、网络流）
    private class AsyncResource : IAsyncDisposable
    {
        private readonly string _name;
        private bool _disposed;

        public AsyncResource(string name)
        {
            _name = name;
            Console.WriteLine($"  [资源] {_name} 已创建");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await Task.Delay(10); // 模拟异步清理
            Console.WriteLine($"  [资源] {_name} 已正确释放（异步）");
        }

        public async Task<string> ReadAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(_name);
            await Task.Delay(20);
            return $"来自 {_name} 的数据";
        }
    }

    // ❌ 反模式：同步 using 用在 IAsyncDisposable 上
    // 编译器不会报错，但实际上调用的是同步 Dispose()，
    // 如果对象只实现了 IAsyncDisposable，会出现问题
    public static async Task BadSyncUsing()
    {
        Console.WriteLine("\n❌ 错误：在异步资源上使用同步 using（资源可能未正确释放）");

        // 注意：如果 AsyncResource 只实现 IAsyncDisposable 而没有 IDisposable
        // 这里会编译报错或 Dispose 逻辑被跳过
        // 为了演示，我们展示概念性的问题

        var resource = new AsyncResource("同步using资源");
        // 使用同步 using 无法调用 DisposeAsync
        // using (resource) // ← 如果只有 IAsyncDisposable，这里无法编译
        // 你会被迫手动调用，或者用错方式
        try
        {
            var data = await resource.ReadAsync();
            Console.WriteLine($"  读取到：{data}");
        }
        finally
        {
            // 开发者可能忘记写这里，或者误写成同步的 Dispose
            await resource.DisposeAsync(); // 必须手动写 DisposeAsync
        }
    }

    // ✅ 正确做法：await using
    public static async Task GoodAwaitUsing()
    {
        Console.WriteLine("\n✅ 正确：使用 await using 自动调用 DisposeAsync");

        await using var resource = new AsyncResource("await using 资源");
        var data = await resource.ReadAsync();
        Console.WriteLine($"  读取到：{data}");
        // 离开作用域时自动调用 DisposeAsync，丝滑优雅
    }

    // ❌ 更隐蔽的反模式：在 using 块中返回 Task 而不是 await
    public static Task<string> BadReturnTaskInUsing()
    {
        // ⚠️ 这是经典的"资源已释放就访问"问题！
        // using 块在方法返回时立即释放资源，但 Task 还没执行完
        var resource = new AsyncResource("提前释放的资源");
        // 不能这样写！resource 在 using 块结束时就被释放了
        // 但返回的 Task 还没有执行到 ReadAsync()
        return resource.ReadAsync(); // ← 资源已经被释放！
        // 正确写法见下面的 GoodAwaitInUsing
    }

    // ✅ 正确做法：在 using 块中 await
    public static async Task<string> GoodAwaitInUsing()
    {
        await using var resource = new AsyncResource("正确使用的资源");
        return await resource.ReadAsync(); // ✅ await 确保任务完成前不离开 using 块
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== async using 资源泄漏演示 ===");
        await BadSyncUsing();
        await GoodAwaitUsing();

        Console.WriteLine("\n✅ 在 using 中正确 await：");
        var result = await GoodAwaitInUsing();
        Console.WriteLine($"  获取到：{result}");
    }
}
