namespace MemoryLeaks.Fixes;

/// <summary>
/// 修复方案 3：Task 集合和 async 状态机的内存优化
/// </summary>
public static class TaskFixDemo
{
    // ✅ 修复 1：静态任务集合 → 完成后及时移除，或用 ConcurrentDictionary 按 ID 管理
    public static async Task FixStaticTaskCollection(int count = 10)
    {
        Console.WriteLine($"\n  [修复] 用 ConcurrentDictionary 跟踪任务，完成后自动移除...");

        var runningTasks = new System.Collections.Concurrent.ConcurrentDictionary<int, Task>();

        var allTasks = new List<Task>();
        for (int i = 0; i < count; i++)
        {
            var taskId = i;
            var task = RunAndCleanupAsync(taskId, runningTasks);
            runningTasks.TryAdd(taskId, task);
            allTasks.Add(task);
        }

        await Task.WhenAll(allTasks).ConfigureAwait(false);
        Console.WriteLine($"  [修复] 所有任务完成，字典剩余条目: {runningTasks.Count}（应为 0）");
    }

    private static async Task RunAndCleanupAsync(
        int id,
        System.Collections.Concurrent.ConcurrentDictionary<int, Task> dict)
    {
        try
        {
            await Task.Delay(20).ConfigureAwait(false);
        }
        finally
        {
            // ✅ 任务完成（无论成功还是失败）都从字典中移除自身
            dict.TryRemove(id, out _);
        }
    }

    // ✅ 修复 2：在 await 后再分配大对象（减少状态机持有时间）
    public static async Task FixEarlyAllocation()
    {
        Console.WriteLine("\n  [修复] await 完成后再分配大对象，减少持有时间...");

        // ✅ 先完成所有 I/O 等待
        await Task.Delay(100).ConfigureAwait(false);

        // ✅ await 结束后再分配，大对象生命周期尽可能短
        var buffer = new byte[1024 * 1024];
        ProcessBuffer(buffer);
        // buffer 在方法结束时自动可被 GC

        Console.WriteLine("  [修复] 大对象在 await 完成后才分配，持有时间最短");
    }

    // ✅ 修复 3：用局部 scope 限制大对象的生命周期
    public static async Task FixWithLocalScope()
    {
        Console.WriteLine("\n  [修复] 用代码块限制大对象作用域...");

        await Task.Delay(50).ConfigureAwait(false); // 第一个 await

        string result;
        {
            // ✅ 大对象只在这个块里存在
            var bigBuffer = new byte[1024 * 1024];
            result = ProcessBufferToString(bigBuffer);
            // bigBuffer 离开块后，下一次 GC 即可回收
        }
        // bigBuffer 在这里已超出作用域

        await Task.Delay(50).ConfigureAwait(false); // 第二个 await，bigBuffer 已经可以被 GC

        Console.WriteLine($"  [修复] 处理结果: {result}，大对象已超出作用域");
    }

    private static void ProcessBuffer(byte[] buffer) => _ = buffer.Length;

    private static string ProcessBufferToString(byte[] buffer) => $"processed {buffer.Length} bytes";

    public static async Task Demo()
    {
        Console.WriteLine("\n=== Task 与状态机内存优化 ===");
        await FixStaticTaskCollection();
        await FixEarlyAllocation();
        await FixWithLocalScope();
    }
}
