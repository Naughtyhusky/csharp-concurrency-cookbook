namespace RealWorldCase.Models;

/// <summary>
/// 文件处理结果
/// </summary>
public record ProcessResult(string FileName, string ProcessedData, long ElapsedMs);

/// <summary>
/// 演进演示请求
/// </summary>
public record EvolutionDemoRequest(
    int FileCount = 100,
    int FileContentLines = 1000,
    string? Version = null  // V0, V1, V2, V3, V4
);

/// <summary>
/// 演进演示结果
/// </summary>
public record EvolutionDemoResult(
    string Version,
    int FileCount,
    long ElapsedMs,
    int SuccessCount,
    int FailCount,
    long PeakMemoryMB,
    int ThreadPoolThreads,
    double ThroughputPerSecond
);
