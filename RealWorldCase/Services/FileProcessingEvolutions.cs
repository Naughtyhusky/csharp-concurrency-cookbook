using System.Collections.Concurrent;
using System.Threading.Channels;
using RealWorldCase.Models;

namespace RealWorldCase.Services;

/// <summary>
/// 文件处理系统的五步演进演示（V0 → V4）
/// 模拟场景：读取文件 → CPU处理 → 写入数据库
/// </summary>
public class FileProcessingEvolutions
{
    private readonly ILogger<FileProcessingEvolutions> _logger;
    private readonly string _tempDir;

    public FileProcessingEvolutions(ILogger<FileProcessingEvolutions> logger)
    {
        _logger = logger;
        _tempDir = Path.Combine(Path.GetTempPath(), $"EvolutionDemo_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// 准备测试文件
    /// </summary>
    private List<string> PrepareFiles(int fileCount, int linesPerFile)
    {
        var files = new List<string>(fileCount);
        for (int i = 0; i < fileCount; i++)
        {
            var path = Path.Combine(_tempDir, $"data_{i:D5}.csv");
            var lines = Enumerable.Range(0, linesPerFile)
                .Select(j => $"Line{j},Value{Random.Shared.Next(1000)},{Random.Shared.NextDouble():F4}")
                .Prepend("Index,Value,Score");
            File.WriteAllLines(path, lines);
            files.Add(path);
        }
        _logger.LogInformation("准备了 {Count} 个测试文件，每个 {Lines} 行", fileCount, linesPerFile);
        return files;
    }

    /// <summary>
    /// 清理测试文件
    /// </summary>
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    // ============================================================
    // 模拟 CPU 密集型处理
    // ============================================================
    private static string HeavyCpuProcess(string content)
    {
        // 模拟 CPU 密集型操作：解析、排序、聚合
        var lines = content.Split('\n');
        double sum = 0;
        foreach (var line in lines.Skip(1))  // 跳过 header
        {
            var parts = line.Split(',');
            if (parts.Length >= 3 && double.TryParse(parts[2], out var value))
            {
                sum += value;
            }
        }
        // 模拟更多计算
        for (int i = 0; i < 100; i++)
        {
            sum = Math.Sqrt(Math.Abs(sum) + i) * Math.Log(Math.Abs(sum) + 2);
        }
        return $"Processed: {lines.Length} lines, Score: {sum:F2}";
    }

    // ============================================================
    // 模拟数据库操作
    // ============================================================
    private static readonly ConcurrentDictionary<string, string> SimulatedDatabase = new();

    private static void SaveToDatabase(string data)
    {
        // 模拟同步数据库写入（阻塞线程）
        Thread.Sleep(20);
        SimulatedDatabase[Guid.NewGuid().ToString("N")] = data;
    }

    private static async Task SaveToDatabaseAsync(string data, CancellationToken ct = default)
    {
        // 模拟异步数据库写入
        await Task.Delay(20, ct);
        SimulatedDatabase[Guid.NewGuid().ToString("N")] = data;
    }

    // ============================================================
    // V0: 原始 Thread 版本 ⚠️ 仅供演示，生产环境不要用
    // ============================================================
    public (int Success, int Fail, long ElapsedMs) ProcessV0_Thread(int fileCount, int linesPerFile)
    {
        var files = PrepareFiles(fileCount, linesPerFile);
        var sw = Stopwatch.StartNew();
        int success = 0, fail = 0;
        var doneEvent = new CountdownEvent(files.Count);

        foreach (var file in files)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var processed = HeavyCpuProcess(content);
                    SaveToDatabase(processed);
                    Interlocked.Increment(ref success);
                }
                catch
                {
                    Interlocked.Increment(ref fail);
                }
                finally
                {
                    doneEvent.Signal();
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        doneEvent.Wait(TimeSpan.FromSeconds(120));
        sw.Stop();

        var result = (success, fail, sw.ElapsedMilliseconds);
        _logger.LogInformation(
            "V0 Thread: {Count} 文件, 成功 {Success} 失败 {Fail}, 耗时 {Elapsed}ms",
            fileCount, success, fail, sw.ElapsedMilliseconds);
        return result;
    }

    // ============================================================
    // V1: Task.Run（线程池化）
    // ============================================================
    public (int Success, int Fail, long ElapsedMs) ProcessV1_Task(int fileCount, int linesPerFile)
    {
        var files = PrepareFiles(fileCount, linesPerFile);
        var sw = Stopwatch.StartNew();
        int success = 0, fail = 0;

        var tasks = files.Select(file => Task.Run(() =>
        {
            try
            {
                var content = File.ReadAllText(file);
                var processed = HeavyCpuProcess(content);
                SaveToDatabase(processed);
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref fail);
            }
        })).ToArray();

        Task.WaitAll(tasks);
        sw.Stop();

        var result = (success, fail, sw.ElapsedMilliseconds);
        _logger.LogInformation(
            "V1 Task: {Count} 文件, 成功 {Success} 失败 {Fail}, 耗时 {Elapsed}ms",
            fileCount, success, fail, sw.ElapsedMilliseconds);
        return result;
    }

    // ============================================================
    // V2: async/await（异步 I/O）
    // ============================================================
    public async Task<(int Success, int Fail, long ElapsedMs)> ProcessV2_Async(
        int fileCount, int linesPerFile)
    {
        var files = PrepareFiles(fileCount, linesPerFile);
        var sw = Stopwatch.StartNew();
        int success = 0, fail = 0;

        var tasks = files.Select(async file =>
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var processed = HeavyCpuProcess(content);
                await SaveToDatabaseAsync(processed);
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref fail);
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        var result = (success, fail, sw.ElapsedMilliseconds);
        _logger.LogInformation(
            "V2 async/await: {Count} 文件, 成功 {Success} 失败 {Fail}, 耗时 {Elapsed}ms",
            fileCount, success, fail, sw.ElapsedMilliseconds);
        return result;
    }

    // ============================================================
    // V3: I/O 异步 + CPU 并行
    // ============================================================
    public async Task<(int Success, int Fail, long ElapsedMs)> ProcessV3_Parallel(
        int fileCount, int linesPerFile)
    {
        var files = PrepareFiles(fileCount, linesPerFile);
        var sw = Stopwatch.StartNew();

        // 阶段一：并发异步读取所有文件
        var readTasks = files.Select(async file =>
        {
            var content = await File.ReadAllTextAsync(file);
            return (file, content);
        });
        var fileContents = await Task.WhenAll(readTasks);

        // 阶段二：并行 CPU 处理
        var results = new ConcurrentBag<ProcessResult>();
        Parallel.ForEach(fileContents, item =>
        {
            var processed = HeavyCpuProcess(item.content);
            results.Add(new ProcessResult(item.file, processed, 0));
        });

        // 阶段三：并发异步写入数据库
        int success = 0, fail = 0;
        var saveTasks = results.Select(async r =>
        {
            try
            {
                await SaveToDatabaseAsync(r.ProcessedData);
                Interlocked.Increment(ref success);
            }
            catch
            {
                Interlocked.Increment(ref fail);
            }
        });
        await Task.WhenAll(saveTasks);

        sw.Stop();
        var result = (success, fail, sw.ElapsedMilliseconds);
        _logger.LogInformation(
            "V3 Parallel: {Count} 文件, 成功 {Success} 失败 {Fail}, 耗时 {Elapsed}ms",
            fileCount, success, fail, sw.ElapsedMilliseconds);
        return result;
    }

    // ============================================================
    // V4: 生产级 — 三阶段流水线（读→CPU→DB 完全解耦）
    // ============================================================
    public async Task<(int Success, int Fail, long ElapsedMs)> ProcessV4_Production(
        int fileCount, int linesPerFile, CancellationToken cancellationToken = default)
    {
        var files = PrepareFiles(fileCount, linesPerFile);
        var sw = Stopwatch.StartNew();
        int success = 0, fail = 0;

        using var dbSemaphore = new SemaphoreSlim(20);

        var channel = Channel.CreateBounded<(string File, string Content)>(
            new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

        // 收集所有 DB 写入 task
        var dbWriteTasks = new List<Task>(files.Count);

        // ── 阶段 1：CPU worker（只做 CPU，DB 写入 fire-and-forget）──
        var cpuWorkerCount = Math.Clamp(Environment.ProcessorCount / 2, 2, 4);
        var cpuWorkers = new Task[cpuWorkerCount];
        for (int i = 0; i < cpuWorkerCount; i++)
        {
            cpuWorkers[i] = Task.Run(async () =>
            {
                await foreach (var (file, content) in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        var processed = HeavyCpuProcess(content);

                        // DB 写入作为独立 task → 不阻塞 CPU worker
                        var t = Task.Run(async () =>
                        {
                            await dbSemaphore.WaitAsync(cancellationToken);
                            try
                            {
                                await SaveToDatabaseAsync(processed, cancellationToken);
                                Interlocked.Increment(ref success);
                            }
                            finally { dbSemaphore.Release(); }
                        }, cancellationToken);

                        lock (dbWriteTasks) { dbWriteTasks.Add(t); }
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Increment(ref fail);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref fail);
                        _logger.LogError(ex, "CPU 处理失败: {File}", file);
                    }
                }
            }, cancellationToken);
        }

        // ── 阶段 2：生产者高并发读文件 → Channel ──
        var producerTasks = files.Select(async file =>
        {
            using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, readCts.Token);
            try
            {
                var content = await File.ReadAllTextAsync(file, linked.Token);
                await channel.Writer.WriteAsync((file, content), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref fail);
                _logger.LogWarning("读取超时或取消: {File}", file);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref fail);
                _logger.LogError(ex, "读取失败: {File}", file);
            }
        }).ToList();

        // ── 阶段 3：生产者完成 → 关 Channel → 等 CPU worker → 等 DB ──
        await Task.WhenAll(producerTasks);
        channel.Writer.Complete();
        await Task.WhenAll(cpuWorkers);
        await Task.WhenAll(dbWriteTasks);
        sw.Stop();

        var result = (success, fail, sw.ElapsedMilliseconds);
        _logger.LogInformation(
            "V4 Production: {Count} 文件, 成功 {Success} 失败 {Fail}, 耗时 {Elapsed}ms, Workers={Workers}",
            fileCount, success, fail, sw.ElapsedMilliseconds, cpuWorkerCount);
        return result;
    }

    /// <summary>
    /// 运行指定版本的演进演示
    /// </summary>
    public async Task<EvolutionDemoResult> RunEvolutionDemo(
        int fileCount, int linesPerFile, string? version = null)
    {
        ThreadPool.GetMaxThreads(out int maxWorker, out _);
        ThreadPool.GetMinThreads(out int minWorker, out _);

        long elapsedMs;
        int success, fail;

        switch (version?.ToUpper())
        {
            case "V0":
                (success, fail, elapsedMs) = ProcessV0_Thread(fileCount, linesPerFile);
                break;
            case "V1":
                (success, fail, elapsedMs) = ProcessV1_Task(fileCount, linesPerFile);
                break;
            case "V2":
                (success, fail, elapsedMs) = await ProcessV2_Async(fileCount, linesPerFile);
                break;
            case "V3":
                (success, fail, elapsedMs) = await ProcessV3_Parallel(fileCount, linesPerFile);
                break;
            case "V4":
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120)))
                {
                    (success, fail, elapsedMs) = await ProcessV4_Production(
                        fileCount, linesPerFile, cts.Token);
                }
                break;
            default:
                // 默认运行所有版本
                _logger.LogInformation("运行所有版本演进的对比测试...");

                // V0
                var v0 = ProcessV0_Thread(fileCount, linesPerFile);
                // V1
                var v1 = ProcessV1_Task(fileCount, linesPerFile);
                // V2
                var v2 = await ProcessV2_Async(fileCount, linesPerFile);
                // V3
                var v3 = await ProcessV3_Parallel(fileCount, linesPerFile);
                // V4
                using (var ctsAll = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    var v4 = await ProcessV4_Production(fileCount, linesPerFile, ctsAll.Token);

                    _logger.LogInformation(
                        "📊 完整对比: V0={V0}ms V1={V1}ms V2={V2}ms V3={V3}ms V4={V4}ms",
                        v0.ElapsedMs, v1.ElapsedMs, v2.ElapsedMs, v3.ElapsedMs, v4.ElapsedMs);

                    return new EvolutionDemoResult(
                        "ALL", fileCount, v4.ElapsedMs,
                        v4.Success, v4.Fail, GC.GetTotalMemory(false) / 1024 / 1024,
                        0, 0);
                }
        }

        return new EvolutionDemoResult(
            version ?? "V0", fileCount, elapsedMs,
            success, fail, GC.GetTotalMemory(false) / 1024 / 1024,
            0, 0);
    }
}
