namespace BestPractices.GoodPractices;

/// <summary>
/// 最佳实践：在同步上下文中调用异步代码
/// 
/// 有时候你真的需要在同步代码中调用异步方法（比如构造函数、老代码接口）
/// 这里展示几种相对安全的做法（但还是尽量避免！）
/// </summary>
public class SyncCallingAsyncDemo
{
    // 场景：你有一个老的同步接口，但实现必须用异步
    public interface ILegacySyncService
    {
        string GetData(int id); // 老接口，无法改成异步
    }

    // ❌ 危险做法 1：直接 .Result（在有 SynchronizationContext 的地方会死锁）
    public class BadSyncWrapper : ILegacySyncService
    {
        public string GetData(int id)
        {
            return FetchDataAsync(id).Result; // 💀 死锁风险
        }
    }

    // ✅ 相对安全做法 1：使用 GetAwaiter().GetResult() + 确保异步代码全程 ConfigureAwait(false)
    // 注意：这依然会阻塞线程，只是降低了死锁风险
    public class BetterSyncWrapper : ILegacySyncService
    {
        public string GetData(int id)
        {
            // GetAwaiter().GetResult() 不会将异常包装成 AggregateException
            // 但仍然会阻塞线程！
            return FetchDataAsync(id).GetAwaiter().GetResult();
        }
    }

    // ✅ 相对安全做法 2：Task.Run(...).GetAwaiter().GetResult()
    // 把异步工作扔到线程池，避免在当前 SynchronizationContext 中等待
    public class SaferSyncWrapper : ILegacySyncService
    {
        public string GetData(int id)
        {
            // 在没有 SynchronizationContext 的线程池线程中运行，减少死锁风险
            // 但仍然会阻塞当前线程！
            return Task.Run(() => FetchDataAsync(id)).GetAwaiter().GetResult();
        }
    }

    // ✅ 最佳做法：设计时避免同步包装异步
    // 1. 改造老接口，提供异步版本
    // 2. 使用工厂方法替代异步构造函数

    // 异步构造函数的正确解决方案：工厂方法
    public class MyService
    {
        private readonly string _config;

        private MyService(string config)
        {
            _config = config;
        }

        // ✅ 用静态工厂方法替代异步构造函数
        public static async Task<MyService> CreateAsync(CancellationToken ct = default)
        {
            var config = await LoadConfigAsync(ct);
            return new MyService(config);
        }

        private static async Task<string> LoadConfigAsync(CancellationToken ct)
        {
            await Task.Delay(50, ct).ConfigureAwait(false);
            return "配置加载完毕";
        }

        public string GetConfig() => _config;
    }

    private static async Task<string> FetchDataAsync(int id)
    {
        // 注意：这里加了 ConfigureAwait(false)，降低死锁风险
        await Task.Delay(50).ConfigureAwait(false);
        return $"数据_{id}";
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== 同步上下文调用异步代码演示 ===");

        Console.WriteLine("\n⚠️ 不得不同步调用时（控制台环境，无死锁风险）：");

        var safer = new SaferSyncWrapper();
        var result = safer.GetData(42);
        Console.WriteLine($"  SaferSyncWrapper 结果: {result}");

        Console.WriteLine("\n✅ 异步工厂方法（推荐的异步初始化模式）：");
        var service = await MyService.CreateAsync();
        Console.WriteLine($"  服务配置: {service.GetConfig()}");

        Console.WriteLine("\n📌 原则：");
        Console.WriteLine("  1. 尽量一路 async all the way，不要打断异步链");
        Console.WriteLine("  2. 确实要同步调用时，确保异步代码全程 ConfigureAwait(false)");
        Console.WriteLine("  3. 用 Task.Run() 把异步代码移到线程池（降低但不消除死锁风险）");
        Console.WriteLine("  4. 用异步工厂方法替代异步构造函数");
    }
}
