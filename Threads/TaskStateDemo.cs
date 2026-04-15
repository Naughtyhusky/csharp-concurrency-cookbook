// ========================================
// Task 状态转换演示
// ========================================

namespace Threads;

/// <summary>
/// 演示 Task 的状态转换
/// </summary>
internal static class TaskStateDemo
{
    public static void Run()
    {
        Console.WriteLine("演示 Task 的状态转换过程\n");

        // 示例 1：Created → WaitingToRun → Running → RanToCompletion
        DemonstrateNormalCompletion();

        Console.WriteLine();

        // 示例 2：Faulted（异常）
        DemonstrateFaulted();

        Console.WriteLine();

        // 示例 3：Canceled（取消）
        DemonstrateCanceled();
    }

    /// <summary>
    /// 正常完成的 Task
    /// </summary>
    private static void DemonstrateNormalCompletion()
    {
        Console.WriteLine("=== 示例 1：正常完成 ===");

        // Created
        var task = new Task(() =>
        {
            Console.WriteLine($"  [执行中] 状态：{TaskStatus.Running}");
            Thread.Sleep(1000);
        });

        Console.WriteLine($"[创建后] 状态：{task.Status}");  // Created

        // WaitingToRun / WaitingForActivation
        task.Start();
        Console.WriteLine($"[启动后] 状态：{task.Status}");  // WaitingToRun 或 Running

        // Running
        Thread.Sleep(100);
        if (task.Status == TaskStatus.Running)
        {
            Console.WriteLine($"[运行中] 状态：{task.Status}");
        }

        // RanToCompletion
        task.Wait();
        Console.WriteLine($"[完成后] 状态：{task.Status}");  // RanToCompletion
        Console.WriteLine($"  IsCompleted：{task.IsCompleted}");
        Console.WriteLine($"  IsCompletedSuccessfully：{task.IsCompletedSuccessfully}");
    }

    /// <summary>
    /// 异常状态的 Task
    /// </summary>
    private static void DemonstrateFaulted()
    {
        Console.WriteLine("=== 示例 2：异常状态 ===");

        var task = Task.Run(() =>
        {
            Thread.Sleep(500);
            throw new InvalidOperationException("模拟异常");
        });

        try
        {
            task.Wait();
        }
        catch (AggregateException ae)
        {
            Console.WriteLine($"[异常后] 状态：{task.Status}");  // Faulted
            Console.WriteLine($"  IsFaulted：{task.IsFaulted}");
            Console.WriteLine($"  异常信息：{ae.InnerException?.Message}");
        }
    }

    /// <summary>
    /// 取消状态的 Task
    /// </summary>
    private static void DemonstrateCanceled()
    {
        Console.WriteLine("=== 示例 3：取消状态 ===");

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var task = Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(200);
            }
        }, token);

        // 等待一段时间后取消
        Thread.Sleep(500);
        cts.Cancel();

        try
        {
            task.Wait();
        }
        catch (AggregateException ae) when (ae.InnerException is OperationCanceledException)
        {
            Console.WriteLine($"[取消后] 状态：{task.Status}");  // Canceled
            Console.WriteLine($"  IsCanceled：{task.IsCanceled}");
        }

        Console.WriteLine();
        Console.WriteLine("状态转换总结：");
        Console.WriteLine("  Created → WaitingForActivation → Running → RanToCompletion（成功）");
        Console.WriteLine("                                           → Faulted（异常）");
        Console.WriteLine("                                           → Canceled（取消）");
    }
}
