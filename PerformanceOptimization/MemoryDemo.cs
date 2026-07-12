using System.Buffers;

namespace PerformanceOptimization;

/// <summary>
/// Memory&lt;T&gt; 示例：可存储的 Span，支持异步
/// </summary>
public class MemoryDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n====================================");
        Console.WriteLine("Part 3: Memory<T> 示例");
        Console.WriteLine("====================================\n");

        // 示例 1：Memory 与 Span 的关系
        MemoryVsSpanDemo();

        // 示例 2：Memory 跨 await 边界
        await MemoryWithAsyncDemo();

        // 示例 3：MemoryPool 租赁模式
        await MemoryPoolDemo();

        // 示例 4：实战场景 - 异步文件读取
        await AsyncFileReadDemo();
    }

    /// <summary>
    /// 示例 1：Memory 与 Span 的关系
    /// </summary>
    private static void MemoryVsSpanDemo()
    {
        Console.WriteLine("【示例 1：Memory<T> 与 Span<T> 的关系】\n");

        byte[] array = new byte[10];
        for (int i = 0; i < array.Length; i++)
            array[i] = (byte)i;

        // Memory<T> 可以作为字段、可以装箱
        Memory<byte> memory = array;
        Console.WriteLine("✅ Memory<T> 是普通结构体，可以作为字段");

        // 获取 Span 进行操作
        Span<byte> span = memory.Span;
        Console.WriteLine($"✅ 通过 Memory.Span 获取底层 Span");
        Console.WriteLine($"   前 5 个元素: [{string.Join(", ", span.Slice(0, 5).ToArray())}]\n");

        // 对比
        Console.WriteLine("📊 对比：");
        Console.WriteLine("  Span<T>：");
        Console.WriteLine("    - ref struct，栈分配");
        Console.WriteLine("    - 不能作为字段");
        Console.WriteLine("    - 不能跨 await");
        Console.WriteLine("    - 性能最优");
        Console.WriteLine("  Memory<T>：");
        Console.WriteLine("    - 普通 struct，可堆分配");
        Console.WriteLine("    - 可以作为字段");
        Console.WriteLine("    - 可以跨 await");
        Console.WriteLine("    - 通过 .Span 获取 Span<T> 进行实际操作\n");
    }

    /// <summary>
    /// 示例 2：Memory 跨 await 边界
    /// </summary>
    private static async Task MemoryWithAsyncDemo()
    {
        Console.WriteLine("【示例 2：Memory<T> 跨 await 边界】\n");

        byte[] buffer = new byte[1024];

        // ❌ Span 不能跨 await（以下代码会编译错误）
        Console.WriteLine("❌ Span<T> 不能跨 await：");
        Console.WriteLine("   public async Task ProcessAsync(Span<byte> buffer)");
        Console.WriteLine("   {");
        Console.WriteLine("       await Task.Delay(100); // ❌ 编译错误！");
        Console.WriteLine("       Process(buffer);");
        Console.WriteLine("   }\n");

        // ✅ Memory 可以跨 await
        Console.WriteLine("✅ Memory<T> 可以跨 await：");
        Memory<byte> memory = buffer;
        await ProcessWithMemoryAsync(memory);
        Console.WriteLine($"   ✅ 成功跨越 await 边界\n");
    }

    private static async Task ProcessWithMemoryAsync(Memory<byte> memory)
    {
        Console.WriteLine("   > 进入异步方法");
        await Task.Delay(50); // 模拟异步操作
        Console.WriteLine("   > await 完成");

        // 获取 Span 进行实际操作
        Span<byte> span = memory.Span;
        span[0] = 42;
        Console.WriteLine($"   > 修改了第一个字节: {span[0]}");
    }

    /// <summary>
    /// 示例 3：MemoryPool 租赁模式
    /// </summary>
    private static async Task MemoryPoolDemo()
    {
        Console.WriteLine("【示例 3：MemoryPool<T> 租赁模式】\n");

        Console.WriteLine("💡 MemoryPool 类似 ArrayPool，但返回的是 IMemoryOwner<T>");

        // ✅ 使用 MemoryPool 租借内存
        using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(4096);
        Console.WriteLine($"✅ 租借了 {owner.Memory.Length} 字节的内存");

        Memory<byte> memory = owner.Memory;
        Console.WriteLine($"✅ 获取 Memory<byte>，可以跨 await 使用");

        // 模拟异步操作
        await Task.Delay(50);

        // 使用 Memory
        Span<byte> span = memory.Span;
        span[0] = 100;
        Console.WriteLine($"✅ 写入数据: {span[0]}");

        Console.WriteLine("✅ using 结束，自动归还内存到池中\n");

        // 注意事项
        Console.WriteLine("⚠️  注意事项：");
        Console.WriteLine("  1. 租借的大小可能大于请求的大小");
        Console.WriteLine("  2. 使用 using 确保释放（归还到池）");
        Console.WriteLine("  3. 归还后不要再使用 Memory（未定义行为）\n");
    }

    /// <summary>
    /// 示例 4：实战场景 - 异步文件读取
    /// </summary>
    private static async Task AsyncFileReadDemo()
    {
        Console.WriteLine("【示例 4：实战场景 - 异步文件读取】\n");

        string tempFile = Path.GetTempFileName();
        try
        {
            // 写入测试数据
            await File.WriteAllTextAsync(tempFile, "Hello, Memory<T>! This is a test file.");

            // ❌ 传统做法：分配新数组
            Console.WriteLine("❌ 传统做法：");
            byte[] traditionalBuffer = new byte[1024];
            await using (var fs = File.OpenRead(tempFile))
            {
                int bytesRead = await fs.ReadAsync(traditionalBuffer, 0, traditionalBuffer.Length);
                Console.WriteLine($"   读取 {bytesRead} 字节");
                Console.WriteLine($"   ⚠️  每次调用都分配新数组\n");
            }

            // ✅ Memory 优化：使用 ArrayPool + Memory
            Console.WriteLine("✅ Memory 优化（结合 ArrayPool）：");
            byte[] pooledBuffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                Memory<byte> memory = pooledBuffer.AsMemory();
                await using (var fs = File.OpenRead(tempFile))
                {
                    int bytesRead = await fs.ReadAsync(memory); // 直接传递 Memory<byte>
                    Console.WriteLine($"   读取 {bytesRead} 字节");
                    Console.WriteLine($"   ✅ 使用池化数组，零额外分配\n");

                    // 可以继续跨 await 使用
                    await Task.Delay(10);
                    var content = System.Text.Encoding.UTF8.GetString(memory.Span.Slice(0, bytesRead));
                    Console.WriteLine($"   内容: {content}\n");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pooledBuffer);
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// 实战类：支持 Memory 的缓冲区管理器
    /// </summary>
    public class BufferManager
    {
        private readonly MemoryPool<byte> _pool = MemoryPool<byte>.Shared;

        /// <summary>
        /// 租借缓冲区
        /// </summary>
        public IMemoryOwner<byte> RentBuffer(int minimumSize)
        {
            return _pool.Rent(minimumSize);
        }

        /// <summary>
        /// 异步处理数据
        /// </summary>
        public async Task<int> ProcessDataAsync(Memory<byte> data)
        {
            // 模拟异步处理
            await Task.Delay(50);

            // 操作 Memory
            Span<byte> span = data.Span;
            int sum = 0;
            foreach (var b in span)
                sum += b;

            return sum;
        }

        /// <summary>
        /// 演示完整流程
        /// </summary>
        public static async Task DemoAsync()
        {
            Console.WriteLine("【BufferManager 演示】\n");

            var manager = new BufferManager();

            // 租借缓冲区
            using IMemoryOwner<byte> owner = manager.RentBuffer(256);
            Memory<byte> memory = owner.Memory.Slice(0, 256);

            // 填充数据
            for (int i = 0; i < 256; i++)
                memory.Span[i] = (byte)i;

            Console.WriteLine("✅ 填充了 256 字节数据");

            // 异步处理（跨 await）
            int sum = await manager.ProcessDataAsync(memory);
            Console.WriteLine($"✅ 异步处理完成，数据之和: {sum}");

            Console.WriteLine("✅ using 结束，自动归还缓冲区\n");
        }
    }

    /// <summary>
    /// 选择指南
    /// </summary>
    public static void PrintUsageGuidelines()
    {
        Console.WriteLine("【Span<T> vs Memory<T> 选择指南】\n");

        Console.WriteLine("✅ 优先使用 Span<T> 的场景：");
        Console.WriteLine("  - 同步方法（不跨 await）");
        Console.WriteLine("  - 方法内局部变量");
        Console.WriteLine("  - 性能要求极致（栈分配）\n");

        Console.WriteLine("✅ 必须使用 Memory<T> 的场景：");
        Console.WriteLine("  - 异步方法（跨 await 边界）");
        Console.WriteLine("  - 需要作为类的字段");
        Console.WriteLine("  - 需要存储到集合中\n");

        Console.WriteLine("💡 最佳实践：");
        Console.WriteLine("  1. 公共 API：同时提供 Span<T> 和 Memory<T> 重载");
        Console.WriteLine("     - Span<T> 重载：同步场景，性能最优");
        Console.WriteLine("     - Memory<T> 重载：异步场景，灵活性更好");
        Console.WriteLine("  2. 内部实现：能用 Span<T> 就用 Span<T>");
        Console.WriteLine("  3. 与 ArrayPool 配合：Memory<T> + ArrayPool = 零分配异步\n");
    }
}
