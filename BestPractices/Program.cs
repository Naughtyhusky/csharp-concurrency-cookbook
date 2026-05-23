using BestPractices.AntiPatterns;
using BestPractices.GoodPractices;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║       第08章：异步编程最佳实践与反模式                    ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

// ────────────────────────────────────────────
// 反模式演示
// ────────────────────────────────────────────
Console.WriteLine("\n【反模式篇】");

// 1. async void 的危害
await AsyncVoidDemo.DemoAsyncVoidProblem();

// 2. 异步转同步（死锁风险）
await AsyncOverSyncDemo.DemoDeadlockRisk();

// 3. Fire-and-Forget 与 Task.Run 滥用
await FireAndForgetDemo.Demo();

// 4. async using 资源泄漏
await AsyncDisposableDemo.Demo();

// ────────────────────────────────────────────
// 最佳实践演示
// ────────────────────────────────────────────
Console.WriteLine("\n\n【最佳实践篇】");

// 5. 命名规范
await NamingConventionDemo.Demo();

// 6. ConfigureAwait 正确使用
await ConfigureAwaitDemo.Demo();

// 7. ValueTask 正确使用
await ValueTaskDemo.Demo();

// 8. 同步上下文调用异步代码
await SyncCallingAsyncDemo.Demo();

Console.WriteLine("\n\n╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    演示完成！                             ║");
Console.WriteLine("║  记住：async/await 不是银弹，用对了才是生产力！           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

