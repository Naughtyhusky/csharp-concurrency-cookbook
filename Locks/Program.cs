using Locks.AdvancedLocks;
using Locks.AsyncLock;
using Locks.Comparison;
using Locks.Internals;
using Locks.LockBasics;
using Locks.Pitfalls;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║      第11章：锁机制完全指南：从 lock 到异步锁        ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine();

// ── Part 0：底层原理 ──
LockInternalsDemo.Demo();
Console.WriteLine();

// ── Part 1：lock / Monitor ──
LockAndMonitorDemo.Demo();
Console.WriteLine();

// ── Part 2：Interlocked 无锁原子操作 ──
InterlockedDemo.Demo();
Console.WriteLine();

// ── Part 3：SpinLock ──
SpinLockDemo.Demo();
Console.WriteLine();

// ── Part 3：ReaderWriterLockSlim ──
ReaderWriterLockSlimDemo.Demo();
Console.WriteLine();

// ── Part 4：SemaphoreSlim ──
await SemaphoreSlimDemo.Demo();
Console.WriteLine();

// ── Part 5：Mutex ──
MutexDemo.Demo();
Console.WriteLine();

// ── Part 6：AsyncLock ──
await AsyncLockDemo.Demo();
Console.WriteLine();

// ── Part 7：常见陷阱 ──
await LockPitfallsDemo.Demo();
Console.WriteLine();

// ── Part 8：性能对比 ──
await LockPerformanceComparison.Demo();
Console.WriteLine();

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║  全部演示完成！记住：选锁原则 → 够用就行，能不加就不加  ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");

