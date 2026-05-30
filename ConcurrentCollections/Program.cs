using ConcurrentCollections;

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║    第12章：并发集合与线程安全类型 - 示例代码          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

// Demo01：ConcurrentDictionary
await Demo01_ConcurrentDictionary.RunAsync();

// Demo02：ConcurrentQueue / ConcurrentStack / ConcurrentBag
await Demo02_QueueStackBag.RunAsync();

// Demo03：BlockingCollection（同步阻塞生产消费）
await Demo03_BlockingCollection.RunAsync();

// Demo04：Channel<T>（异步生产消费，现代首选）
await Demo04_Channel.RunAsync();

// Demo05：ImmutableCollections（不可变集合）
await Demo05_ImmutableCollections.RunAsync();

Console.WriteLine("所有示例运行完毕！");

