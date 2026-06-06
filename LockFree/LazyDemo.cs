namespace LockFree;

/// <summary>
/// Lazy&lt;T&gt; 与 LazyInitializer 示例
/// 演示：线程安全的延迟初始化
/// </summary>
public static class LazyDemo
{
    // ---- 示例1：Lazy<T> 基本用法 ----

    // 模拟一个"重型"对象，创建成本很高
    public class HeavyService(string name)
    {
        public string Name { get; } = name;
        public DateTime CreatedAt { get; } = DateTime.Now;

        public string Process(string input) => $"[{Name}] processed: {input}";
    }

    // ✅ 线程安全的延迟初始化：只有第一次访问 .Value 时才会创建
    // 默认使用 LazyThreadSafetyMode.ExecutionAndPublication（最安全）
    private static readonly Lazy<HeavyService> _lazyService = new(() =>
    {
        Console.WriteLine("  [Lazy] HeavyService 正在创建...（只会执行一次）");
        Thread.Sleep(100); // 模拟耗时初始化
        return new HeavyService("DefaultService");
    });

    public static void RunBasicLazyDemo()
    {
        Console.WriteLine("=== 示例1：Lazy<T> 基本用法 ===\n");
        Console.WriteLine("  Lazy<T> 已声明，但 HeavyService 还没创建");
        Console.WriteLine($"  IsValueCreated = {_lazyService.IsValueCreated}");
        Console.WriteLine();

        // 多线程同时访问，只有一个线程会执行工厂函数
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(() =>
            {
                var result = _lazyService.Value.Process($"input-{i}");
                Console.WriteLine($"  线程 {i}：{result}");
            }))
            .ToArray();
        Task.WaitAll(tasks);

        Console.WriteLine($"\n  IsValueCreated = {_lazyService.IsValueCreated}（现在是 true 了）");
        Console.WriteLine($"  服务创建时间：{_lazyService.Value.CreatedAt:HH:mm:ss.fff}\n");
    }

    // ---- 示例2：三种 LazyThreadSafetyMode 对比 ----

    public static void RunThreadSafetyModeDemo()
    {
        Console.WriteLine("=== 示例2：LazyThreadSafetyMode 三种模式对比 ===\n");

        int createCount1 = 0, createCount2 = 0, createCount3 = 0;

        // 模式1：ExecutionAndPublication（默认，最安全）
        // 加了锁，保证工厂函数只执行一次
        var mode1 = new Lazy<string>(() =>
        {
            Interlocked.Increment(ref createCount1);
            Thread.Sleep(10);
            return "Mode1-Value";
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        // 模式2：PublicationOnly（乐观并发）
        // 允许多个线程都执行工厂函数，但只有一个线程的结果会被发布
        // 适合工厂函数没有副作用的场景，性能更好
        var mode2 = new Lazy<string>(() =>
        {
            Interlocked.Increment(ref createCount2);
            Thread.Sleep(10);
            return "Mode2-Value";
        }, LazyThreadSafetyMode.PublicationOnly);

        // 模式3：None（不线程安全，单线程用）
        var mode3 = new Lazy<string>(() =>
        {
            Interlocked.Increment(ref createCount3);
            return "Mode3-Value";
        }, LazyThreadSafetyMode.None);

        // 并发访问 mode1 和 mode2
        var tasks1 = Enumerable.Range(0, 10).Select(_ => Task.Run(() => { string _ = mode1.Value; })).ToArray();
        var tasks2 = Enumerable.Range(0, 10).Select(_ => Task.Run(() => { string _ = mode2.Value; })).ToArray();
        Task.WaitAll([.. tasks1, .. tasks2]);
        _ = mode3.Value; // 单线程访问

        Console.WriteLine($"  ExecutionAndPublication：工厂函数执行了 {createCount1} 次（期望：1）");
        Console.WriteLine($"  PublicationOnly：        工厂函数执行了 {createCount2} 次（可能 > 1，但值只发布一次）");
        Console.WriteLine($"  None：                   工厂函数执行了 {createCount3} 次（单线程，期望：1）");
        Console.WriteLine();
        Console.WriteLine("  💡 建议：大多数场景用默认模式（ExecutionAndPublication）");
        Console.WriteLine("           如果初始化开销极小且无副作用，可考虑 PublicationOnly");
        Console.WriteLine();
    }

    // ---- 示例3：LazyInitializer.EnsureInitialized —— 轻量级版本 ----
    // 相比 Lazy<T>，它不需要包装类，直接初始化字段
    // 内部使用 PublicationOnly 语义（允许多个线程竞争，CAS 保证只发布一个）

    private static HeavyService? _lazyField;

    public static void RunLazyInitializerDemo()
    {
        Console.WriteLine("=== 示例3：LazyInitializer.EnsureInitialized ===\n");

        int createCount = 0;

        // 多线程竞争初始化同一个字段
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(() =>
            {
                // EnsureInitialized 保证 _lazyField 只被赋值一次（CAS 竞争）
                var service = LazyInitializer.EnsureInitialized(ref _lazyField, () =>
                {
                    Interlocked.Increment(ref createCount);
                    Console.WriteLine($"  线程 {i}：正在执行初始化工厂函数...");
                    Thread.Sleep(20);
                    return new HeavyService($"Service-from-thread-{i}");
                });
                Console.WriteLine($"  线程 {i}：得到服务 = {service.Name}");
            }))
            .ToArray();
        Task.WaitAll(tasks);

        Console.WriteLine($"\n  工厂函数执行了 {createCount} 次（PublicationOnly：可能 > 1）");
        Console.WriteLine($"  最终服务名称：{_lazyField!.Name}（只有一个被发布）\n");
    }

    // ---- 示例4：Lazy<T> 在单例模式中的应用 ----

    public static void RunSingletonPatternDemo()
    {
        Console.WriteLine("=== 示例4：Lazy<T> 实现线程安全单例 ===\n");

        Console.WriteLine("  以前写单例要 double-check locking + volatile，现在用 Lazy<T> 一行搞定：");
        Console.WriteLine();
        Console.WriteLine("  // ✅ 现代写法：");
        Console.WriteLine("  public class MyService {");
        Console.WriteLine("      private static readonly Lazy<MyService> _instance =");
        Console.WriteLine("          new(() => new MyService());");
        Console.WriteLine("      public static MyService Instance => _instance.Value;");
        Console.WriteLine("  }");
        Console.WriteLine();
        Console.WriteLine("  // ❌ 老写法（容易出错）：");
        Console.WriteLine("  private static volatile MyService? _instance;");
        Console.WriteLine("  private static readonly object _lock = new();");
        Console.WriteLine("  public static MyService Instance {");
        Console.WriteLine("      get {");
        Console.WriteLine("          if (_instance == null) {");
        Console.WriteLine("              lock (_lock) {");
        Console.WriteLine("                  if (_instance == null)");
        Console.WriteLine("                      _instance = new MyService();");
        Console.WriteLine("              }");
        Console.WriteLine("          }");
        Console.WriteLine("          return _instance;");
        Console.WriteLine("      }");
        Console.WriteLine("  }");
        Console.WriteLine();

        // 验证 Lazy 单例
        var s1 = MySingleton.Instance;
        var s2 = MySingleton.Instance;
        Console.WriteLine($"  s1 == s2：{ReferenceEquals(s1, s2)}（✅ 真正的单例）\n");
    }

    private sealed class MySingleton
    {
        private static readonly Lazy<MySingleton> _instance = new(() => new MySingleton());
        public static MySingleton Instance => _instance.Value;
        private MySingleton() { }
    }

    public static void RunAll()
    {
        RunBasicLazyDemo();
        RunThreadSafetyModeDemo();
        RunLazyInitializerDemo();
        RunSingletonPatternDemo();
    }
}
