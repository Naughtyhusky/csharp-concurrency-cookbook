namespace MemoryLeaks.Leaks;

/// <summary>
/// 泄漏场景 3：未完成的 Task 被静态集合持有，导致 Task 关联的闭包/上下文无法 GC
///
/// 常见模式：
///   - 用静态 List/Dictionary 跟踪"进行中的任务"，但从不清理已完成的
///   - Task 的 async 状态机（闭包）持有大量业务对象引用
///   - async 方法的局部变量在 await 点被状态机"装箱"保留
/// </summary>
public static class TaskLeakDemo
{
    // ❌ 反模式：静态集合持有 Task，永远不清理
    private static readonly List<Task> _runningTasks = new();

    public static async Task LeakByStaticTaskCollection(int count = 10)
    {
        Console.WriteLine($"\n  [泄漏] 向静态集合添加 {count} 个 Task，不清理...");

        for (int i = 0; i < count; i++)
        {
            // 每个 Task 的 async 状态机持有一个 1MB 的 buffer
            var task = ProcessWithBigBufferAsync(i);
            _runningTasks.Add(task); // ❌ 添加但从不移除完成的 Task
        }

        // 等所有任务完成
        await Task.WhenAll(_runningTasks).ConfigureAwait(false);

        Console.WriteLine($"  [泄漏] 所有 Task 已完成，但 _runningTasks 仍持有 {_runningTasks.Count} 个引用");
        Console.WriteLine("  [泄漏] 每个 Task 的状态机（含 1MB buffer）都无法被 GC");

        // 演示后清理
        _runningTasks.Clear();
    }

    private static async Task ProcessWithBigBufferAsync(int id)
    {
        // 模拟 async 状态机持有大对象
        // 在 await 点前后，局部变量都被状态机保留
        var buffer = new byte[1024 * 1024]; // 1MB，await 前声明，状态机会持有到方法结束
        await Task.Delay(50).ConfigureAwait(false);
        // await 结束后，buffer 仍在状态机里，直到方法 return
        _ = buffer.Length;
    }

    // ❌ 反模式 2：ContinueWith 回调持有外部对象，Task 链无限延伸
    public static async Task LeakByTaskChain()
    {
        Console.WriteLine("\n  [泄漏] 不断 ContinueWith 构建任务链，中间对象无法释放...");

        var bigObject = new byte[512 * 1024]; // 512KB

        // ❌ 每个 ContinueWith 的闭包都捕获了 bigObject
        // 只要整条链没执行完，bigObject 就不会被释放
        var task = Task.Delay(10)
            .ContinueWith(t =>
            {
                GC.KeepAlive(bigObject); // 闭包捕获 bigObject
                return Task.Delay(10);
            })
            .Unwrap()
            .ContinueWith(t =>
            {
                GC.KeepAlive(bigObject); // 再次捕获
                return Task.Delay(10);
            })
            .Unwrap();

        await task.ConfigureAwait(false);

        Console.WriteLine("  [泄漏演示] 任务链完成，但如果链很长且中间有 await，");
        Console.WriteLine("             所有中间状态机都会在链执行期间持有 bigObject");
    }

    // ❌ 反模式 3：async 方法中过早声明大对象，await 后才使用
    public static async Task LeakByEarlyAllocation()
    {
        Console.WriteLine("\n  [演示] async 状态机在 await 期间持有大对象...");

        // ❌ 在第一个 await 前就声明了大对象
        // await 期间（可能几秒甚至几分钟），这个 buffer 一直占着内存
        var eagerBuffer = new byte[1024 * 1024]; // 1MB，过早分配

        await Task.Delay(100).ConfigureAwait(false); // 模拟长时间等待

        // 正确做法：await 之后再分配，或者用 scope 限制生命周期
        _ = eagerBuffer.Length; // 直到这里才用，但已经持有了 100ms+

        Console.WriteLine("  [演示] 大对象在整个 await 等待期间都被状态机持有");
    }
}
