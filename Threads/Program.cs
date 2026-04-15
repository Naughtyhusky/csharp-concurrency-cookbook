// ========================================
// 第二章：并发的底层：Thread、ThreadPool 与 Task
// ========================================

namespace Threads;

/// <summary>
/// 第二章示例代码入口
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("第二章：Thread、ThreadPool 与 Task 的关系");
        Console.WriteLine("===========================================\n");

        // 示例 1：震惊的实验 - Thread vs Task
        Console.WriteLine("【示例 1】Thread vs Task 对比");
        Console.WriteLine("警告：创建 10000 个线程可能导致系统卡顿或崩溃");
        Console.WriteLine("是否运行此实验？(y/n)");
        var input = Console.ReadLine();
        if (input?.ToLower() == "y")
        {
            await ThreadVsTaskDemo.RunAsync();
        }
        Console.WriteLine();

        // 示例 2：Thread 的成本
        Console.WriteLine("【示例 2】Thread 的上下文切换成本");
        ContextSwitchCostDemo.Run();
        Console.WriteLine();

        // 示例 2.5：False Sharing 演示
        Console.WriteLine("【示例 2.5】False Sharing 性能影响");
        Console.WriteLine("警告：此实验需要运行大量迭代，可能需要 1-2 分钟");
        Console.WriteLine("是否运行此实验？(y/n)");
        var input2 = Console.ReadLine();
        if (input2?.ToLower() == "y")
        {
            FalseSharingDemo.Run();
        }
        Console.WriteLine();

        // 示例 3：ThreadPool 监控
        Console.WriteLine("【示例 3】ThreadPool 动态调整");
        ThreadPoolDynamicDemo.Run();
        Console.WriteLine();

        // 示例 4：工作窃取演示
        Console.WriteLine("【示例 4】工作窃取算法（简化模拟）");
        WorkStealingDemo.Run();
        Console.WriteLine();

        // 示例 5：Task 状态机
        Console.WriteLine("【示例 5】Task 状态转换");
        TaskStateDemo.Run();
        Console.WriteLine();

        // 示例 6：I/O Task 不占用线程
        Console.WriteLine("【示例 6】I/O Task 不占用线程验证");
        await IoTaskThreadDemo.RunAsync();
        Console.WriteLine();

        // 示例 7：ThreadPool 监控
        Console.WriteLine("【示例 7】ThreadPool 实时监控");
        Console.WriteLine("监控将持续 30 秒，按 Enter 提前退出");
        ThreadPoolMonitorDemo.Run();
        Console.WriteLine();

        Console.WriteLine("===========================================");
        Console.WriteLine("所有示例运行完毕！");
        Console.WriteLine("===========================================");
    }
}
