using Parallel_PLinq.ParallelBasic;
using Parallel_PLinq.Performance;
using Parallel_PLinq.Pitfalls;
using Parallel_PLinq.PLinqBasic;
using Parallel_PLinq.WhenToUse;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔═══════════════════════════════════════════════════╗");
Console.WriteLine("║      第10章：Parallel 与 PLINQ —— 榨干多核 CPU      ║");
Console.WriteLine("╚═══════════════════════════════════════════════════╝");
Console.WriteLine();

// ── 第一部分：Parallel.For / ForEach 基本用法 ──
ParallelForDemo.Demo();
Console.WriteLine();

// ── 第二部分：Parallel 循环中断与退出 ──
ParallelBreakDemo.Demo();
Console.WriteLine();

// ── 第三部分：PLINQ 基本用法 ──
PLinqDemo.Demo();
Console.WriteLine();

// ── 第四部分：何时选择并行 & 实战案例 ──
WhenToUseParallelDemo.Demo();
Console.WriteLine();

// ── 第五部分：性能对比 ──
PerformanceComparisonDemo.Demo();
Console.WriteLine();

// ── 第六部分：常见陷阱 ──
CommonPitfallsDemo.Demo();
Console.WriteLine();

Console.WriteLine("╔═══════════════════════════════════════════════════╗");
Console.WriteLine("║  所有演示完成！记住：并行不是银弹，数据量 + 计算量决定一切  ║");
Console.WriteLine("╚═══════════════════════════════════════════════════╝");

