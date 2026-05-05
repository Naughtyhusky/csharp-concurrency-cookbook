namespace CancellationToken;

/// <summary>
/// 示例5：CPU 密集型任务取消
/// </summary>
public static class Example05_CpuIntensiveTask
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 示例5：CPU 密集型任务取消 ===\n");

        using var cts = new CancellationTokenSource();

        // 启动计算任务
        var task = Task.Run(() => CalculatePrimesAsync(10_000_000, cts.Token));

        // 等待 2 秒后取消
        Console.WriteLine("计算质数中（2秒后取消）...\n");
        await Task.Delay(2000);

        Console.WriteLine("\n发送取消信号...");
        cts.Cancel();

        try
        {
            var count = await task;
            Console.WriteLine($"✅ 计算完成，找到 {count} 个质数");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("❌ 计算已取消");
        }
    }

    private static int CalculatePrimesAsync(int max, System.Threading.CancellationToken cancellationToken)
    {
        int count = 0;
        int lastReported = 0;

        for (int i = 2; i < max; i++)
        {
            // 每 10000 次检查一次取消（性能权衡）
            if (i % 10000 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 显示进度
                if (i - lastReported >= 100000)
                {
                    Console.WriteLine($"  已检查: {i:N0} / {max:N0} ({(double)i / max * 100:F1}%)，找到 {count} 个质数");
                    lastReported = i;
                }
            }

            if (IsPrime(i))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsPrime(int number)
    {
        if (number < 2) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;

        var sqrt = (int)Math.Sqrt(number);
        for (int i = 3; i <= sqrt; i += 2)
        {
            if (number % i == 0) return false;
        }

        return true;
    }
}
