using System.Text;

namespace ProductionDiagnostics;

/// <summary>
/// 内存泄漏演示：模拟常见的内存泄漏场景
/// 诊断工具：dotnet-gcdump、Visual Studio 内存分析器
/// </summary>
public static class MemoryLeakDemo
{
    // 场景1：事件未取消订阅
    public class EventPublisher
    {
        public event EventHandler<string>? DataReceived;

        public void PublishData(string data)
        {
            DataReceived?.Invoke(this, data);
        }
    }

    public class EventSubscriber
    {
        private readonly EventPublisher _publisher;
        private readonly byte[] _largeData = new byte[1024 * 1024]; // 1MB 数据

        public EventSubscriber(EventPublisher publisher)
        {
            _publisher = publisher;
            _publisher.DataReceived += OnDataReceived; // 订阅事件（没有取消订阅会导致泄漏）
        }

        private void OnDataReceived(object? sender, string data)
        {
            if (_largeData.Length > 0)
            {
                // 假装处理数据，避免_largeData没有使用，被JIT优化掉
                _largeData[0] = 1;
            }
        }

        // ❌ 忘记取消订阅
        // ~EventSubscriber()
        // {
        //     _publisher.DataReceived -= OnDataReceived;
        // }
    }

    // 场景2：静态字段持有大对象
    private static readonly List<byte[]> _staticCache = new();

    // 场景3：CancellationTokenSource 未释放
    private static readonly List<CancellationTokenSource> _tokenSources = new();

    public static void Run()
    {
        Console.WriteLine("\n=== 内存泄漏演示 ===");
        Console.WriteLine("将模拟三种常见的内存泄漏场景\n");
        Console.WriteLine("⚠️  建议使用以下工具诊断：");
        Console.WriteLine("1. dotnet-gcdump collect -p <进程ID>");
        Console.WriteLine("2. Visual Studio → 调试 → 性能分析器 → .NET 对象分配跟踪");
        Console.WriteLine("3. dotMemory（JetBrains）\n");

        Console.WriteLine("请选择要演示的泄漏场景：");
        Console.WriteLine("1. 事件未取消订阅");
        Console.WriteLine("2. 静态字段持有大对象");
        Console.WriteLine("3. CancellationTokenSource 未释放");
        Console.WriteLine("4. 闭包捕获大对象");
        Console.WriteLine("0. 返回\n");

        Console.Write("请输入选项: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                DemoEventLeak();
                break;
            case "2":
                DemoStaticFieldLeak();
                break;
            case "3":
                DemoCancellationTokenLeak();
                break;
            case "4":
                DemoClosureLeak();
                break;
            case "0":
                return;
            default:
                Console.WriteLine("无效的选项");
                break;
        }
    }

    private static readonly EventPublisher _publisher = new(); 

    private static void DemoEventLeak()
    {
        Console.WriteLine("\n--- 场景1：事件未取消订阅导致内存泄漏 ---\n");

        var memoryBefore = GC.GetTotalMemory(true) / 1024 / 1024;
        Console.WriteLine($"泄漏前内存: {memoryBefore}MB");

        Console.WriteLine("创建 100 个订阅者（每个持有 1MB 数据）...");
        for (int i = 0; i < 100; i++)
        {
            var subscriber = new EventSubscriber(_publisher);
            // subscriber 离开作用域，但由于事件订阅，无法被 GC 回收
        }

        Console.WriteLine("订阅者已离开作用域，触发 GC...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
        Console.WriteLine($"泄漏后内存: {memoryAfter}MB");
        Console.WriteLine($"泄漏内存: {memoryAfter - memoryBefore}MB\n");

        Console.WriteLine("❌ 问题分析：");
        Console.WriteLine("- EventPublisher 持有 EventSubscriber 的引用（通过委托）");
        Console.WriteLine("- 即使 EventSubscriber 离开作用域，也无法被 GC 回收");
        Console.WriteLine("- 每个 EventSubscriber 持有 1MB 数据，导致内存泄漏\n");

        Console.WriteLine("✅ 解决方案：");
        Console.WriteLine("1. 在 Dispose 或析构函数中取消订阅：");
        Console.WriteLine("   _publisher.DataReceived -= OnDataReceived;");
        Console.WriteLine("2. 使用弱事件模式（WeakEventManager）");
        Console.WriteLine("3. 使用 IObservable/IObserver（Rx.NET）自动管理订阅");
    }

    private static void DemoStaticFieldLeak()
    {
        Console.WriteLine("\n--- 场景2：静态字段持有大对象导致内存泄漏 ---\n");

        var memoryBefore = GC.GetTotalMemory(true) / 1024 / 1024;
        Console.WriteLine($"泄漏前内存: {memoryBefore}MB");

        Console.WriteLine("向静态缓存中添加 100 个 1MB 对象...");
        for (int i = 0; i < 100; i++)
        {
            _staticCache.Add(new byte[1024 * 1024]);
        }

        Console.WriteLine("触发 GC...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
        Console.WriteLine($"泄漏后内存: {memoryAfter}MB");
        Console.WriteLine($"泄漏内存: {memoryAfter - memoryBefore}MB\n");

        Console.WriteLine("❌ 问题分析：");
        Console.WriteLine("- 静态字段在应用程序生命周期内永远存活");
        Console.WriteLine("- _staticCache 持有的对象永远不会被 GC 回收");
        Console.WriteLine("- 随着时间推移，缓存越来越大\n");

        Console.WriteLine("✅ 解决方案：");
        Console.WriteLine("1. 使用 MemoryCache 并设置过期策略：");
        Console.WriteLine("   var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });");
        Console.WriteLine("2. 使用弱引用（WeakReference）存储不重要的数据");
        Console.WriteLine("3. 定期清理缓存（设置 TTL）");
        Console.WriteLine("4. 监控缓存大小，超过阈值时清理");

        // 清理演示数据
        _staticCache.Clear();
    }

    private static void DemoCancellationTokenLeak()
    {
        Console.WriteLine("\n--- 场景3：CancellationTokenSource 未释放导致内存泄漏 ---\n");

        var memoryBefore = GC.GetTotalMemory(true) / 1024 / 1024;
        Console.WriteLine($"泄漏前内存: {memoryBefore}MB");

        Console.WriteLine("创建 10000 个 CancellationTokenSource（未释放）...");
        for (int i = 0; i < 10000; i++)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // 注册定时器
            _tokenSources.Add(cts); // 保持引用（模拟忘记释放）
        }

        Console.WriteLine("触发 GC...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
        Console.WriteLine($"泄漏后内存: {memoryAfter}MB");
        Console.WriteLine($"泄漏内存: {memoryAfter - memoryBefore}MB\n");

        Console.WriteLine("❌ 问题分析：");
        Console.WriteLine("- CancellationTokenSource 内部注册了 Timer");
        Console.WriteLine("- 未调用 Dispose，Timer 不会被释放");
        Console.WriteLine("- 每个 CTS 占用约 1-2KB 内存（包括 Timer）\n");

        Console.WriteLine("✅ 解决方案：");
        Console.WriteLine("1. 始终使用 using 语句：");
        Console.WriteLine("   using var cts = new CancellationTokenSource();");
        Console.WriteLine("2. 或在 finally 块中调用 Dispose：");
        Console.WriteLine("   try { ... } finally { cts?.Dispose(); }");
        Console.WriteLine("3. 使用 CancellationTokenSource.CreateLinkedTokenSource 时也要记得释放");

        // 清理演示数据
        foreach (var cts in _tokenSources)
        {
            cts.Dispose();
        }
        _tokenSources.Clear();
    }

    private static void DemoClosureLeak()
    {
        Console.WriteLine("\n--- 场景4：闭包捕获大对象导致内存泄漏 ---\n");

        var memoryBefore = GC.GetTotalMemory(true) / 1024 / 1024;
        Console.WriteLine($"泄漏前内存: {memoryBefore}MB");

        var tasks = new List<Task>();
        Console.WriteLine("创建 100 个长时间运行的任务，闭包捕获大对象...");

        for (int i = 0; i < 100; i++)
        {
            byte[] largeData = new byte[1024 * 1024]; // 1MB
            StringBuilder sb = new StringBuilder();
            sb.Append($"Task {i}");

            // ❌ 错误：闭包捕获了 largeData
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(1000);
                // 只使用 sb，但闭包同时捕获了 largeData
                Console.Write(".");
            }));
        }

        Console.WriteLine($"\n已启动 {tasks.Count} 个任务，每个闭包持有 1MB 数据");
        Console.WriteLine("触发 GC（任务尚未完成）...");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
        Console.WriteLine($"泄漏后内存: {memoryAfter}MB");
        Console.WriteLine($"占用内存: {memoryAfter - memoryBefore}MB\n");

        Console.WriteLine("❌ 问题分析：");
        Console.WriteLine("- Lambda 表达式形成闭包，捕获了所有局部变量");
        Console.WriteLine("- 即使只使用 sb，largeData 也被捕获");
        Console.WriteLine("- 任务未完成时，闭包持有的对象无法被 GC 回收\n");

        Console.WriteLine("✅ 解决方案：");
        Console.WriteLine("1. 避免在长时间运行的任务中捕获大对象");
        Console.WriteLine("2. 显式释放不需要的引用：");
        Console.WriteLine("   var data = largeData;");
        Console.WriteLine("   largeData = null; // 显式释放");
        Console.WriteLine("   Task.Run(() => Process(data));");
        Console.WriteLine("3. 将数据传递为参数，而不是通过闭包捕获");
        Console.WriteLine("4. 使用局部函数或单独的方法");

        // 等待任务完成
        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("\n任务完成，闭包释放");
    }
}
