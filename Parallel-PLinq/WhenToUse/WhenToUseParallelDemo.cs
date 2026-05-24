namespace Parallel_PLinq.WhenToUse;

/// <summary>
/// 何时选择并行？决策指南与实战案例
/// </summary>
public static class WhenToUseParallelDemo
{
    // ─── 决策树展示 ──────────────────────────────────────────────────────────

    public static void DecisionTreeDemo()
    {
        Console.WriteLine("\n── 并行化决策树 ──");
        Console.WriteLine("""
          你的任务是否适合并行化？

          ┌─ 是 CPU 密集型吗？
          │   ├── 否 → 用 async/await + Task.WhenAll（第02-04章）
          │   └── 是 ↓
          ├─ 每个元素的计算相互独立吗？
          │   ├── 否（需要共享状态）→ 考虑重构，或用线程安全结构
          │   └── 是 ↓
          ├─ 数据量足够大（>10万）或单元素计算足够重？
          │   ├── 否 → 顺序执行更快（并行开销大于收益）
          │   └── 是 ↓
          └─ ✅ 适合并行！选 Parallel.For/ForEach 或 PLINQ
        """);
    }

    // ─── 实战案例：图像像素处理 ──────────────────────────────────────────────

    /// <summary>
    /// 典型的 CPU 密集型任务：对图像每个像素做变换
    /// 每个像素计算独立，数据量大 → 完美适合并行
    /// </summary>
    public static void ImageProcessingDemo()
    {
        Console.WriteLine("\n── 实战案例：模拟图像像素处理 ──");

        const int width = 1000;
        const int height = 1000;
        int totalPixels = width * height;

        // 模拟 100万像素的灰度图像
        byte[] pixels = new byte[totalPixels];
        Random.Shared.NextBytes(pixels);

        // 顺序处理：对每个像素做 gamma 校正（CPU 密集型）
        byte[] seqOutput = new byte[totalPixels];
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < totalPixels; i++)
        {
            seqOutput[i] = GammaCorrect(pixels[i]);
        }
        sw.Stop();
        Console.WriteLine($"  顺序处理 {totalPixels:N0} 像素: {sw.ElapsedMilliseconds} ms");

        // 并行处理：完美适合！每个像素独立，计算量大
        byte[] parOutput = new byte[totalPixels];
        sw.Restart();
        Parallel.For(0, totalPixels, i =>
        {
            parOutput[i] = GammaCorrect(pixels[i]);
        });
        sw.Stop();
        Console.WriteLine($"  并行处理 {totalPixels:N0} 像素: {sw.ElapsedMilliseconds} ms");

        // 验证结果一致
        bool isEqual = seqOutput.SequenceEqual(parOutput);
        Console.WriteLine($"  结果验证: {(isEqual ? "✅ 完全一致" : "❌ 不一致")}");
        Console.WriteLine("  像素处理是并行的经典场景：独立、大量、无共享状态");
    }

    // ─── 实战案例：并行数据聚合 ──────────────────────────────────────────────

    /// <summary>
    /// 对海量订单数据做统计分析：PLINQ 让代码保持 LINQ 风格同时获得并行加速
    /// </summary>
    public static void DataAggregationDemo()
    {
        Console.WriteLine("\n── 实战案例：海量订单数据统计（PLINQ）──");

        // 模拟 100 万条订单
        var orders = Enumerable.Range(1, 1_000_000).Select(i => new
        {
            Id = i,
            Amount = (decimal)(i % 10000) / 100m,
            Category = (i % 5) switch { 0 => "A", 1 => "B", 2 => "C", 3 => "D", _ => "E" },
            IsValid = i % 7 != 0
        }).ToList();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // PLINQ：代码几乎和普通 LINQ 一样，只加了 AsParallel()
        var summary = orders
            .AsParallel()
            .Where(o => o.IsValid && o.Amount > 50m)
            .GroupBy(o => o.Category)
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count(),
                Total = g.Sum(o => o.Amount)
            })
            .OrderBy(x => x.Category)
            .ToList();

        sw.Stop();

        Console.WriteLine($"  处理 100万条订单耗时: {sw.ElapsedMilliseconds} ms");
        foreach (var item in summary)
            Console.WriteLine($"  分类 {item.Category}: {item.Count:N0} 笔，总金额 {item.Total:N2}");
    }

    private static byte GammaCorrect(byte input)
    {
        // 模拟 gamma 2.2 校正（有一定计算量）
        double normalized = input / 255.0;
        double corrected = Math.Pow(normalized, 1.0 / 2.2);
        return (byte)(corrected * 255.0);
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  何时选择并行 & 实战案例");
        Console.WriteLine("═══════════════════════════════════════");

        DecisionTreeDemo();
        ImageProcessingDemo();
        DataAggregationDemo();
    }
}
