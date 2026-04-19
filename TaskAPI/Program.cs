namespace TaskAPI;

/// <summary>
/// Task API 完全指南 - 配套代码示例
/// 对应博客：《03. Task API 完全指南：方法与属性的实战应用》
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Task API 完全指南 - 代码示例 ===\n");

        // 运行所有示例
        RunSection("1. 创建任务", Demo01_TaskCreation.Run);
        RunSection("2. 等待任务", Demo02_TaskWaiting.Run);
        await RunSectionAsync("3. 组合任务", Demo03_TaskComposition.RunAsync);
        await RunSectionAsync("4. 任务延续", Demo04_TaskContinuation.RunAsync);
        RunSection("5. 任务状态", Demo05_TaskStatus.Run);
        await RunSectionAsync("6. 常见陷阱", Demo06_CommonPitfalls.RunAsync);
        await RunSectionAsync("7. 实战演练", Demo07_PracticalExamples.RunAsync);

        Console.WriteLine("\n=== 所有示例执行完毕 ===");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }

    static void RunSection(string title, Action action)
    {
        Console.WriteLine($"\n{'=',-50}");
        Console.WriteLine($"{title}");
        Console.WriteLine($"{'=',-50}");
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 异常: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static async Task RunSectionAsync(string title, Func<Task> action)
    {
        Console.WriteLine($"\n{'=',-50}");
        Console.WriteLine($"{title}");
        Console.WriteLine($"{'=',-50}");
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 异常: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
