namespace BackgroundService.Services;

/// <summary>
/// 后台任务队列接口
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// 将任务加入队列
    /// </summary>
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);

    /// <summary>
    /// 从队列中取出任务
    /// </summary>
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
