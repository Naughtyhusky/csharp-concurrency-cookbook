using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsyncEnumerable;

/// <summary>
/// GitHub Issue 获取器：演示真实的 API 分页场景
/// </summary>
public class GitHubIssueFetcher
{
    private readonly HttpClient _httpClient;

    public GitHubIssueFetcher(string? token = null)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/"),
            DefaultRequestHeaders =
            {
                { "User-Agent", "AsyncEnumerable-Demo" }
            }
        };

        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// 流式获取所有 Issue
    /// </summary>
    /// <param name="owner">仓库所有者</param>
    /// <param name="repo">仓库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async IAsyncEnumerable<GitHubIssue> GetAllIssuesAsync(
        string owner,
        string repo,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int page = 1;
        int totalLoaded = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<GitHubIssue> issues;
            try
            {
                issues = await FetchPageAsync(owner, repo, page, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ 获取第 {page} 页失败: {ex.Message}");
                break; // 失败后停止
            }

            if (issues.Count == 0)
                break; // 没有更多数据

            foreach (var issue in issues)
            {
                yield return issue;
                totalLoaded++;
            }

            Console.WriteLine($"✅ 已加载第 {page} 页，共 {totalLoaded} 条 Issue");
            page++;

            // GitHub API 限流：避免请求过快
            await Task.Delay(100, cancellationToken);
        }

        Console.WriteLine($"🎉 加载完成，共 {totalLoaded} 条 Issue");
    }

    /// <summary>
    /// 流式获取所有 Issue（带预加载优化）
    /// </summary>
    public async IAsyncEnumerable<GitHubIssue> GetAllIssuesWithPrefetchAsync(
        string owner,
        string repo,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int page = 1;
        Task<List<GitHubIssue>>? nextPageTask = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 如果没有预加载任务，启动加载当前页
            var currentPageTask = nextPageTask ?? FetchPageAsync(owner, repo, page, cancellationToken);

            // 同时启动加载下一页（预加载）
            nextPageTask = FetchPageAsync(owner, repo, page + 1, cancellationToken);

            var issues = await currentPageTask;
            if (issues.Count == 0)
                break;

            foreach (var issue in issues)
                yield return issue;

            page++;
            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>
    /// 获取单页 Issue
    /// </summary>
    private async Task<List<GitHubIssue>> FetchPageAsync(
        string owner,
        string repo,
        int page,
        CancellationToken cancellationToken)
    {
        var url = $"repos/{owner}/{repo}/issues?page={page}&per_page=100&state=all";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<GitHubIssue>>(json) ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  获取第 {page} 页失败: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// 统计标签分布
    /// </summary>
    public async Task<Dictionary<string, int>> GetLabelStatisticsAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default)
    {
        var labelCount = new Dictionary<string, int>();

        await foreach (var issue in GetAllIssuesAsync(owner, repo, cancellationToken))
        {
            foreach (var label in issue.Labels)
            {
                labelCount[label.Name] = labelCount.GetValueOrDefault(label.Name) + 1;
            }
        }

        return labelCount;
    }

    /// <summary>
    /// 查找包含关键词的 Issue
    /// </summary>
    public async IAsyncEnumerable<GitHubIssue> SearchIssuesAsync(
        string owner,
        string repo,
        string keyword,
        int maxResults = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int found = 0;

        await foreach (var issue in GetAllIssuesAsync(owner, repo, cancellationToken))
        {
            if (issue.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (issue.Body?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                yield return issue;

                if (++found >= maxResults)
                    break; // 找到足够的结果就停止
            }
        }
    }
}

/// <summary>
/// GitHub Issue 数据模型
/// </summary>
public class GitHubIssue
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("labels")]
    public List<GitHubLabel> Labels { get; set; } = new();

    [JsonPropertyName("user")]
    public GitHubUser? User { get; set; }
}

/// <summary>
/// GitHub 标签数据模型
/// </summary>
public class GitHubLabel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// GitHub 用户数据模型
/// </summary>
public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }
}
