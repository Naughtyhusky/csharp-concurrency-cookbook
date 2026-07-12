namespace PerformanceOptimization;

/// <summary>
/// 性能优化实战：零拷贝与内存优化
/// 第 19 章示例程序
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  第 19 章：性能优化实战 - 零拷贝与内存优化                  ║");
        Console.WriteLine("║  ValueTask / Span / Memory / ReadOnlySequence / ArrayPool  ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        // Part 1: ValueTask 示例
        await ValueTaskDemo.RunAsync();
        ValueTaskDemo.PrintUsageGuidelines();

        // Part 2: Span<T> 示例
        SpanDemo.Run();
        SpanDemo.LogParsingScenario();

        // Part 3: Memory<T> 示例
        await MemoryDemo.RunAsync();
        MemoryDemo.PrintUsageGuidelines();
        await MemoryDemo.BufferManager.DemoAsync();

        // Part 4: ReadOnlySequence<T> 示例
        await ReadOnlySequenceDemo.RunAsync();
        ReadOnlySequenceDemo.PerformanceComparison();

        // Part 5: ArrayPool<T> 示例
        await ArrayPoolDemo.RunAsync();
        ArrayPoolDemo.PerformanceComparison();

        // Part 6: 综合实战示例
        await ComprehensiveDemo.RunAsync();
        ComprehensiveDemo.PrintOptimizationSummary();

        // Part 7: BenchmarkDotNet 测试
        Benchmarks.RunBenchmarks();
        BenchmarkGuide.PrintGuide();

        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  🎉 所有示例运行完成！                                      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        Console.WriteLine("💡 关键要点总结：");
        Console.WriteLine("  1. ValueTask：高频+同步完成占比高 → 减少 Task 分配");
        Console.WriteLine("  2. Span<T>：栈分配+零拷贝切片 → 字符串/数组处理利器");
        Console.WriteLine("  3. Memory<T>：可跨 await → Span 的异步版本");
        Console.WriteLine("  4. ReadOnlySequence<T>：处理分段数据 → 网络编程必备");
        Console.WriteLine("  5. ArrayPool<T>：租借-归还 → 减少 GC 压力");
        Console.WriteLine("  6. 综合使用：性能提升 5-10 倍，GC 压力归零");
        Console.WriteLine();

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
