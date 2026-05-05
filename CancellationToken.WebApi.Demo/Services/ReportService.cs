using CancellationToken.WebApi.Demo.Models;

namespace CancellationToken.WebApi.Demo.Services;

/// <summary>
/// 报表服务接口
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 生成报表（CPU 密集型任务）
    /// </summary>
    Task<ReportData> GenerateReportAsync(
        ReportRequest request, 
        System.Threading.CancellationToken cancellationToken);
}

/// <summary>
/// 报表服务实现
/// 
/// 演示场景：
/// 1. 用户请求生成销售报表（可能需要分析海量数据）
/// 2. 报表生成是 CPU 密集型任务，可能需要几十秒
/// 3. 用户可以取消报表生成
/// 
/// 关键点：
/// - CPU 密集型任务中定期检查 CancellationToken
/// - 不要每次循环都检查（性能影响）
/// - 每隔一定迭代次数检查一次（如：每 1000 次）
/// - 提供进度报告（可选）
/// </summary>
public class ReportService(ILogger<ReportService> logger) : IReportService
{
    private readonly ILogger<ReportService> _logger = logger;

    public async Task<ReportData> GenerateReportAsync(
        ReportRequest request, 
        System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "开始生成报表：Type={Type}, StartDate={StartDate}, EndDate={EndDate}", 
            request.ReportType, 
            request.StartDate, 
            request.EndDate);

        var report = new ReportData
        {
            ReportType = request.ReportType,
            GeneratedAt = DateTime.UtcNow,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        try
        {
            // 🔥 关键点1：开始前检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 模拟 CPU 密集型计算
            _logger.LogInformation("开始处理数据...");

            // 场景1：使用 Task.Run 处理 CPU 密集型任务
            report.ProcessedRecords = await Task.Run(async () =>
            {
                return await ProcessDataAsync(request, cancellationToken);
            }, cancellationToken);

            // 🔥 关键点2：长时间操作后检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 生成报表数据
            report.Data = GenerateReportData(request, report.ProcessedRecords);

            _logger.LogInformation("报表生成完成，处理了 {Count} 条记录", report.ProcessedRecords);

            return report;
        }
        catch (OperationCanceledException)
        {
            // 🔥 关键点3：记录取消信息
            _logger.LogWarning("报表生成已取消，已处理 {Count} 条记录", report.ProcessedRecords);
            throw;
        }
    }

    /// <summary>
    /// 处理数据（CPU 密集型任务）
    /// </summary>
    private async Task<int> ProcessDataAsync(
        ReportRequest request, 
        System.Threading.CancellationToken cancellationToken)
    {
        // 模拟处理 10000 条记录
        const int totalRecords = 10000;
        int processedCount = 0;

        // 计算每次延迟时间（总延迟 / 批次数）
        int batchSize = 1000;  // 每批处理 1000 条
        int batchCount = totalRecords / batchSize;
        int delayPerBatch = request.SimulatedDelayMs / batchCount;

        for (int batch = 0; batch < batchCount; batch++)
        {
            // 🔥 关键点：每批数据处理前检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 模拟处理一批数据
            await Task.Delay(delayPerBatch, cancellationToken);

            processedCount += batchSize;

            // 输出进度
            _logger.LogInformation(
                "报表生成中... {Processed}/{Total} ({Percentage}%)", 
                processedCount, 
                totalRecords, 
                (processedCount * 100 / totalRecords));
        }

        return processedCount;
    }

    /// <summary>
    /// 生成报表数据
    /// </summary>
    private Dictionary<string, object> GenerateReportData(ReportRequest request, int recordCount)
    {
        return request.ReportType.ToLower() switch
        {
            "sales" => new Dictionary<string, object>
            {
                ["TotalSales"] = 1234567.89m,
                ["OrderCount"] = recordCount,
                ["AverageOrderValue"] = 123.45m,
                ["TopProducts"] = new[] { "Product A", "Product B", "Product C" }
            },
            "inventory" => new Dictionary<string, object>
            {
                ["TotalProducts"] = recordCount,
                ["LowStockItems"] = 42,
                ["OutOfStockItems"] = 15,
                ["TotalValue"] = 987654.32m
            },
            "customer" => new Dictionary<string, object>
            {
                ["TotalCustomers"] = recordCount,
                ["NewCustomers"] = 256,
                ["ActiveCustomers"] = 1500,
                ["CustomerRetentionRate"] = 85.5
            },
            _ => []
        };
    }
}
