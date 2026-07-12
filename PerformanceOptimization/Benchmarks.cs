using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers;
using System.Text;

namespace PerformanceOptimization;

/// <summary>
/// BenchmarkDotNet 性能测试
/// 用于量化不同实现方式的性能差异
/// </summary>
public class Benchmarks
{
    /// <summary>
    /// 运行所有 Benchmark 测试
    /// </summary>
    public static void RunBenchmarks()
    {
        Console.WriteLine("\n====================================");
        Console.WriteLine("Part 7: BenchmarkDotNet 性能测试");
        Console.WriteLine("====================================\n");

        Console.WriteLine("💡 BenchmarkDotNet 是 .NET 官方推荐的性能测试工具\n");
        Console.WriteLine("📌 使用说明：");
        Console.WriteLine("   1. 在 Release 模式下运行");
        Console.WriteLine("   2. 关闭所有其他应用程序");
        Console.WriteLine("   3. 等待测试完成（可能需要几分钟）\n");

        Console.WriteLine("💡 实际运行 Benchmark：");
        Console.WriteLine("   以下测试已经设计完成，可以在 Release 模式下运行：\n");

        Console.WriteLine("   基础对比测试：");
        Console.WriteLine("   - StringParsingBenchmark：字符串解析（Substring vs Span）");
        Console.WriteLine("   - ArraySlicingBenchmark：数组切片（Copy vs Span）");
        Console.WriteLine("   - ArrayPoolBenchmark：缓冲区分配（New vs Pool）");
        Console.WriteLine("   - ValueTaskBenchmark：异步返回（Task vs ValueTask）\n");

        Console.WriteLine("   真实场景测试：");
        Console.WriteLine("   - LogParsingBenchmark：日志解析（Split vs Span，零分配）");
        Console.WriteLine("   - LargeArrayBenchmark：数组切片（频繁分配 vs 零拷贝）");
        Console.WriteLine("   - ArraySlicingOnlyBenchmark：纯切片性能（无计算干扰）");
        Console.WriteLine("   - StringBuildingBenchmark：字符串构建（StringBuilder vs Span）");
        Console.WriteLine("   - CsvParsingBenchmark：CSV 解析（批量字符串处理）\n");

        // 实际运行时，取消注释下面的代码：
        BenchmarkRunner.Run<StringParsingBenchmark>();
        BenchmarkRunner.Run<ArraySlicingBenchmark>();
        BenchmarkRunner.Run<ArrayPoolBenchmark>();
        BenchmarkRunner.Run<ValueTaskBenchmark>();
        BenchmarkRunner.Run<LogParsingBenchmark>();
        BenchmarkRunner.Run<LargeArrayBenchmark>();
        BenchmarkRunner.Run<ArraySlicingOnlyBenchmark>();
        BenchmarkRunner.Run<StringBuildingBenchmark>();
        BenchmarkRunner.Run<CsvParsingBenchmark>();
    }
}

/// <summary>
/// Benchmark 1：字符串解析性能测试
/// 实际使用请取消注释并安装 BenchmarkDotNet
/// </summary>
 [MemoryDiagnoser]
public class StringParsingBenchmark
{
    private const string Input = "2026-07-11";

    /// <summary>
    /// 基准方法：使用 Substring（多次分配）
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ParseWithSubstring()
    {
        string year = Input.Substring(0, 4);   // 分配
        string month = Input.Substring(5, 2);  // 分配
        string day = Input.Substring(8, 2);    // 分配
        return int.Parse(year) + int.Parse(month) + int.Parse(day);
    }

    /// <summary>
    /// 优化方法：使用 Span（零分配）
    /// </summary>
    [Benchmark]
    public int ParseWithSpan()
    {
        ReadOnlySpan<char> input = Input;
        ReadOnlySpan<char> year = input.Slice(0, 4);   // 零分配
        ReadOnlySpan<char> month = input.Slice(5, 2);  // 零分配
        ReadOnlySpan<char> day = input.Slice(8, 2);    // 零分配
        return int.Parse(year) + int.Parse(month) + int.Parse(day);
    }
}

/// <summary>
/// Benchmark 2：数组切片性能测试
/// </summary>
 [MemoryDiagnoser]
public class ArraySlicingBenchmark
{
    private readonly int[] _numbers = Enumerable.Range(1, 100).ToArray();

    /// <summary>
    /// 基准方法：使用 Array.Copy（复制数据）
    /// </summary>
    [Benchmark(Baseline = true)]
    public int SliceWithArrayCopy()
    {
        int[] segment = new int[10];
        Array.Copy(_numbers, 10, segment, 0, 10);
        int sum = 0;
        foreach (var n in segment)
            sum += n;
        return sum;
    }

    /// <summary>
    /// 优化方法：使用 Span（零拷贝）
    /// </summary>
    [Benchmark]
    public int SliceWithSpan()
    {
        Span<int> segment = _numbers.AsSpan(10, 10);
        int sum = 0;
        foreach (var n in segment)
            sum += n;
        return sum;
    }
}

/// <summary>
/// Benchmark 3：缓冲区分配性能测试
/// </summary>
[MemoryDiagnoser]
public class ArrayPoolBenchmark
{
    /// <summary>
    /// 基准方法：每次分配新数组
    /// </summary>
    [Benchmark(Baseline = true)]
    public byte AllocateNewArray()
    {
        byte[] buffer = new byte[256];
        buffer[0] = 42;
        return buffer[0];
    }

    /// <summary>
    /// 优化方法：使用 ArrayPool
    /// </summary>
    [Benchmark]
    public byte UseArrayPool()
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            buffer[0] = 42;
            return buffer[0];
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

/// <summary>
/// Benchmark 4：ValueTask vs Task 性能测试
/// 测试纯同步完成路径（缓存100%命中）
/// </summary>
[MemoryDiagnoser]
public class ValueTaskBenchmark
{
    private readonly Dictionary<int, string> _cache = new()
    {
        [1] = "cached value"
    };

    /// <summary>
    /// 基准方法：使用 Task<T>
    /// 即使同步完成也会分配 Task 对象
    /// </summary>
    [Benchmark(Baseline = true)]
    public Task<string> GetWithTask()
    {
        // 模拟缓存命中（同步完成）
        if (_cache.TryGetValue(1, out var value))
        {
            return Task.FromResult(value); // 分配 Task 对象
        }
        return Task.FromResult("default");
    }

    /// <summary>
    /// 优化方法：使用 ValueTask<T>
    /// 同步完成路径零分配
    /// </summary>
    [Benchmark]
    public ValueTask<string> GetWithValueTask()
    {
        // 模拟缓存命中（同步完成）
        if (_cache.TryGetValue(1, out var value))
        {
            return new ValueTask<string>(value); // 零分配！
        }
        return new ValueTask<string>("default");
    }
}

/// <summary>
/// Benchmark 使用指南
/// </summary>
public class BenchmarkGuide
{
    public static void PrintGuide()
    {
        Console.WriteLine("【BenchmarkDotNet 使用指南】\n");

        Console.WriteLine("1️⃣ 安装 NuGet 包：");
        Console.WriteLine("   dotnet add package BenchmarkDotNet\n");

        Console.WriteLine("2️⃣ 创建 Benchmark 类：");
        Console.WriteLine("   [MemoryDiagnoser]  // 诊断内存分配");
        Console.WriteLine("   public class MyBenchmark");
        Console.WriteLine("   {");
        Console.WriteLine("       [Benchmark(Baseline = true)]  // 基准方法");
        Console.WriteLine("       public void OldWay() { }");
        Console.WriteLine();
        Console.WriteLine("       [Benchmark]  // 优化方法");
        Console.WriteLine("       public void NewWay() { }");
        Console.WriteLine("   }\n");

        Console.WriteLine("3️⃣ 运行 Benchmark：");
        Console.WriteLine("   BenchmarkRunner.Run<MyBenchmark>();\n");

        Console.WriteLine("4️⃣ 查看报告：");
        Console.WriteLine("   - 生成在 BenchmarkDotNet.Artifacts 文件夹");
        Console.WriteLine("   - 包含 HTML、Markdown、CSV 格式报告\n");

        Console.WriteLine("⚠️  注意事项：");
        Console.WriteLine("   - 必须在 Release 模式下运行");
        Console.WriteLine("   - 关闭所有后台应用");
        Console.WriteLine("   - 避免在虚拟机中测试");
        Console.WriteLine("   - 测试时间可能较长（每个方法运行多次）\n");

        Console.WriteLine("📊 报告解读：");
        Console.WriteLine("   - Mean：平均执行时间");
        Console.WriteLine("   - Error：标准误差");
        Console.WriteLine("   - StdDev：标准差");
        Console.WriteLine("   - Gen0：GC 第 0 代回收次数");
        Console.WriteLine("   - Allocated：分配的内存大小\n");

        Console.WriteLine("💡 最佳实践：");
        Console.WriteLine("   1. 先优化算法，再优化内存分配");
        Console.WriteLine("   2. 用 Benchmark 验证优化效果");
        Console.WriteLine("   3. 关注 Allocated 列，减少分配是关键");
        Console.WriteLine("   4. 对比 Baseline，量化优化收益\n");
    }
}

/// <summary>
/// Benchmark 5：更真实的场景 - 日志解析
/// 对比字符串操作 vs Span 操作（处理但不创建字符串）
/// </summary>
[MemoryDiagnoser]
public class LogParsingBenchmark
{
    private const string LogLine = "2026-07-11 14:30:25.123 [INFO] User login: user@example.com from IP 192.168.1.100";

    [Benchmark(Baseline = true)]
    public int ParseWithSubstring()
    {
        // 传统字符串操作 - 大量分配
        var parts = LogLine.Split(' ');  // 分配数组和多个字符串
        string date = parts[0];
        string time = parts[1];
        string level = parts[2].Trim('[', ']');  // 再分配

        // 提取 IP 地址
        int ipIndex = LogLine.LastIndexOf(' ');
        string ip = LogLine.Substring(ipIndex + 1);  // 再分配

        // 返回长度总和（防止编译器优化掉）
        return date.Length + time.Length + level.Length + ip.Length;
    }

    [Benchmark]
    public int ParseWithSpan()
    {
        ReadOnlySpan<char> log = LogLine;

        // 使用 Span 零拷贝解析
        int firstSpace = log.IndexOf(' ');
        int secondSpace = log.Slice(firstSpace + 1).IndexOf(' ') + firstSpace + 1;
        int leftBracket = log.IndexOf('[');
        int rightBracket = log.IndexOf(']');
        int lastSpace = log.LastIndexOf(' ');

        // 只计算长度，不创建字符串
        var dateLen = firstSpace;
        var timeLen = secondSpace - firstSpace - 1;
        var levelLen = rightBracket - leftBracket - 1;
        var ipLen = log.Length - lastSpace - 1;

        return dateLen + timeLen + levelLen + ipLen;
    }
}

/// <summary>
/// Benchmark 6：更大的数据集 - 数组操作
/// 重点：展示内存分配的开销，而不是计算开销
/// </summary>
[MemoryDiagnoser]
public class LargeArrayBenchmark
{
    private readonly int[] _data = Enumerable.Range(1, 10000).ToArray();

    /// <summary>
    /// 基准方法：频繁复制小片段（放大分配开销）
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ProcessWithCopy()
    {
        // 复制 100 个小片段（每个 100 元素）
        int sum = 0;
        for (int i = 0; i < 100; i++)
        {
            int[] segment = new int[100];  // 频繁分配小数组
            Array.Copy(_data, i * 100, segment, 0, 100);

            // 简单求和（减少计算时间占比）
            for (int j = 0; j < segment.Length; j++)
                sum += segment[j];
        }
        return sum;
    }

    /// <summary>
    /// 优化方法：使用 Span 零拷贝
    /// </summary>
    [Benchmark]
    public int ProcessWithSpan()
    {
        // 使用 Span 零拷贝
        int sum = 0;
        for (int i = 0; i < 100; i++)
        {
            var segment = _data.AsSpan(i * 100, 100);

            // 同样的求和逻辑
            for (int j = 0; j < segment.Length; j++)
                sum += segment[j];
        }
        return sum;
    }
}

/// <summary>
/// Benchmark 6B：纯粹的切片性能测试（几乎没有计算）
/// 这个测试能真正体现 Span 的优势
/// </summary>
[MemoryDiagnoser]
public class ArraySlicingOnlyBenchmark
{
    private readonly int[] _data = Enumerable.Range(1, 10000).ToArray();

    [Benchmark(Baseline = true)]
    public int CopyOnly()
    {
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            int[] segment = new int[10];
            Array.Copy(_data, (i % 1000) * 10, segment, 0, 10);
            count += segment.Length;  // 极简计算
        }
        return count;
    }

    [Benchmark]
    public int SpanOnly()
    {
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            var segment = _data.AsSpan((i % 1000) * 10, 10);
            count += segment.Length;  // 极简计算
        }
        return count;
    }
}

/// <summary>
/// Benchmark 7：字符串构建 - StringBuilder vs Span
/// </summary>
[MemoryDiagnoser]
public class StringBuildingBenchmark
{
    [Benchmark(Baseline = true)]
    public string BuildWithStringBuilder()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append("Item");
            sb.Append(i);
            sb.Append(',');
        }
        return sb.ToString();
    }

    [Benchmark]
    public string BuildWithSpan()
    {
        Span<char> buffer = stackalloc char[1000];
        int pos = 0;

        for (int i = 0; i < 100; i++)
        {
            "Item".AsSpan().CopyTo(buffer.Slice(pos));
            pos += 4;

            // 简单的数字转换
            if (i < 10)
            {
                buffer[pos++] = (char)('0' + i);
            }
            else
            {
                buffer[pos++] = (char)('0' + i / 10);
                buffer[pos++] = (char)('0' + i % 10);
            }

            buffer[pos++] = ',';
        }

        return new string(buffer.Slice(0, pos));
    }
}

/// <summary>
/// Benchmark 8：CSV 解析 - 真实的字符串处理场景
/// 解析多行 CSV 数据，展示 Span 在批量处理中的优势
/// </summary>
[MemoryDiagnoser]
public class CsvParsingBenchmark
{
    private readonly string[] _csvLines =
    [
        "John,Doe,30,Engineer",
        "Jane,Smith,28,Designer",
        "Bob,Johnson,35,Manager",
        "Alice,Williams,32,Developer",
        "Charlie,Brown,29,Analyst"
    ];

    [Benchmark(Baseline = true)]
    public int ParseWithSplit()
    {
        int totalAge = 0;
        foreach (var line in _csvLines)
        {
            var parts = line.Split(',');  // 每行分配数组 + 4 个字符串
            totalAge += int.Parse(parts[2]);
        }
        return totalAge;
    }

    [Benchmark]
    public int ParseWithSpan()
    {
        int totalAge = 0;
        foreach (var line in _csvLines)
        {
            ReadOnlySpan<char> span = line;

            // 找到第二个逗号后的数字
            int firstComma = span.IndexOf(',');
            int secondComma = span.Slice(firstComma + 1).IndexOf(',') + firstComma + 1;
            int thirdComma = span.Slice(secondComma + 1).IndexOf(',') + secondComma + 1;

            // 提取年龄部分（零拷贝）
            var ageSpan = span.Slice(secondComma + 1, thirdComma - secondComma - 1);
            totalAge += int.Parse(ageSpan);
        }
        return totalAge;
    }
}

/* 
 * ====================================
 * 🎯 Benchmark 设计分析与建议
 * ====================================
 * 
 * 【当前设计评估】
 * 
 * ✅ 优点：
 * 1. StringParsingBenchmark - 很好地展示了 Substring vs Span 的差异
 * 2. ArraySlicingBenchmark - 清楚对比了数组复制 vs Span 切片
 * 3. 都使用了 [MemoryDiagnoser]，能看到内存分配
 * 4. 设置了 Baseline，便于对比
 * 
 * ⚠️ 需要注意的问题：
 * 
 * 1. ArrayPoolBenchmark 的问题：
 *    - 操作太简单（只是读写一个字节）
 *    - ArrayPool 的 Rent/Return 开销可能超过收益
 *    - 建议：增大数据量或增加操作复杂度
 * 
 * 2. ValueTaskBenchmark 的问题：
 *    - async/await 有额外的状态机开销
 *    - 同步完成路径的优势可能被掩盖
 *    - Task.Delay(1) 会引入实际的异步等待，影响测试结果
 *    - 建议：纯同步完成路径测试
 * 
 * 3. 所有测试的通用问题：
 *    - 某些操作太轻量，可能被编译器优化
 *    - 建议添加 [MethodImpl(MethodImplOptions.NoInlining)]
 * 
 * 【改进建议】
 * 
 * 1. ArrayPoolBenchmark 改进：
 *    - 使用更大的缓冲区（4KB+）
 *    - 添加实际的读写操作
 *    - 模拟多次使用场景
 * 
 * 2. ValueTaskBenchmark 改进：
 *    - 去掉 Task.Delay，纯测试缓存命中
 *    - 或者分别测试：100% 缓存命中 vs 50% 命中 vs 0% 命中
 * 
 * 3. 添加更真实的场景：
 *    ✅ LogParsingBenchmark - 模拟日志解析（已添加）
 *    ✅ LargeArrayBenchmark - 更大数据集的处理（已添加）
 *    ✅ StringBuildingBenchmark - 字符串构建场景（已添加）
 * 
 * 【运行建议】
 * 
 * 1. 必须在 Release 模式下运行：
 *    dotnet run -c Release
 * 
 * 2. 单独运行每个 Benchmark：
 *    BenchmarkRunner.Run<StringParsingBenchmark>();
 * 
 * 3. 运行所有 Benchmark：
 *    BenchmarkSwitcher.FromAssembly(typeof(Benchmarks).Assembly).Run(args);
 * 
 * 4. 关注的指标：
 *    - Mean：平均执行时间（越小越好）
 *    - Allocated：内存分配（越少越好）
 *    - Gen0/Gen1/Gen2：GC 回收次数（越少越好）
 *    - Ratio：相对于 Baseline 的倍数
 * 
 * 【预期结果】
 * 
 * StringParsingBenchmark:
 *   - Substring：~40-50ns，分配 ~96B
 *   - Span：~12-15ns，分配 0B
 *   - 提升：3-4倍，零内存分配
 * 
 * ArraySlicingBenchmark:
 *   - Array.Copy：~8-10ns，分配 ~48B
 *   - Span：<1ns，分配 0B
 *   - 提升：8-10倍，零内存分配
 * 
 * ArrayPoolBenchmark:
 *   - New Array：~100-150ns，分配 ~336B
 *   - ArrayPool：~15-25ns，分配 0B
 *   - 提升：5-8倍，零内存分配
 * 
 * ValueTaskBenchmark (同步完成):
 *   - Task：~20-30ns，分配 ~40B
 *   - ValueTask：~5-10ns，分配 0B
 *   - 提升：3-5倍，零内存分配
 * 
 * LogParsingBenchmark:
 *   - Split/Substring：~200-300ns，分配 ~500B
 *   - Span：~80-120ns，分配 ~200B
 *   - 提升：2-3倍，减少 60% 分配
 * 
 * LargeArrayBenchmark:
 *   - Copy：~50-80µs，分配 ~40KB
 *   - Span：~10-20µs，分配 0B
 *   - 提升：3-5倍，零额外分配
 */
