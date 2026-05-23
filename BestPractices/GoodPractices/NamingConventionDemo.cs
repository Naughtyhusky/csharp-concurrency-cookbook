namespace BestPractices.GoodPractices;

/// <summary>
/// 最佳实践：异步方法命名规范（Async 后缀）
/// 以及方法签名的正确设计
/// </summary>
public class NamingConventionDemo
{
    // ❌ 不规范：异步方法没有 Async 后缀
    public static async Task<string> GetUser(int id)
    {
        await Task.Delay(50);
        return $"用户_{id}";
    }

    // ❌ 不规范：同步方法加了 Async 后缀（误导调用方）
    public static string GetUserAsync_Wrong(int id)
    {
        return $"用户_{id}"; // 这不是异步方法，加后缀是误导！
    }

    // ✅ 规范：异步方法统一加 Async 后缀
    public static async Task<string> GetUserAsync(int id)
    {
        await Task.Delay(50);
        return $"用户_{id}";
    }

    // ✅ 规范：接口方法也要有 Async 后缀
    public interface IUserRepository
    {
        Task<string> GetUserAsync(int id);       // ✅ 有后缀
        Task<List<string>> GetAllUsersAsync();   // ✅ 有后缀
        Task SaveUserAsync(string user);         // ✅ 有后缀
    }

    // ✅ 规范：异步方法应该接受 CancellationToken（可选但推荐）
    public static async Task<string> GetUserWithCancellationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return $"用户_{id}";
    }

    // ✅ 特殊情况：某些接口方法历史上没有 Async 后缀，比如 IAsyncEnumerable
    // GetAsyncEnumerator() 没有 Async 后缀，这是约定俗成的例外

    public static async Task Demo()
    {
        Console.WriteLine("\n=== 命名规范演示 ===");

        Console.WriteLine("✅ 正确命名的异步方法：");
        var user = await GetUserAsync(1);
        Console.WriteLine($"  获取用户：{user}");

        Console.WriteLine("\n✅ 带 CancellationToken 的标准签名：");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var user2 = await GetUserWithCancellationAsync(2, cts.Token);
        Console.WriteLine($"  获取用户：{user2}");

        Console.WriteLine("\n📌 命名规范总结：");
        Console.WriteLine("  1. 返回 Task/Task<T>/ValueTask 的方法，加 Async 后缀");
        Console.WriteLine("  2. 同步方法不要加 Async 后缀（别骗人！）");
        Console.WriteLine("  3. 公共异步 API 最好提供 CancellationToken 参数");
        Console.WriteLine("  4. CancellationToken 参数放在最后，给默认值 default");
    }
}
