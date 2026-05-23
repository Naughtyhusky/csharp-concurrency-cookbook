namespace MemoryLeaks.Fixes;

/// <summary>
/// 修复方案 2：事件订阅的正确管理
/// </summary>
public static class EventFixDemo
{
    // 复用泄漏演示里的 Publisher 结构
    public class LongLivedPublisher
    {
        public event EventHandler<string>? DataReceived;
        public event Func<string, Task>? AsyncDataReceived;

        public void Publish(string data) => DataReceived?.Invoke(this, data);
        public async Task PublishAsync(string data)
        {
            if (AsyncDataReceived != null)
                await AsyncDataReceived.Invoke(data).ConfigureAwait(false);
        }
    }

    // ✅ 修复 1：实现 IDisposable，在 Dispose 时取消订阅
    public sealed class ProperSubscriber : IDisposable
    {
        public string Name { get; }
        private readonly byte[] _cache = new byte[512 * 1024]; // 512KB
        private readonly LongLivedPublisher _publisher;
        private bool _disposed;

        public ProperSubscriber(string name, LongLivedPublisher publisher)
        {
            Name = name;
            _publisher = publisher;
            // 订阅
            _publisher.DataReceived += HandleData;
            Console.WriteLine($"  [修复] {Name} 订阅成功");
        }

        private void HandleData(object? sender, string data)
        {
            if (_disposed) return;
            _ = _cache.Length;
            Console.WriteLine($"  [修复] {Name} 处理数据: {data}");
        }

        // ✅ Dispose 时取消订阅，发布者释放对我们的引用，GC 可以回收
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _publisher.DataReceived -= HandleData; // ← 关键：用 -= 取消订阅
            Console.WriteLine($"  [修复] {Name} 已取消订阅，可以被 GC 回收");
        }
    }

    // ✅ 修复 2：异步事件用 Func，通过局部变量持有 lambda，便于取消
    public static async Task FixAsyncLambdaSubscription(LongLivedPublisher publisher)
    {
        Console.WriteLine("\n  [修复] 用局部变量持有异步 lambda 引用，便于取消订阅...");

        var bigContext = new byte[512 * 1024]; // 512KB

        // ✅ 持有 lambda 引用，以便后续 -= 取消
        Func<string, Task> handler = async data =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            _ = bigContext.Length;
            Console.WriteLine($"  [修复] 异步处理: {data}");
        };

        publisher.AsyncDataReceived += handler;

        await publisher.PublishAsync("消息1").ConfigureAwait(false);
        await publisher.PublishAsync("消息2").ConfigureAwait(false);

        // ✅ 用完后取消订阅，bigContext 可以被 GC
        publisher.AsyncDataReceived -= handler;
        Console.WriteLine("  [修复] 取消订阅后，bigContext（512KB）可以被 GC 释放");
    }

    // ✅ 修复 3：使用 WeakReference 实现弱事件（高级场景）
    // 弱事件：发布者对订阅者持有弱引用，订阅者被 GC 后自动"消失"
    public sealed class WeakEventManager<TEventArgs>
    {
        private readonly List<WeakReference<Action<object?, TEventArgs>>> _handlers = new();

        public void Subscribe(Action<object?, TEventArgs> handler)
        {
            _handlers.Add(new WeakReference<Action<object?, TEventArgs>>(handler));
        }

        public void Publish(object? sender, TEventArgs args)
        {
            // 触发时清理已 GC 的弱引用
            _handlers.RemoveAll(wr => !wr.TryGetTarget(out _));

            foreach (var wr in _handlers.ToList())
            {
                if (wr.TryGetTarget(out var handler))
                    handler(sender, args);
            }
        }
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== 事件订阅泄漏修复方案 ===");

        var publisher = new LongLivedPublisher();

        Console.WriteLine("\n  [修复 1] IDisposable 模式取消订阅:");
        using (var sub1 = new ProperSubscriber("订阅者A", publisher))
        using (var sub2 = new ProperSubscriber("订阅者B", publisher))
        {
            publisher.Publish("广播消息");
            await Task.Delay(10).ConfigureAwait(false);
        } // ← using 结束，两个订阅者自动取消订阅

        publisher.Publish("取消订阅后的广播（没人收到）");

        Console.WriteLine("\n  [修复 2] 持有 lambda 引用以取消订阅:");
        await FixAsyncLambdaSubscription(publisher);
    }
}
