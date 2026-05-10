namespace ExceptionHandling.Extensions
{
    /// <summary>
    /// 安全的 WhenAll 扩展方法
    /// 返回成功和失败的结果，不会丢失任何异常信息
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// 安全地等待所有任务完成，返回成功和失败的结果
        /// </summary>
        public static async Task<(List<T> Successes, List<Exception> Failures)> SafeWhenAll<T>(
            this IEnumerable<Task<T>> tasks)
        {
            var taskList = tasks.ToList();
            var successes = new List<T>();
            var failures = new List<Exception>();

            foreach (var task in taskList)
            {
                try
                {
                    var result = await task;
                    successes.Add(result);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            return (successes, failures);
        }

        /// <summary>
        /// 安全地等待所有任务完成（无返回值版本）
        /// </summary>
        public static async Task<List<Exception>> SafeWhenAll(this IEnumerable<Task> tasks)
        {
            var taskList = tasks.ToList();
            var failures = new List<Exception>();

            foreach (var task in taskList)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            return failures;
        }

        /// <summary>
        /// 安全的 Fire-and-Forget
        /// </summary>
        public static async void SafeFireAndForget(
            this Task task,
            Action<Exception>? onException = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                // 调用自定义异常处理器
                onException?.Invoke(ex);

                // 如果没有提供处理器，记录到控制台
                if (onException == null)
                {
                    Console.WriteLine($"❌ 后台任务异常: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}
