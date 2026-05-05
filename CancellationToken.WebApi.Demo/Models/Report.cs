namespace CancellationToken.WebApi.Demo.Models;

/// <summary>
/// 报表请求
/// </summary>
public class ReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ReportType { get; set; } = "Sales"; // Sales, Inventory, Customer

    /// <summary>
    /// 模拟报表生成延迟（毫秒）
    /// 用于演示：设置较大的值可以测试 CPU 密集型任务取消
    /// </summary>
    public int SimulatedDelayMs { get; set; } = 10000;
}

/// <summary>
/// 报表数据
/// </summary>
public class ReportData
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, object> Data { get; set; } = [];
    public int ProcessedRecords { get; set; }
}
