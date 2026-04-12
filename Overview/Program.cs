// ========================================
// 第01章：并发编程全景图 - 代码示例
// ========================================

namespace Overview;

/// <summary>
/// 程序入口
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== C# 并发编程全景图 - 实战示例 ===\n");

        // 第一部分：并发、并行、异步概念演示
        await DemonstrateConceptsAsync();

        // 第二部分：任务类型识别演示
        await TaskTypeDemo.RunAsync();

        // 第三部分：常见误区演示
        await CommonMistakesDemo.RunAsync();

        Console.WriteLine("=== 所有演示完成 ===");
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 并发、并行、异步概念演示
    /// </summary>
    private static async Task DemonstrateConceptsAsync()
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("  第一部分：并发、并行、异步概念");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

        // 1. 并发示例
        await ConcurrencyDemo.RunAsync();

        // 2. 并行示例
        ParallelDemo.Run();

        // 3. 异步示例
        await AsyncDemo.RunAsync();

        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
    }
}
