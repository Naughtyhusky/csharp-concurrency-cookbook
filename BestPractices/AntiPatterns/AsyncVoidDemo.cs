namespace BestPractices.AntiPatterns;

/// <summary>
/// 反模式演示：async void 的各种坑
/// </summary>
public class AsyncVoidDemo
{
    // ❌ 反模式 1：普通方法使用 async void
    // 异常会直接崩溃进程，调用方无法捕获，也无法 await
    public static async void BadAsyncVoid_FireAndForget()
    {
        await Task.Delay(100);
        throw new InvalidOperationException("这个异常会让进程崩溃，谁都救不了你！");
    }

    // ❌ 调用方根本无法捕获这个异常
    public static void CallerCannotCatch()
    {
        try
        {
            BadAsyncVoid_FireAndForget(); // 这里不会抛出异常
            Console.WriteLine("调用返回了，但异常还在路上...");
        }
        catch (Exception ex)
        {
            // 永远不会执行到这里
            Console.WriteLine($"能捕获到吗？不能！{ex.Message}");
        }
    }

    // ❌ 反模式 2：用 async void 做测试方法（单元测试框架不会等它）
    // [Test] public async void TestSomething() { ... }  // 测试会立即通过/失败，不等待异步完成

    // ✅ 唯一合法的 async void 使用场景：UI 事件处理器
    // 如果你在 WinForms/WPF 里写事件处理，这是被允许的
    // private async void Button_Click(object sender, EventArgs e)
    // {
    //     await DoSomeWorkAsync();
    // }

    /// <summary>
    /// 演示 async void 的问题：调用方无法知道任务是否完成
    /// </summary>
    public static async Task DemoAsyncVoidProblem()
    {
        Console.WriteLine("\n=== async void 反模式演示 ===");
        Console.WriteLine("❌ async void 方法被调用，但调用方无法等待它完成...");

        // async void 方法调用后立即返回，调用方无法 await
        // 下面这行如果取消注释会导致未处理异常崩溃
        // BadAsyncVoid_FireAndForget();

        Console.WriteLine("✅ 正确做法：始终返回 Task，这样调用方可以 await 或处理异常");
        await Task.CompletedTask;
    }
}
