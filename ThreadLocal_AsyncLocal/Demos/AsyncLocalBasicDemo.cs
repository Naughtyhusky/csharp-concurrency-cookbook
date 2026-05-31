namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 05：AsyncLocal&lt;T&gt; 基础 —— 异步调用链中的上下文传播
    ///
    /// AsyncLocal&lt;T&gt; 是为"异步世界"设计的线程本地存储。
    /// 它的值会沿着"异步调用链"向下传播——你在父 Task 里设置的值，子 Task 能看到。
    /// 但父 Task 看不到子 Task 对它的修改（写时复制隔离）。
    ///
    /// 底层依赖 ExecutionContext，每次 await 都会流转 ExecutionContext 快照。
    /// </summary>
    public static class AsyncLocalBasicDemo
    {
        private static readonly AsyncLocal<string?> _asyncContext = new();

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Demo 05：AsyncLocal<T> 基础 —— 异步调用链传播 ===\n");

            _asyncContext.Value = "Root";
            Console.WriteLine($"[根] 设置 AsyncLocal = '{_asyncContext.Value}'");

            await Level1Async();

            // 根层：子任务的修改不影响我
            Console.WriteLine($"[根] await 返回后，AsyncLocal = '{_asyncContext.Value}'（仍然是 Root，未被子修改）\n");
        }

        private static async Task Level1Async()
        {
            Console.WriteLine($"  [Level1] 进入时 AsyncLocal = '{_asyncContext.Value}'（继承自根）");

            // 修改值：这个修改只影响 Level1 及其以下，不会影响上层（根）
            _asyncContext.Value = "Level1";
            Console.WriteLine($"  [Level1] 修改为 '{_asyncContext.Value}'");

            await Level2Async();

            Console.WriteLine($"  [Level1] await 返回后，AsyncLocal = '{_asyncContext.Value}'（仍是 Level1）");
        }

        private static async Task Level2Async()
        {
            Console.WriteLine($"    [Level2] 进入时 AsyncLocal = '{_asyncContext.Value}'（继承自 Level1）");

            // 模拟真实异步操作（切换线程）
            await Task.Delay(1);

            // await 之后，AsyncLocal 的值仍然保持！这就是 ExecutionContext 流转的魔法
            Console.WriteLine($"    [Level2] await Task.Delay 后，AsyncLocal = '{_asyncContext.Value}'（值没丢！）");

            _asyncContext.Value = "Level2";
            Console.WriteLine($"    [Level2] 修改为 '{_asyncContext.Value}'（只影响本层及以下）");
        }
    }
}
