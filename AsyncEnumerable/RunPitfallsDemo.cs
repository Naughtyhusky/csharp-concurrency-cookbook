namespace AsyncEnumerable;

/// <summary>
/// 演示运行陷阱示例（需要单独运行，避免与主程序混淆）
/// 取消注释 Main 方法并注释掉 Program.cs 的 Main 方法即可运行
/// </summary>
public class RunPitfallsDemo
{
    // 如需运行此演示，请取消注释下面的代码，并注释掉 Program.cs 的 Main 方法
    /*
    public static async Task Main()
    {
        Console.WriteLine("=== IAsyncEnumerable 常见陷阱演示 ===\n");

        // 陷阱1：多次遍历
        await CommonPitfalls.Pitfall1_MultipleEnumeration();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱2：在 yield 之前加载全部数据
        await CommonPitfalls.Pitfall2_LoadAllBeforeYield();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱3：忘记 await foreach
        CommonPitfalls.Pitfall3_ForgotAwaitForEach();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱4：没有正确传递 CancellationToken
        await CommonPitfalls.Pitfall4_MissingCancellationToken();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱5：捕获大对象
        await CommonPitfalls.Pitfall5_CapturingLargeObjects();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱6：异常处理
        await CommonPitfalls.Pitfall6_ImproperExceptionHandling();

        Console.WriteLine("\n=== 演示完成 ===");
    }
    */

    public static async Task RunAsync()
    {
        Console.WriteLine("=== IAsyncEnumerable 常见陷阱演示 ===\n");

        // 陷阱1：多次遍历
        await CommonPitfalls.Pitfall1_MultipleEnumeration();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱2：在 yield 之前加载全部数据
        await CommonPitfalls.Pitfall2_LoadAllBeforeYield();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱3：忘记 await foreach
        CommonPitfalls.Pitfall3_ForgotAwaitForEach();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱4：没有正确传递 CancellationToken
        await CommonPitfalls.Pitfall4_MissingCancellationToken();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱5：捕获大对象
        await CommonPitfalls.Pitfall5_CapturingLargeObjects();
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        // 陷阱6：异常处理
        await CommonPitfalls.Pitfall6_ImproperExceptionHandling();

        Console.WriteLine("\n=== 演示完成 ===");
    }
}
