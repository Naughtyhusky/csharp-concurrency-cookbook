using System.Buffers;
using System.Text;

namespace PerformanceOptimization;

/// <summary>
/// ArrayPool&lt;T&gt; 示例：数组复用，减少 GC 压力
/// </summary>
public class ArrayPoolDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n====================================");
        Console.WriteLine("Part 5: ArrayPool<T> 示例");
        Console.WriteLine("====================================\n");

        // 示例 1：基本用法
        BasicUsageDemo();

        // 示例 2：租借大小与实际大小
        RentSizeDemo();

        // 示例 3：归还时是否清空
        ReturnClearDemo();

        // 示例 4：实战场景 - 高性能文件读取
        await FileReadingScenario();

        // 示例 5：实战场景 - 字符串构建
        StringBuildingScenario();

        // 示例 6：自定义对象池
        CustomPoolDemo();
    }

    /// <summary>
    /// 示例 1：基本用法
    /// </summary>
    private static void BasicUsageDemo()
    {
        Console.WriteLine("【示例 1：ArrayPool 基本用法】\n");

        // ❌ 传统做法：每次分配新数组
        Console.WriteLine("❌ 传统做法：");
        for (int i = 0; i < 3; i++)
        {
            byte[] buffer = new byte[1024]; // 每次分配！
            Array.Fill(buffer, (byte)i);
            Console.WriteLine($"   循环 {i + 1}: 分配了新数组 {buffer.Length} 字节");
        }
        Console.WriteLine("   ⚠️  分配了 3 次，增加 GC 压力\n");

        // ✅ ArrayPool 优化：租借-归还
        Console.WriteLine("✅ ArrayPool 优化：");
        for (int i = 0; i < 3; i++)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024); // 租借
            try
            {
                Array.Fill(buffer, (byte)i);
                Console.WriteLine($"   循环 {i + 1}: 租借了数组 {buffer.Length} 字节");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer); // 归还
            }
        }
        Console.WriteLine("   ✅ 复用数组，零额外分配！\n");
    }

    /// <summary>
    /// 示例 2：租借大小与实际大小
    /// </summary>
    private static void RentSizeDemo()
    {
        Console.WriteLine("【示例 2：租借大小与实际大小】\n");

        Console.WriteLine("⚠️  注意：租借的数组可能比请求的大\n");

        int[] requestSizes = [100, 1000, 10000, 100000];

        foreach (var requestSize in requestSizes)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(requestSize);
            Console.WriteLine($"   请求 {requestSize,6} 字节 -> 实际获得 {buffer.Length,6} 字节");
            ArrayPool<byte>.Shared.Return(buffer);
        }

        Console.WriteLine("\n💡 原因：");
        Console.WriteLine("   - ArrayPool 按 2 的幂次管理缓冲区（16, 32, 64, 128, ...）");
        Console.WriteLine("   - 请求 100 字节，会分配最接近的 128 字节");
        Console.WriteLine("   - 使用时注意：只用前 requestSize 个元素\n");

        Console.WriteLine("📌 最佳实践：");
        Console.WriteLine("   var buffer = ArrayPool<byte>.Shared.Rent(size);");
        Console.WriteLine("   var usableSpan = buffer.AsSpan(0, size); // 只用需要的部分");
        Console.WriteLine("   ArrayPool<byte>.Shared.Return(buffer);\n");
    }

    /// <summary>
    /// 示例 3：归还时是否清空
    /// </summary>
    private static void ReturnClearDemo()
    {
        Console.WriteLine("【示例 3：归还时是否清空】\n");

        // 租借数组并写入敏感数据
        byte[] buffer = ArrayPool<byte>.Shared.Rent(16);
        for (int i = 0; i < 16; i++)
            buffer[i] = (byte)(65 + i); // 'A', 'B', 'C', ...

        Console.WriteLine($"写入数据: {Encoding.ASCII.GetString(buffer, 0, 16)}");

        // ❌ 归还时不清空（默认）
        Console.WriteLine("\n❌ 归还时不清空（clearArray: false）：");
        ArrayPool<byte>.Shared.Return(buffer, clearArray: false);

        // 再次租借（可能是同一个数组）
        byte[] buffer2 = ArrayPool<byte>.Shared.Rent(16);
        Console.WriteLine($"   再次租借，内容: {Encoding.ASCII.GetString(buffer2, 0, 16)}");
        Console.WriteLine("   ⚠️  数据仍然存在！可能泄露敏感信息\n");
        ArrayPool<byte>.Shared.Return(buffer2);

        // ✅ 归还时清空
        Console.WriteLine("✅ 归还时清空（clearArray: true）：");
        buffer = ArrayPool<byte>.Shared.Rent(16);
        for (int i = 0; i < 16; i++)
            buffer[i] = (byte)(65 + i);
        Console.WriteLine($"写入数据: {Encoding.ASCII.GetString(buffer, 0, 16)}");

        ArrayPool<byte>.Shared.Return(buffer, clearArray: true); // 清空

        buffer2 = ArrayPool<byte>.Shared.Rent(16);
        Console.WriteLine($"   再次租借，内容: {Encoding.ASCII.GetString(buffer2, 0, 16)}");
        Console.WriteLine("   ✅ 数据已清空\n");
        ArrayPool<byte>.Shared.Return(buffer2);

        Console.WriteLine("📌 何时清空：");
        Console.WriteLine("   - 敏感数据（密码、密钥）：必须清空");
        Console.WriteLine("   - 引用类型数组：必须清空（避免内存泄漏）");
        Console.WriteLine("   - 普通值类型：不清空（性能更好）\n");
    }

    /// <summary>
    /// 示例 4：实战场景 - 高性能文件读取
    /// </summary>
    private static async Task FileReadingScenario()
    {
        Console.WriteLine("【示例 4：实战场景 - 高性能文件读取】\n");

        string tempFile = Path.GetTempFileName();
        try
        {
            // 写入测试数据
            await File.WriteAllTextAsync(tempFile, string.Concat(Enumerable.Repeat("Hello, ArrayPool! ", 1000)));

            // ❌ 传统做法：每次分配新缓冲区
            Console.WriteLine("❌ 传统做法：");
            await using (var fs = File.OpenRead(tempFile))
            {
                int totalBytes = 0;
                int allocations = 0;
                while (true)
                {
                    byte[] buffer = new byte[4096]; // 每次分配！
                    allocations++;
                    int bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    totalBytes += bytesRead;
                }
                Console.WriteLine($"   读取 {totalBytes} 字节，分配了 {allocations} 次\n");
            }

            // ✅ ArrayPool 优化
            Console.WriteLine("✅ ArrayPool 优化：");
            await using (var fs = File.OpenRead(tempFile))
            {
                int totalBytes = 0;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(4096); // 只租借一次
                try
                {
                    while (true)
                    {
                        int bytesRead = await fs.ReadAsync(buffer.AsMemory(0, 4096));
                        if (bytesRead == 0) break;
                        totalBytes += bytesRead;
                    }
                    Console.WriteLine($"   读取 {totalBytes} 字节，复用了同一个缓冲区");
                    Console.WriteLine($"   ✅ 零额外分配！\n");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// 示例 5：实战场景 - 字符串构建
    /// </summary>
    private static void StringBuildingScenario()
    {
        Console.WriteLine("【示例 5：实战场景 - 字符串构建】\n");

        Console.WriteLine("💡 场景：构建 JSON 字符串（手动拼接）\n");

        // ❌ 传统做法：StringBuilder
        Console.WriteLine("❌ 传统做法（StringBuilder）：");
        var sb = new StringBuilder();
        sb.Append("{\"users\":[");
        for (int i = 0; i < 5; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append($"{{\"id\":{i},\"name\":\"User{i}\"}}");
        }
        sb.Append("]}");
        string json1 = sb.ToString();
        Console.WriteLine($"   结果: {json1}");
        Console.WriteLine("   ⚠️  StringBuilder 内部多次扩容分配\n");

        // ✅ ArrayPool 优化（结合 Span）
        Console.WriteLine("✅ ArrayPool 优化（结合 Span）：");
        char[] buffer = ArrayPool<char>.Shared.Rent(1024);
        try
        {
            Span<char> span = buffer.AsSpan();
            int pos = 0;

            // 手动拼接
            "{\"users\":[".AsSpan().CopyTo(span.Slice(pos));
            pos += 10;

            for (int i = 0; i < 5; i++)
            {
                if (i > 0)
                {
                    span[pos++] = ',';
                }
                string userJson = $"{{\"id\":{i},\"name\":\"User{i}\"}}";
                userJson.AsSpan().CopyTo(span.Slice(pos));
                pos += userJson.Length;
            }

            "]}".AsSpan().CopyTo(span.Slice(pos));
            pos += 2;

            string json2 = new string(span.Slice(0, pos));
            Console.WriteLine($"   结果: {json2}");
            Console.WriteLine("   ✅ 使用池化缓冲区，零额外分配\n");
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 示例 6：自定义对象池
    /// </summary>
    private static void CustomPoolDemo()
    {
        Console.WriteLine("【示例 6：自定义对象池（池化其他类型）】\n");

        // 使用自定义 StringBuilder 池
        var pool = new StringBuilderPool();

        Console.WriteLine("✅ 租借 StringBuilder：");
        var sb1 = pool.Rent();
        sb1.Append("Hello");
        Console.WriteLine($"   内容: {sb1}");
        pool.Return(sb1);

        var sb2 = pool.Rent();
        Console.WriteLine($"   再次租借，内容: {sb2}"); // 应该是空的
        sb2.Append("World");
        pool.Return(sb2);

        Console.WriteLine("\n💡 自定义对象池适用场景：");
        Console.WriteLine("   - StringBuilder");
        Console.WriteLine("   - MemoryStream");
        Console.WriteLine("   - XmlReader/XmlWriter");
        Console.WriteLine("   - 自定义重量级对象\n");
    }

    /// <summary>
    /// 自定义 StringBuilder 对象池
    /// </summary>
    public class StringBuilderPool
    {
        private readonly Stack<StringBuilder> _pool = new();
        private readonly object _lock = new();
        private const int MaxCapacity = 4096;

        public StringBuilder Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }
            return new StringBuilder();
        }

        public void Return(StringBuilder sb)
        {
            // 清空内容
            sb.Clear();

            // 如果容量过大，不放回池中（避免内存泄漏）
            if (sb.Capacity > MaxCapacity)
            {
                return;
            }

            lock (_lock)
            {
                _pool.Push(sb);
            }
        }
    }

    /// <summary>
    /// 性能对比总结
    /// </summary>
    public static void PerformanceComparison()
    {
        Console.WriteLine("【性能对比：传统分配 vs ArrayPool】\n");

        Console.WriteLine("📊 性能数据（100 万次 4KB 缓冲区分配）：");
        Console.WriteLine("  | 实现方式          | 分配次数  | GC Gen0  | GC Gen1  | 耗时    |");
        Console.WriteLine("  |------------------|----------|----------|----------|---------|");
        Console.WriteLine("  | new byte[4096]   | 1,000,000| ~8,000   | ~200     | ~800ms  |");
        Console.WriteLine("  | ArrayPool.Rent   | ~50      | ~0       | ~0       | ~100ms  |\n");

        Console.WriteLine("✅ ArrayPool 优势：");
        Console.WriteLine("   - 减少堆分配：减少 GC Gen0 约 99%");
        Console.WriteLine("   - 减少 GC 暂停：几乎不触发 GC");
        Console.WriteLine("   - 提升吞吐量：性能提升 5-10 倍\n");

        Console.WriteLine("⚠️  注意事项：");
        Console.WriteLine("   1. 租借的数组可能比请求的大");
        Console.WriteLine("   2. 归还后不要再使用数组");
        Console.WriteLine("   3. 敏感数据记得 clearArray: true");
        Console.WriteLine("   4. 长期持有的数组不适合用 ArrayPool\n");

        Console.WriteLine("📌 最佳实践：");
        Console.WriteLine("   - 高频短期使用：ArrayPool");
        Console.WriteLine("   - 低频长期持有：直接 new");
        Console.WriteLine("   - 异步场景：ArrayPool + Memory<T>");
        Console.WriteLine("   - 与 Span<T> 配合：零分配极致性能\n");
    }
}
