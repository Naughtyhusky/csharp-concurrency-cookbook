namespace AsyncEnumerable;

/// <summary>
/// 演示性能对比（需要单独运行）
/// 取消注释 Main 方法并注释掉 Program.cs 的 Main 方法即可运行
/// </summary>
public class RunPerformanceDemo
{
    // 如需运行此演示，请取消注释下面的代码，并注释掉 Program.cs 的 Main 方法
    /*
    public static async Task Main()
    {
        Console.WriteLine("=== IAsyncEnumerable 性能对比演示 ===\n");

        // 对比1：内存占用
        await PerformanceComparison.CompareMemoryUsage();

        // 对比2：响应时间
        await PerformanceComparison.CompareResponseTime();

        // 对比3：提前退出
        await PerformanceComparison.CompareEarlyExit();

        // 实际场景：日志分析
        await PerformanceComparison.RealWorldScenario_LogAnalysis();

        Console.WriteLine("\n=== 演示完成 ===");
    }
    */

    public static async Task RunAsync()
    {
        Console.WriteLine("=== IAsyncEnumerable 性能对比演示 ===\n");

        // 对比1：内存占用
        await PerformanceComparison.CompareMemoryUsage();

        // 对比2：响应时间
        await PerformanceComparison.CompareResponseTime();

        // 对比3：提前退出
        await PerformanceComparison.CompareEarlyExit();

        // 实际场景：日志分析
        await PerformanceComparison.RealWorldScenario_LogAnalysis();

        Console.WriteLine("\n=== 演示完成 ===");
    }
}
