namespace AsyncEnumerable;

/// <summary>
/// IAsyncEnumerable 扩展方法
/// 提供常用的 LINQ 风格操作
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// 过滤元素
    /// </summary>
    public static async IAsyncEnumerable<T> Where<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate)
    {
        await foreach (var item in source)
        {
            if (predicate(item))
                yield return item;
        }
    }

    /// <summary>
    /// 转换元素
    /// </summary>
    public static async IAsyncEnumerable<TResult> Select<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        await foreach (var item in source)
        {
            yield return selector(item);
        }
    }

    /// <summary>
    /// 限制数量
    /// </summary>
    public static async IAsyncEnumerable<T> Take<T>(
        this IAsyncEnumerable<T> source,
        int count)
    {
        int taken = 0;
        await foreach (var item in source)
        {
            if (taken >= count)
                break;

            yield return item;
            taken++;
        }
    }

    /// <summary>
    /// 跳过前 N 个元素
    /// </summary>
    public static async IAsyncEnumerable<T> Skip<T>(
        this IAsyncEnumerable<T> source,
        int count)
    {
        int skipped = 0;
        await foreach (var item in source)
        {
            if (skipped < count)
            {
                skipped++;
                continue;
            }

            yield return item;
        }
    }

    /// <summary>
    /// 批量处理：将流式数据分批返回
    /// </summary>
    /// <param name="source">数据源</param>
    /// <param name="batchSize">每批大小</param>
    public static async IAsyncEnumerable<List<T>> Batch<T>(
        this IAsyncEnumerable<T> source,
        int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "批量大小必须大于 0");

        var batch = new List<T>(batchSize);

        await foreach (var item in source)
        {
            batch.Add(item);

            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        // 返回最后不满一批的数据
        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// 转换为 List
    /// </summary>
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// 转换为数组
    /// </summary>
    public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = await source.ToListAsync();
        return [.. list];
    }

    /// <summary>
    /// 计数
    /// </summary>
    public static async Task<int> CountAsync<T>(this IAsyncEnumerable<T> source)
    {
        int count = 0;
        await foreach (var _ in source)
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// 获取第一个元素，如果没有则返回默认值
    /// </summary>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source)
    {
        await foreach (var item in source)
        {
            return item;
        }
        return default;
    }

    /// <summary>
    /// 对每个元素执行操作
    /// </summary>
    public static async Task ForEachAsync<T>(
        this IAsyncEnumerable<T> source,
        Action<T> action)
    {
        await foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// 对每个元素执行异步操作
    /// </summary>
    public static async Task ForEachAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task> asyncAction)
    {
        await foreach (var item in source)
        {
            await asyncAction(item);
        }
    }

    /// <summary>
    /// 去重
    /// </summary>
    public static async IAsyncEnumerable<T> Distinct<T>(this IAsyncEnumerable<T> source)
    {
        var seen = new HashSet<T>();

        await foreach (var item in source)
        {
            if (seen.Add(item))
                yield return item;
        }
    }

    /// <summary>
    /// 添加进度回调
    /// </summary>
    public static async IAsyncEnumerable<T> WithProgress<T>(
        this IAsyncEnumerable<T> source,
        Action<int> onProgress)
    {
        int count = 0;
        await foreach (var item in source)
        {
            count++;
            onProgress(count);
            yield return item;
        }
    }
}
