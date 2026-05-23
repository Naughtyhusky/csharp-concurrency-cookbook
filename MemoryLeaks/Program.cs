using MemoryLeaks.Fixes;
using MemoryLeaks.Leaks;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║       第09章：异步编程中的内存泄漏                        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

// ──────────────────────────────────────────────
// 场景 1：CancellationTokenSource 泄漏
// ──────────────────────────────────────────────
Console.WriteLine("\n\n【场景1：CancellationTokenSource 未释放】");
await CtsLeakDemo.LeakInLoop_NoDispose(iterations: 5);
await CtsLeakDemo.LeakViaCallbackRegistration();

Console.WriteLine("\n--- 对应修复 ---");
await CtsFixDemo.Demo();

// ──────────────────────────────────────────────
// 场景 2：事件订阅泄漏
// ──────────────────────────────────────────────
Console.WriteLine("\n\n【场景2：事件订阅忘记取消】");
var publisher = new EventLeakDemo.LongLivedPublisher();
await EventLeakDemo.LeakByForgettingUnsubscribe(publisher);
await EventLeakDemo.LeakByAsyncLambdaSubscription(publisher);

Console.WriteLine("\n--- 对应修复 ---");
await EventFixDemo.Demo();

// ──────────────────────────────────────────────
// 场景 3：Task 与 async 状态机泄漏
// ──────────────────────────────────────────────
Console.WriteLine("\n\n【场景3：Task 被静态集合持有 & 状态机大对象】");
await TaskLeakDemo.LeakByStaticTaskCollection(count: 5);
await TaskLeakDemo.LeakByEarlyAllocation();

Console.WriteLine("\n--- 对应修复 ---");
await TaskFixDemo.Demo();

// ──────────────────────────────────────────────
// 场景 4：Timer / PeriodicTimer 泄漏
// ──────────────────────────────────────────────
Console.WriteLine("\n\n【场景4：Timer 未释放】");
await TimerLeakDemo.LeakByOrphanedTimer();
await Task.Delay(800); // 等孤立 Timer 演示完

using var cts4 = new CancellationTokenSource(TimeSpan.FromSeconds(2));
await TimerLeakDemo.LeakByPeriodicTimerNotStopped(cts4.Token);

await TimerLeakDemo.LeakByTimersTimerEvent();
await Task.Delay(900); // 等 System.Timers.Timer 演示完

Console.WriteLine("\n--- 对应修复 ---");
await TimerFixDemo.Demo();

// ──────────────────────────────────────────────
// 场景 5：Channel 使用不当
// ──────────────────────────────────────────────
Console.WriteLine("\n\n【场景5：Channel Writer 未 Complete & 无界堆积】");
await ChannelLeakDemo.LeakByForgettingComplete();
await ChannelLeakDemo.LeakByExceptionNotPropagated();
await ChannelLeakDemo.LeakByUnboundedChannelOverflow();

Console.WriteLine("\n--- 对应修复 ---");
await ChannelFixDemo.Demo();

// ──────────────────────────────────────────────
Console.WriteLine("\n\n╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    演示完成！                             ║");
Console.WriteLine("║  内存泄漏不一定会立刻崩溃，但会让你的服务越跑越慢！       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

