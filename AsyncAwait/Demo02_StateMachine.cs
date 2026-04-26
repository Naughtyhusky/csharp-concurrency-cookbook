namespace AsyncAwait;

/// <summary>
/// Demo02: 状态机原理演示
/// </summary>
public static class Demo02_StateMachine
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo02: 状态机原理演示 ===\n");

        // 2.1 简单的状态机示例
        Console.WriteLine("--- 2.1 观察状态转换 ---");
        await SimpleStateMachineExample();

        // 2.2 同步完成 vs 异步完成
        Console.WriteLine("\n--- 2.2 同步路径 vs 异步路径 ---");
        await CompareSyncAndAsyncPaths();

        // 2.3 手动模拟状态机
        Console.WriteLine("\n--- 2.3 手动模拟状态机 ---");
        await ManualStateMachine();
    }

    // 简单的异步方法
    static async Task SimpleStateMachineExample()
    {
        Console.WriteLine($"✓ [状态 0] 方法开始（线程 {Environment.CurrentManagedThreadId}）");

        await Task.Delay(500);

        Console.WriteLine($"✓ [状态 1] await 完成后（线程 {Environment.CurrentManagedThreadId}）");
    }

    // 对比同步和异步路径
    static async Task CompareSyncAndAsyncPaths()
    {
        // 测试 1：同步完成（IsCompleted = true）
        Console.WriteLine("✓ 测试 1: 已完成的任务（同步路径）");
        var completedTask = Task.FromResult(42);
        Console.WriteLine($"  IsCompleted: {completedTask.IsCompleted}");

        int result1 = await completedTask;
        Console.WriteLine($"  结果: {result1}（无状态转换，直接继续）");

        // 测试 2：异步完成（IsCompleted = false）
        Console.WriteLine("\n✓ 测试 2: 未完成的任务（异步路径）");
        var incompleteTask = Task.Run(async () =>
        {
            await Task.Delay(500);
            return 100;
        });
        Console.WriteLine($"  IsCompleted: {incompleteTask.IsCompleted}");

        int result2 = await incompleteTask;
        Console.WriteLine($"  结果: {result2}（经过状态转换）");
    }

    // 手动模拟简化的状态机
    static Task ManualStateMachine()
    {
        var stateMachine = new SimpleStateMachine();
        stateMachine.Start();
        return stateMachine.Task;
    }

    // 简化的状态机实现（仅用于演示）
    class SimpleStateMachine
    {
        private int _state = 0;
        private TaskCompletionSource<bool> _tcs = new();

        public Task Task => _tcs.Task;

        public void Start()
        {
            MoveNext();
        }

        private void MoveNext()
        {
            try
            {
                switch (_state)
                {
                    case 0: // 初始状态
                        Console.WriteLine($"  ✓ [手动状态机] 状态 0: 开始（线程 {Environment.CurrentManagedThreadId}）");

                        // 模拟 await Task.Delay(500)
                        var awaiter = Task.Delay(500).GetAwaiter();

                        if (awaiter.IsCompleted)
                        {
                            Console.WriteLine("  ✓ [手动状态机] 同步完成，直接跳到状态 1");
                            goto case 1;
                        }
                        else
                        {
                            Console.WriteLine("  ✓ [手动状态机] 异步完成，注册回调");
                            _state = 1;
                            awaiter.OnCompleted(MoveNext); // 注册回调
                            return; // 暂停执行
                        }

                    case 1: // await 完成后
                        Console.WriteLine($"  ✓ [手动状态机] 状态 1: 完成（线程 {Environment.CurrentManagedThreadId}）");
                        _tcs.SetResult(true); // 完成 Task
                        return;
                }
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }
    }
}
