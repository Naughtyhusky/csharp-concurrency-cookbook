namespace MemoryLeaks.Leaks;

/// <summary>
/// 泄漏场景 2：事件订阅忘记取消，导致订阅者被发布者"钉住"无法 GC
///
/// 根本原因：
///   事件本质是委托链表，发布者（publisher）持有所有订阅者（subscriber）的引用。
///   只要发布者活着，所有订阅者就无法被 GC，即使你认为订阅者"已经不用了"。
///
/// 异步场景下更危险：
///   - async lambda 订阅事件，闭包捕获大量上下文
///   - 每次创建新对象重新订阅，但从不取消旧的订阅
/// </summary>
public static class EventLeakDemo
{
    // 模拟一个长生命周期的事件发布者（比如 SignalR Hub、消息总线、全局配置）
    public class LongLivedPublisher
    {
        // 普通事件
        public event EventHandler<string>? DataReceived;
        // 异步事件（用 Func 模拟）
        public event Func<string, Task>? AsyncDataReceived;

        private int _subscriberCount;

        public void Subscribe() => _subscriberCount++;
        public void Unsubscribe() => _subscriberCount--;
        public int SubscriberCount => _subscriberCount;

        public void Publish(string data)
        {
            DataReceived?.Invoke(this, data);
        }

        public async Task PublishAsync(string data)
        {
            if (AsyncDataReceived != null)
                await AsyncDataReceived.Invoke(data).ConfigureAwait(false);
        }
    }

    // 模拟一个短生命周期的订阅者（比如请求处理器、页面 ViewModel）
    public class ShortLivedSubscriber
    {
        public string Name { get; }
        private readonly byte[] _cache = new byte[512 * 1024]; // 512KB 的"业务数据"

        public ShortLivedSubscriber(string name)
        {
            Name = name;
        }

        public void HandleData(object? sender, string data)
        {
            // 使用 _cache 防止被优化掉
            _ = _cache.Length;
        }

        public async Task HandleDataAsync(string data)
        {
            await Task.Delay(1).ConfigureAwait(false);
            _ = _cache.Length;
        }
    }

    // ❌ 反模式：不断创建订阅者并订阅，但从不取消订阅
    public static async Task LeakByForgettingUnsubscribe(LongLivedPublisher publisher)
    {
        Console.WriteLine("\n  [泄漏] 创建多个订阅者，只订阅不取消...");

        for (int i = 0; i < 5; i++)
        {
            var subscriber = new ShortLivedSubscriber($"Subscriber-{i}");

            // ❌ 订阅后 subscriber 离开作用域，但发布者还持有它的引用
            // subscriber 的 512KB _cache 永远无法被 GC
            publisher.DataReceived += subscriber.HandleData;
            publisher.Subscribe();

            await Task.Delay(1).ConfigureAwait(false);
        }

        Console.WriteLine($"  [泄漏] publisher 仍持有 {publisher.SubscriberCount} 个订阅者引用");
        Console.WriteLine("  [泄漏] 每个订阅者携带 512KB，共约 2.5MB 无法释放");

        // 触发事件，验证订阅者还"活着"
        publisher.Publish("心跳包");
    }

    // ❌ 反模式 2：异步 lambda 订阅，闭包捕获更多上下文
    public static async Task LeakByAsyncLambdaSubscription(LongLivedPublisher publisher)
    {
        Console.WriteLine("\n  [泄漏] 异步 lambda 订阅，闭包捕获大对象...");

        var bigContext = new byte[1024 * 1024]; // 1MB 上下文

        // ❌ async lambda 形成闭包，捕获了 bigContext
        // 只要 publisher 存在，这个 1MB 就无法释放
        publisher.AsyncDataReceived += async data =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            _ = bigContext.Length; // 闭包持有 bigContext
        };

        await publisher.PublishAsync("异步消息");

        Console.WriteLine("  [泄漏] bigContext（1MB）被 async lambda 闭包持有，随发布者生存");
    }
}
