using System.Collections.Concurrent;

namespace SyncContext
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  SynchronizationContext 与死锁演示 - Console 版本            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // 测试 1：.Result 不会死锁（Console 没有 SynchronizationContext）
            Console.WriteLine("【测试 1】使用 .Result（Console 不会死锁）");
            Console.WriteLine("─────────────────────────────────────────────");
            TestResultInConsole();
            Console.WriteLine();

            // 测试 2：async Main
            Console.WriteLine("【测试 2】async Main（推荐做法）");
            Console.WriteLine("─────────────────────────────────────────────");
            TestAsyncMain().Wait();
            Console.WriteLine();

            // 测试 3：SynchronizationContext 检查
            Console.WriteLine("【测试 3】检查 SynchronizationContext");
            Console.WriteLine("─────────────────────────────────────────────");
            TestSynchronizationContext().Wait();
            Console.WriteLine();

            // 测试 4：对比 ConfigureAwait
            Console.WriteLine("【测试 4】ConfigureAwait 对比");
            Console.WriteLine("─────────────────────────────────────────────");
            TestConfigureAwait().Wait();
            Console.WriteLine();

            // 测试 5：模拟 WinForms 的死锁
            Console.WriteLine("【测试 5】模拟 WinForms 的死锁（可选）");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("提示：要模拟死锁，需要创建一个单线程 SynchronizationContext");
            Console.WriteLine("按任意键继续...");
            Console.ReadKey();
            // SimulateWinFormsDeadlock(); // 取消注释会死锁

            Console.WriteLine("\n所有测试完成！按任意键退出...");
            Console.ReadKey();
        }

        // 测试 1：在 Console 中使用 .Result
        static void TestResultInConsole()
        {
            Console.WriteLine($"  当前线程 ID: {Environment.CurrentManagedThreadId}");
            Console.WriteLine($"  当前 SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
            Console.WriteLine($"  调用 DownloadDataAsync().Result...");

            try
            {
                // ✅ Console 中不会死锁，因为没有 SynchronizationContext
                string result = DownloadDataAsync().Result;
                Console.WriteLine($"\n  结果:\n{result}");
                Console.WriteLine("  ✅ 没有死锁！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ 错误: {ex.Message}");
            }
        }

        // 测试 2：async Main
        static async Task TestAsyncMain()
        {
            Console.WriteLine($"  当前线程 ID: {Environment.CurrentManagedThreadId}");
            Console.WriteLine($"  当前 SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
            Console.WriteLine($"  使用 await...");

            string result = await DownloadDataAsync();
            Console.WriteLine($"\n  结果:\n{result}");
            Console.WriteLine("  ✅ 这是推荐的异步写法！");
        }

        // 测试 3：检查 SynchronizationContext
        static async Task TestSynchronizationContext()
        {
            Console.WriteLine("  1. 主线程:");
            Console.WriteLine($"     - 线程 ID: {Environment.CurrentManagedThreadId}");
            Console.WriteLine($"     - SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");

            await Task.Run(() =>
            {
                Console.WriteLine("  2. Task.Run 内部:");
                Console.WriteLine($"     - 线程 ID: {Environment.CurrentManagedThreadId}");
                Console.WriteLine($"     - SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
            });

            Console.WriteLine("  3. await 之后:");
            Console.WriteLine($"     - 线程 ID: {Environment.CurrentManagedThreadId}");
            Console.WriteLine($"     - SynchronizationContext: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
            Console.WriteLine("  📝 观察：线程 ID 可能改变，但 SynchronizationContext 始终是 null");
        }

        // 测试 4：ConfigureAwait 对比
        static async Task TestConfigureAwait()
        {
            Console.WriteLine("  4.1 默认 await:");
            int thread1Before = Environment.CurrentManagedThreadId;
            await Task.Delay(500);
            int thread1After = Environment.CurrentManagedThreadId;
            Console.WriteLine($"      - 之前线程 ID: {thread1Before}");
            Console.WriteLine($"      - 之后线程 ID: {thread1After}");

            Console.WriteLine("  4.2 ConfigureAwait(false):");
            int thread2Before = Environment.CurrentManagedThreadId;
            await Task.Delay(500).ConfigureAwait(false);
            int thread2After = Environment.CurrentManagedThreadId;
            Console.WriteLine($"      - 之前线程 ID: {thread2Before}");
            Console.WriteLine($"      - 之后线程 ID: {thread2After}");

            Console.WriteLine("  📝 在 Console 中，ConfigureAwait(false) 没有实际区别");
            Console.WriteLine("      因为没有 SynchronizationContext 需要捕获");
        }

        // 测试 5：模拟 WinForms 的死锁
        static void SimulateWinFormsDeadlock()
        {
            Console.WriteLine("⚠️ 警告：这个测试会导致死锁！");
            Console.WriteLine("创建一个单线程 SynchronizationContext...");

            var context = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);

            var thread = new Thread(() =>
            {
                context.RunOnCurrentThread();
            });
            thread.Start();

            // 等待 SynchronizationContext 启动
            Thread.Sleep(100);

            context.Post(_ =>
            {
                Console.WriteLine($"  在 SynchronizationContext 线程中");
                Console.WriteLine($"  线程 ID: {Environment.CurrentManagedThreadId}");
                Console.WriteLine($"  调用 .Result...");

                // 💣 这里会死锁！
                string result = DownloadDataAsync().Result;
                Console.WriteLine(result);

                context.Complete();
            }, null);

            thread.Join();
            Console.WriteLine("完成（如果你看到这行，说明没有死锁）");
        }

        // 模拟异步下载
        static async Task<string> DownloadDataAsync()
        {
            int beforeAwait = Environment.CurrentManagedThreadId;
            var contextBefore = SynchronizationContext.Current?.GetType().Name ?? "null";

            // 模拟网络请求
            await Task.Delay(1000);

            int afterAwait = Environment.CurrentManagedThreadId;
            var contextAfter = SynchronizationContext.Current?.GetType().Name ?? "null";

            return $"      ✅ 下载完成！\n" +
                   $"      await 之前: 线程 {beforeAwait}, Context: {contextBefore}\n" +
                   $"      await 之后: 线程 {afterAwait}, Context: {contextAfter}";
        }

        // 自定义单线程 SynchronizationContext（用于模拟 WinForms）
        class SingleThreadSynchronizationContext : SynchronizationContext
        {
            private readonly BlockingCollection<(SendOrPostCallback callback, object? state)> _queue 
                = new BlockingCollection<(SendOrPostCallback, object?)>();

            public override void Post(SendOrPostCallback d, object? state)
            {
                _queue.Add((d, state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                var tcs = new TaskCompletionSource<object?>();
                _queue.Add((s =>
                {
                    try
                    {
                        d(s);
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, state));
                tcs.Task.Wait();
            }

            public void RunOnCurrentThread()
            {
                SynchronizationContext.SetSynchronizationContext(this);

                foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                {
                    callback(state);
                }
            }

            public void Complete()
            {
                _queue.CompleteAdding();
            }
        }
    }
}
