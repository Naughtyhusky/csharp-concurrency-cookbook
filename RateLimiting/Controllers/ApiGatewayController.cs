using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RateLimiting.DistributedLimiters;
using StackExchange.Redis;

namespace RateLimiting.Controllers;

/// <summary>
/// 实战案例 3：API 网关限流
/// 模拟 API 网关按 API Key 限流，不同订阅等级不同限额
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApiGatewayController(ILogger<ApiGatewayController> logger, IConnectionMultiplexer? redis) : ControllerBase
{
    private readonly RedisTokenBucketRateLimiter? _redisLimiter = redis != null ? new RedisTokenBucketRateLimiter(redis, capacity: 100, refillRate: 10) : null;
    private readonly ILogger<ApiGatewayController> _logger = logger;

    // 模拟 API Key 数据库
    private static readonly Dictionary<string, ApiKeyInfo> _apiKeys = new()
    {
        { "free-key-123", new ApiKeyInfo("free-key-123", "Free", 100, 1) },        // 免费版：容量100，每秒补充1个
        { "pro-key-456", new ApiKeyInfo("pro-key-456", "Pro", 1000, 10) },         // 专业版：容量1000，每秒补充10个
        { "enterprise-key-789", new ApiKeyInfo("enterprise-key-789", "Enterprise", 10000, 100) }  // 企业版：容量10000，每秒补充100个
    };



    /// <summary>
    /// 受保护的 API 端点
    /// 需要提供 API Key，根据订阅等级限流
    /// </summary>
    [HttpGet("protected/data")]
    public async Task<IActionResult> GetProtectedData()
    {
        // 从请求头获取 API Key
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey))
        {
            return Unauthorized(new { error = "缺少 API Key" });
        }

        // 验证 API Key
        if (!_apiKeys.TryGetValue(apiKey, out var keyInfo))
        {
            return Unauthorized(new { error = "无效的 API Key" });
        }

        // 基于内存的限流（单机版）
        var limiter = new RateLimiters.TokenBucketRateLimiter(keyInfo.Capacity, keyInfo.RefillRate);

        if (!limiter.TryAcquire())
        {
            _logger.LogWarning($"API Key {apiKey} 限流触发");
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "请求过于频繁",
                subscription = keyInfo.Subscription,
                limit = $"容量 {keyInfo.Capacity}，每秒补充 {keyInfo.RefillRate} 个令牌",
                suggestion = "请升级订阅以获得更高的配额"
            });
        }

        // 返回数据
        return Ok(new
        {
            message = "数据获取成功",
            subscription = keyInfo.Subscription,
            data = new
            {
                timestamp = DateTime.UtcNow,
                values = Enumerable.Range(1, 10).Select(i => Random.Shared.Next(1, 100))
            }
        });
    }

    /// <summary>
    /// 分布式限流版本（需要 Redis）
    /// </summary>
    [HttpGet("protected/data-distributed")]
    public async Task<IActionResult> GetProtectedDataDistributed()
    {
        if (_redisLimiter == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Redis 未配置，无法使用分布式限流"
            });
        }

        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey) || !_apiKeys.ContainsKey(apiKey))
        {
            return Unauthorized(new { error = "无效的 API Key" });
        }

        // 使用 Redis 分布式限流
        var key = $"ratelimit:apikey:{apiKey}";
        if (!await _redisLimiter.TryAcquireAsync(key))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "请求过于频繁（分布式限流）"
            });
        }

        return Ok(new
        {
            message = "数据获取成功（分布式限流）",
            data = new { timestamp = DateTime.UtcNow }
        });
    }

    /// <summary>
    /// 查询 API Key 信息和限流配额
    /// </summary>
    [HttpGet("key-info")]
    public IActionResult GetKeyInfo()
    {
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || !_apiKeys.TryGetValue(apiKey, out var keyInfo))
        {
            return Unauthorized(new { error = "无效的 API Key" });
        }

        return Ok(new
        {
            apiKey = keyInfo.ApiKey,
            subscription = keyInfo.Subscription,
            quota = new
            {
                capacity = keyInfo.Capacity,
                refillRate = keyInfo.RefillRate,
                description = $"令牌桶容量 {keyInfo.Capacity}，每秒补充 {keyInfo.RefillRate} 个令牌"
            }
        });
    }

    /// <summary>
    /// 获取所有可用的 API Key（测试用）
    /// </summary>
    [HttpGet("test-keys")]
    [DisableRateLimiting]
    public IActionResult GetTestKeys()
    {
        return Ok(new
        {
            message = "测试用 API Keys",
            keys = _apiKeys.Values.Select(k => new
            {
                apiKey = k.ApiKey,
                subscription = k.Subscription,
                capacity = k.Capacity,
                refillRate = k.RefillRate
            })
        });
    }
}

public record ApiKeyInfo(string ApiKey, string Subscription, int Capacity, double RefillRate);
