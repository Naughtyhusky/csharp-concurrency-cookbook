using System.Collections.Immutable;

namespace ConcurrentCollections;

/// <summary>
/// 示例05：ImmutableCollections - 不可变集合
/// </summary>
public static class Demo05_ImmutableCollections
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Demo05：ImmutableCollections（不可变集合） ===\n");

        BasicImmutableDemo();
        BuilderPatternDemo();
        await ThreadSafeSnapshotDemo();
    }

    // ---------------------------------------------------------------
    // 1. 基础用法：所有"修改"都返回新对象，原对象不变
    // ---------------------------------------------------------------
    static void BasicImmutableDemo()
    {
        Console.WriteLine("--- 1. 基础用法：修改返回新对象 ---");

        // ImmutableList
        var list = ImmutableList.Create(1, 2, 3);
        var list2 = list.Add(4);
        var list3 = list2.Remove(2);

        Console.WriteLine($"原始 list:  [{string.Join(", ", list)}]");
        Console.WriteLine($"Add(4):     [{string.Join(", ", list2)}]");
        Console.WriteLine($"Remove(2):  [{string.Join(", ", list3)}]");
        Console.WriteLine($"原始 list 未变: [{string.Join(", ", list)}]");
        Console.WriteLine();

        // ImmutableDictionary
        var dict = ImmutableDictionary<string, int>.Empty
            .Add("one", 1)
            .Add("two", 2)
            .Add("three", 3);

        var dict2 = dict.SetItem("two", 22); // 修改某个键
        var dict3 = dict2.Remove("one");

        Console.WriteLine($"原始 dict[\"two\"] = {dict["two"]}");
        Console.WriteLine($"SetItem(\"two\",22) 后 dict2[\"two\"] = {dict2["two"]}");
        Console.WriteLine($"Remove(\"one\") 后 dict3 包含 \"one\": {dict3.ContainsKey("one")}");
        Console.WriteLine($"原始 dict 不变，dict[\"two\"] = {dict["two"]}");
        Console.WriteLine();

        // ImmutableArray（结构体，零额外开销，随机访问 O(1)）
        var arr = ImmutableArray.Create(10, 20, 30, 40, 50);
        var arr2 = arr.SetItem(2, 999);
        Console.WriteLine($"ImmutableArray 原始: [{string.Join(", ", arr)}]");
        Console.WriteLine($"SetItem(2, 999):     [{string.Join(", ", arr2)}]");
        Console.WriteLine();
    }

    // ---------------------------------------------------------------
    // 2. Builder 模式：批量构建，避免频繁创建中间对象
    // ---------------------------------------------------------------
    static void BuilderPatternDemo()
    {
        Console.WriteLine("--- 2. Builder 模式（批量修改时使用） ---");

        // ❌ 低效写法：每次 Add 都创建一个新的 ImmutableList
        var slowList = ImmutableList<int>.Empty;
        for (int i = 0; i < 10; i++)
            slowList = slowList.Add(i); // 10次 Add = 创建 10 个中间对象

        // ✅ 高效写法：用 Builder 批量操作，最后一次性生成不可变对象
        var builder = ImmutableList.CreateBuilder<int>();
        for (int i = 0; i < 10; i++)
            builder.Add(i); // 在可变的 Builder 上操作，无额外对象
        var fastList = builder.ToImmutable(); // 只创建一个最终对象

        Console.WriteLine($"Builder 结果: [{string.Join(", ", fastList)}]");
        Console.WriteLine();
    }

    // ---------------------------------------------------------------
    // 3. 实战：线程安全的快照读 + 原子更新
    //    多线程读取快照，主线程不断更新，读线程永远看到一致的完整快照
    // ---------------------------------------------------------------
    static async Task ThreadSafeSnapshotDemo()
    {
        Console.WriteLine("--- 3. 实战：多线程快照读（无锁） ---");

        // 用字段引用（局部变量不支持 volatile，改用辅助类包裹）
        ImmutableList<string> sharedList = ImmutableList<string>.Empty;

        int readCount = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        // 写线程：每 20ms 追加一条记录（原子替换引用）
        var writer = Task.Run(async () =>
        {
            int seq = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                var current = sharedList;
                var updated = current.Add($"record-{seq++:D3}");
                // Interlocked.CompareExchange 原子替换引用（无锁更新）
                Interlocked.CompareExchange(ref sharedList, updated, current);
                await Task.Delay(20, cts.Token).ConfigureAwait(false);
            }
        });

        // 5 个读线程：随时读取，拿到的永远是某个完整快照（不会读到中间状态）
        var readers = Enumerable.Range(0, 5).Select(id => Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Volatile.Read 保证读到最新已发布的引用，得到一个完整不可变快照
                var snapshot = Volatile.Read(ref sharedList);
                // snapshot 里的数据永远是一致的，不会有"读到一半"的情况
                _ = snapshot.Count;
                Interlocked.Increment(ref readCount);
                await Task.Delay(5, cts.Token).ConfigureAwait(false);
            }
        })).ToArray();

        try { await Task.WhenAll([writer, .. readers]); }
        catch (OperationCanceledException) { }

        Console.WriteLine($"最终快照大小: {sharedList.Count} 条记录");
        Console.WriteLine($"读线程总计读取次数: {readCount}");
        Console.WriteLine($"全程无锁，无一次死锁或数据竞争 ✅\n");
    }
}
