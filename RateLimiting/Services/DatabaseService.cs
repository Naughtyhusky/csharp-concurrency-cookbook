namespace RateLimiting.Services;

/// <summary>
/// 演示使用 SemaphoreSlim 限制数据库连接并发数
/// </summary>
public class DatabaseService(ILogger<DatabaseService> logger)
{
    private readonly SemaphoreSlim _connectionLimit = new SemaphoreSlim(10);
    private readonly ILogger<DatabaseService> _logger = logger;
    private int _activeConnections;

    public async Task<string> QueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"等待数据库连接槽位，当前活跃连接数: {_activeConnections}");

        // 获取连接槽位
        await _connectionLimit.WaitAsync(cancellationToken);

        try
        {
            Interlocked.Increment(ref _activeConnections);
            _logger.LogInformation($"获得数据库连接，当前活跃连接数: {_activeConnections}");

            // 模拟数据库查询
            await Task.Delay(1000, cancellationToken);

            return $"查询结果：{sql}";
        }
        finally
        {
            Interlocked.Decrement(ref _activeConnections);
            _connectionLimit.Release();
            _logger.LogInformation($"释放数据库连接，当前活跃连接数: {_activeConnections}");
        }
    }

    public async Task<List<string>> BatchQueryAsync(List<string> sqls, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"开始批量查询，总共 {sqls.Count} 条SQL");

        var tasks = sqls.Select(sql => QueryAsync(sql, cancellationToken)).ToList();
        var results = await Task.WhenAll(tasks);

        _logger.LogInformation("批量查询完成");

        return [.. results];
    }

    public int GetActiveConnections() => _activeConnections;
}
