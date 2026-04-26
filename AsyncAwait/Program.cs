namespace AsyncAwait;

/// <summary>
/// 第 04 章：async/await 原理与性能优化
/// 
/// 演示内容：
/// - Demo01: async/await 基础
/// - Demo02: 状态机原理演示
/// - Demo03: 线程使用对比（async vs 同步）
/// - Demo04: SynchronizationContext 与 ConfigureAwait
/// - Demo05: ValueTask 性能优化
/// - Demo06: 常见陷阱演示
/// - Demo07: 实战示例
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 第 04 章：async/await 原理与性能优化 ===\n");

        // 1. 基础演示
        Console.WriteLine("按任意键开始 Demo01: async/await 基础...");
        Console.ReadKey();
        await Demo01_AsyncBasics.Run();

        // 2. 状态机原理
        Console.WriteLine("\n按任意键开始 Demo02: 状态机原理...");
        Console.ReadKey();
        await Demo02_StateMachine.Run();

        // 3. 线程使用对比
        Console.WriteLine("\n按任意键开始 Demo03: 线程使用对比...");
        Console.ReadKey();
        await Demo03_ThreadComparison.Run();

        // 4. SynchronizationContext
        Console.WriteLine("\n按任意键开始 Demo04: SynchronizationContext...");
        Console.ReadKey();
        await Demo04_SynchronizationContext.Run();

        // 5. ValueTask 性能
        Console.WriteLine("\n按任意键开始 Demo05: ValueTask 性能...");
        Console.ReadKey();
        await Demo05_ValueTask.Run();

        // 6. 常见陷阱
        Console.WriteLine("\n按任意键开始 Demo06: 常见陷阱...");
        Console.ReadKey();
        await Demo06_CommonPitfalls.Run();

        // 7. 实战示例
        Console.WriteLine("\n按任意键开始 Demo07: 实战示例...");
        Console.ReadKey();
        await Demo07_PracticalExamples.Run();

        Console.WriteLine("\n\n=== 所有演示完成！===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}

