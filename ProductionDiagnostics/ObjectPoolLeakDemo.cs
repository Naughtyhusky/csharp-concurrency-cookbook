using System.Collections.Concurrent;

namespace ProductionDiagnostics;

/// <summary>
/// 对象池内存泄漏演示：模拟对象池使用不当导致的内存泄漏
/// </summary>
public static class ObjectPoolLeakDemo
{
    // 模拟一个简单的对象池
    private class SimpleObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentBag<T> _objects = new();
        private int _objectCount;
        private readonly int _maxSize;

        public SimpleObjectPool(int maxSize = 100)
        {
            _maxSize = maxSize;
        }

        public T Rent()
        {
            if (_objects.TryTake(out var obj))
            {
                return obj;
            }

            Interlocked.Increment(ref _objectCount);
            return new T();
        }

        public void Return(T obj)
        {
            // ❌ 错误：没有限制池大小
            _objects.Add(obj);
        }

        public int Count => _objects.Count;
        public int TotalCreated => _objectCount;
    }

    // 测试对象
    private class LargeObject
    {
        public byte[] Data { get; } = new byte[1024 * 1024]; // 1MB
        public int Id { get; set; }
    }

    public static void Run()
    {
        Console.WriteLine("\n=== 对象池内存泄漏演示 ===");
        Console.WriteLine("演示对象池使用不当导致的内存泄漏\n");

        Console.WriteLine("请选择要演示的场景：");
        Console.WriteLine("1. 对象池无限增长（未限制大小）");
        Console.WriteLine("2. 对象池使用正确（有大小限制）");
        Console.WriteLine("0. 返回\n");

        Console.Write("请输入选项: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                DemoUnboundedPool();
                break;
            case "2":
                DemoBoundedPool();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("无效的选项");
                break;
        }
    }

    private static void DemoUnboundedPool()
    {
        Console.WriteLine("\n--- 场景1：对象池无限增长（未限制大小）---\n");

        var pool = new SimpleObjectPool<LargeObject>();
        var memoryBefore = GC.GetTotalMemory(true) / 1024 / 1024;
        Console.WriteLine($"初始内存: {memoryBefore}MB\n");

        Console.WriteLine("模拟使用对象池处理 1000 个请求...");
        Console.WriteLine("每个请求租借对象，使用后归还\n");

        // 模拟高峰期：租借但很多对象不立即归还
        var rentedObjects = new List<LargeObject>();
        for (int i = 0; i < 1000; i++)
        {
            var obj = pool.Rent();
            obj.Id = i;

            // 模拟处理
            Thread.Sleep(1);

            // 模拟高峰期：30% 的对象延迟归还
            if (i % 3 == 0)
            {
                rentedObjects.Add(obj); // 延迟归还
            }
            else
            {
                pool.Return(obj); // 立即归还
            }

            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"  已处理 {i + 1} 个请求，池中对象数: {pool.Count}，总创建: {pool.TotalCreated}");
            }
        }

        Console.WriteLine("\n高峰期结束，归还所有延迟对象...");
        foreach (var obj in rentedObjects)
        {
            pool.Return(obj);
        }

        var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
        Console.WriteLine($"\n最终内存: {memoryAfter}MB");
        Console.WriteLine($"池中对象数: {pool.Count}");
        Console.WriteLine($"总创建对象: {pool.TotalCreated}");
        Console.WriteLine($"内存占用: {memoryAfter - memoryBefore}MB");

        Console.WriteLine("\n❌ 问题分析：");
        Console.WriteLine("- 对象池没有大小限制");
        Console.WriteLine("- 高峰期创建了大量对象");
        Console.WriteLine("- 高峰过后，这些对象仍保留在池中");
        Console.WriteLine("- 导致内存无法释放（即使没有使用）\n");

        Console.WriteLine("✅ 解决方案：");
        Console.WriteLine("1. 限制对象池最大大小");
        Console.WriteLine("2. 超过最大大小时，丢弃归还的对象");
        Console.WriteLine("3. 使用 ArrayPool<T>.Shared（内置大小限制）");
        Console.WriteLine("4. 定期清理闲置对象（TTL 机制）");
    }

    private static void DemoBoundedPool()
    {
        Console.WriteLine("\n--- 场景2：对象池使用正确（有大小限制）---\n");

        var pool = new BoundedObjectPool<LargeObject>(maxSize: 100);
        var memoryBefore = GC.GetTotalMemory(true) / 1024 / 1024;
        Console.WriteLine($"初始内存: {memoryBefore}MB");
        Console.WriteLine($"对象池最大大小: {pool.MaxSize}\n");

        Console.WriteLine("模拟使用对象池处理 1000 个请求...");

        var rentedObjects = new List<LargeObject>();
        for (int i = 0; i < 1000; i++)
        {
            var obj = pool.Rent();
            obj.Id = i;

            Thread.Sleep(1);

            if (i % 3 == 0)
            {
                rentedObjects.Add(obj);
            }
            else
            {
                pool.Return(obj);
            }

            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"  已处理 {i + 1} 个请求，池中对象数: {pool.Count}，总创建: {pool.TotalCreated}");
            }
        }

        Console.WriteLine("\n高峰期结束，归还所有延迟对象...");
        foreach (var obj in rentedObjects)
        {
            pool.Return(obj);
        }

        // 触发 GC，清理未归还池的对象
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
        Console.WriteLine($"\n最终内存: {memoryAfter}MB");
        Console.WriteLine($"池中对象数: {pool.Count}");
        Console.WriteLine($"总创建对象: {pool.TotalCreated}");
        Console.WriteLine($"内存占用: {memoryAfter - memoryBefore}MB");

        Console.WriteLine("\n✅ 优化效果：");
        Console.WriteLine("- 对象池大小限制在 100 个");
        Console.WriteLine("- 超过限制的对象被丢弃，可被 GC 回收");
        Console.WriteLine("- 内存占用可控");
        Console.WriteLine("- 平衡了性能和内存占用");
    }

    // 改进的对象池：有大小限制
    private class BoundedObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentBag<T> _objects = new();
        private int _objectCount;
        public int MaxSize { get; }

        public BoundedObjectPool(int maxSize = 100)
        {
            MaxSize = maxSize;
        }

        public T Rent()
        {
            if (_objects.TryTake(out var obj))
            {
                return obj;
            }

            Interlocked.Increment(ref _objectCount);
            return new T();
        }

        public void Return(T obj)
        {
            // ✅ 正确：限制池大小
            if (_objects.Count < MaxSize)
            {
                _objects.Add(obj);
            }
            // else: 丢弃对象，让 GC 回收
        }

        public int Count => _objects.Count;
        public int TotalCreated => _objectCount;
    }
}
