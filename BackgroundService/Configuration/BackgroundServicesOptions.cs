namespace BackgroundService.Configuration;

/// <summary>
/// 后台服务配置
/// </summary>
public class BackgroundServicesOptions
{
    public const string SectionName = "BackgroundServices";

    public TimedTaskOptions TimedTask { get; set; } = new();
    public DataSyncOptions DataSync { get; set; } = new();
    public QueueOptions Queue { get; set; } = new();
    public EmailSenderOptions EmailSender { get; set; } = new();
}

public class TimedTaskOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 5;
}

public class DataSyncOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 1;
}

public class QueueOptions
{
    public int Capacity { get; set; } = 100;
    public int MaxConcurrency { get; set; } = 10;
}

public class EmailSenderOptions
{
    public int MaxRetries { get; set; } = 3;
    public int[] RetryDelayMs { get; set; } = new[] { 1000, 5000, 15000 };
}
