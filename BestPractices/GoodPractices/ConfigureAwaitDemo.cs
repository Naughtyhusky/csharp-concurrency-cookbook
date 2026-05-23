namespace BestPractices.GoodPractices;

/// <summary>
/// 最佳实践：ConfigureAwait 的正确使用
/// 
/// 核心规则：
/// - 库代码（类库/NuGet 包）：始终用 ConfigureAwait(false)
/// - 应用程序代码（WinForms/WPF/ASP.NET）：按需使用，不强制
/// </summary>
public class ConfigureAwaitDemo
{
    // ❌ 反模式：库代码没有 ConfigureAwait(false)
    // 如果库的调用方在 UI 线程，await 结束后会试图回到 UI 线程
    // 而如果调用方用了 .Result 阻塞了 UI 线程，就死锁了
    public static async Task<string> LibraryMethod_Bad(string url)
    {
        // 假设这是你发布的 NuGet 包里的代码
        await Task.Delay(100); // ← 没有 ConfigureAwait(false)，潜在死锁风险
        return "数据";
    }

    // ✅ 最佳实践：库代码统一加 ConfigureAwait(false)
    public static async Task<string> LibraryMethod_Good(string url)
    {
        await Task.Delay(100).ConfigureAwait(false); // ✅
        // 继续执行不需要特定的同步上下文
        return "数据";
    }

    // ✅ 应用程序代码中需要更新 UI 时，不用 ConfigureAwait(false)
    // （WinForms/WPF 示例，这里用注释说明）
    // private async void LoadData_Click(object sender, EventArgs e)
    // {
    //     var data = await FetchDataAsync(); // ← 不加 ConfigureAwait(false)
    //     label.Text = data; // ← 需要在 UI 线程执行
    // }

    // ✅ 应用程序代码中不需要 UI 上下文时，加上更好
    public static async Task ProcessInBackground()
    {
        var data = await FetchDataAsync().ConfigureAwait(false); // 不需要回 UI 线程
        // 纯数据处理，跑在任意线程都行
        var processed = data.ToUpper();
        Console.WriteLine($"  处理结果: {processed}");
    }

    // 模拟多层嵌套的异步调用 - 体现 ConfigureAwait(false) 的传播
    public static async Task<int> MultiLayerAsync()
    {
        // 每一层都加 ConfigureAwait(false)，彻底脱离同步上下文
        var step1 = await Step1Async().ConfigureAwait(false);
        var step2 = await Step2Async(step1).ConfigureAwait(false);
        return await Step3Async(step2).ConfigureAwait(false);
    }

    private static async Task<int> Step1Async()
    {
        await Task.Delay(10).ConfigureAwait(false);
        return 1;
    }

    private static async Task<int> Step2Async(int input)
    {
        await Task.Delay(10).ConfigureAwait(false);
        return input + 1;
    }

    private static async Task<int> Step3Async(int input)
    {
        await Task.Delay(10).ConfigureAwait(false);
        return input * 10;
    }

    private static async Task<string> FetchDataAsync()
    {
        await Task.Delay(30).ConfigureAwait(false);
        return "fetched data";
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== ConfigureAwait 最佳实践演示 ===");

        Console.WriteLine("✅ 库代码使用 ConfigureAwait(false)：");
        var result = await LibraryMethod_Good("https://example.com");
        Console.WriteLine($"  结果: {result}");

        Console.WriteLine("\n✅ 多层异步调用，每层都 ConfigureAwait(false)：");
        var value = await MultiLayerAsync();
        Console.WriteLine($"  计算结果: {value}");

        Console.WriteLine("\n📌 ConfigureAwait 规则总结：");
        Console.WriteLine("  类库代码：始终写 ConfigureAwait(false)");
        Console.WriteLine("  应用代码：");
        Console.WriteLine("    - 需要访问 UI 控件 → 不加（保留上下文）");
        Console.WriteLine("    - 纯数据处理 → 加上更好（避免不必要的上下文切换）");
        Console.WriteLine("  .NET 5+ 的 ASP.NET Core：没有 SynchronizationContext，加不加都行");
        Console.WriteLine("  但养成习惯写 ConfigureAwait(false) 总是没错的！");
    }
}
