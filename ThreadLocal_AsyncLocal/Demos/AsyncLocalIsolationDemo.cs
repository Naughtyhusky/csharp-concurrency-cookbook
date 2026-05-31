namespace ThreadLocal_AsyncLocal.Demos
{
    /// <summary>
    /// Demo 06：AsyncLocal&lt;T&gt; 的"写时隔离"特性
    ///
    /// AsyncLocal 的核心机制是"写时复制（Copy-on-Write）"：
    /// - 读取时，子流直接看父流的值（共享，高效）
    /// - 写入时，只影响当前流及其下游，不影响上游和兄弟流
    ///
    /// 这个特性让 AsyncLocal 非常适合"请求级别"的上下文，每个请求分支互不干扰。
    /// </summary>
    public static class AsyncLocalIsolationDemo
    {
        private static readonly AsyncLocal<string?> _userId = new();

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Demo 06：AsyncLocal<T> 的写时隔离特性 ===\n");

            _userId.Value = "用户A";
            Console.WriteLine($"[主流] 设置 UserId = '{_userId.Value}'");

            // 同时启动两个"子请求"，它们各自修改 _userId 互不干扰
            var task1 = ProcessRequestAsync("分支1", "用户B");
            var task2 = ProcessRequestAsync("分支2", "用户C");
            await Task.WhenAll(task1, task2);

            // 主流的值依然是"用户A"，两个分支的修改都不影响它
            Console.WriteLine($"\n[主流] 所有分支完成后，UserId = '{_userId.Value}'（仍是用户A，隔离成功！）");

            Console.WriteLine("\n💡 AsyncLocal 的写时隔离就像'平行宇宙'，每个分支都有自己的副本，互不干扰。\n");
        }

        private static async Task ProcessRequestAsync(string branchName, string userId)
        {
            // 每个分支设置自己的 userId，这是在"写时复制"语义下进行的
            _userId.Value = userId;
            Console.WriteLine($"  [{branchName}] 开始处理，UserId = '{_userId.Value}'");

            await Task.Delay(10);  // 模拟 I/O

            // await 后值仍然保持（ExecutionContext 流转）
            Console.WriteLine($"  [{branchName}] 处理完成，UserId = '{_userId.Value}'（await后未丢失）");
        }
    }
}
