using ThreadLocal_AsyncLocal.Demos;

namespace ThreadLocal_AsyncLocal
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  第13章：ThreadLocal 与 AsyncLocal");
            Console.WriteLine("  线程本地存储 · 异步上下文传播");
            Console.WriteLine("========================================\n");

            // Demo 01：ThreadStatic 特性的基础用法与陷阱
            ThreadStaticDemo.Run();

            // Demo 02：ThreadLocal<T> 的基础用法
            ThreadLocalBasicDemo.Run();

            // Demo 03：ThreadLocal<T> 追踪所有线程的值
            ThreadLocalTrackingDemo.Run();

            // Demo 04：ThreadLocal<T> 在线程池中的"复用陷阱"
            ThreadLocalThreadPoolDemo.Run();

            // Demo 05：AsyncLocal<T> 基础 —— 异步调用链中的上下文传播
            await AsyncLocalBasicDemo.RunAsync();

            // Demo 06：AsyncLocal<T> 的"写时隔离"特性
            await AsyncLocalIsolationDemo.RunAsync();

            // Demo 07：AsyncLocal<T> 实战 —— 模拟请求追踪 TraceId
            await AsyncLocalTraceIdDemo.RunAsync();

            // Demo 08：ThreadLocal<T> 实战 —— 线程安全的 Random
            ThreadLocalRandomDemo.Run();

            // Demo 09：ThreadLocal vs AsyncLocal 场景对比
            await ComparisonDemo.RunAsync();

            Console.WriteLine("\n========================================");
            Console.WriteLine("  所有 Demo 运行完毕！");
            Console.WriteLine("========================================");
        }
    }
}

