using Microsoft.AspNetCore.Mvc;
using RateLimiting.RateLimiters;

namespace RateLimiting.Controllers;

/// <summary>
/// 演示自定义限流算法的使用
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomRateLimitController(ILogger<CustomRateLimitController> logger) : ControllerBase
{
    private static readonly FixedWindowRateLimiter _fixedWindowLimiter = new(limit: 5, window: TimeSpan.FromSeconds(10));
    private static readonly SlidingWindowRateLimiter _slidingWindowLimiter = new(limit: 5, window: TimeSpan.FromSeconds(10));
    private static readonly TokenBucketRateLimiter _tokenBucketLimiter = new(capacity: 10, refillRate: 1);
    private static readonly LeakyBucketRateLimiter _leakyBucketLimiter = new(capacity: 10, leakInterval: TimeSpan.FromSeconds(1));

    private readonly ILogger<CustomRateLimitController> _logger = logger;

    /// <summary>
    /// 测试固定窗口限流器
    /// 每10秒最多5次请求
    /// </summary>
    [HttpGet("fixed-window")]
    public IActionResult FixedWindow()
    {
        if (!_fixedWindowLimiter.TryAcquire())
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "请求过于频繁",
                resetIn = _fixedWindowLimiter.GetTimeUntilReset().TotalSeconds,
                currentCount = _fixedWindowLimiter.GetCurrentCount()
            });
        }

        return Ok(new
        {
            message = "固定窗口限流：每10秒最多5次请求",
            currentCount = _fixedWindowLimiter.GetCurrentCount(),
            resetIn = _fixedWindowLimiter.GetTimeUntilReset().TotalSeconds
        });
    }

    /// <summary>
    /// 测试滑动窗口限流器
    /// 每10秒最多5次请求（更精确）
    /// </summary>
    [HttpGet("sliding-window")]
    public IActionResult SlidingWindow()
    {
        if (!_slidingWindowLimiter.TryAcquire())
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "请求过于频繁",
                currentCount = _slidingWindowLimiter.GetCurrentCount(),
                oldestTimestamp = _slidingWindowLimiter.GetOldestTimestamp()
            });
        }

        return Ok(new
        {
            message = "滑动窗口限流：每10秒最多5次请求",
            currentCount = _slidingWindowLimiter.GetCurrentCount()
        });
    }

    /// <summary>
    /// 测试令牌桶限流器
    /// 容量10，每秒补充1个令牌
    /// </summary>
    [HttpGet("token-bucket")]
    public IActionResult TokenBucket()
    {
        if (!_tokenBucketLimiter.TryAcquire())
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "请求过于频繁",
                availableTokens = _tokenBucketLimiter.GetAvailableTokens(),
                nextTokenIn = _tokenBucketLimiter.GetTimeUntilNextToken().TotalSeconds
            });
        }

        return Ok(new
        {
            message = "令牌桶限流：容量10，每秒补充1个令牌",
            availableTokens = _tokenBucketLimiter.GetAvailableTokens()
        });
    }

    /// <summary>
    /// 测试漏桶限流器
    /// 容量10，每秒漏出1个请求
    /// </summary>
    [HttpGet("leaky-bucket")]
    public IActionResult LeakyBucket()
    {
        if (!_leakyBucketLimiter.TryAcquire())
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "请求过于频繁",
                currentCount = _leakyBucketLimiter.GetCurrentCount(),
                availableSlots = _leakyBucketLimiter.GetAvailableSlots()
            });
        }

        return Ok(new
        {
            message = "漏桶限流：容量10，每秒漏出1个请求",
            currentCount = _leakyBucketLimiter.GetCurrentCount(),
            availableSlots = _leakyBucketLimiter.GetAvailableSlots()
        });
    }

    /// <summary>
    /// 查看所有限流器的状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            fixedWindow = new
            {
                currentCount = _fixedWindowLimiter.GetCurrentCount(),
                resetIn = _fixedWindowLimiter.GetTimeUntilReset().TotalSeconds
            },
            slidingWindow = new
            {
                currentCount = _slidingWindowLimiter.GetCurrentCount(),
                oldestTimestamp = _slidingWindowLimiter.GetOldestTimestamp()
            },
            tokenBucket = new
            {
                availableTokens = _tokenBucketLimiter.GetAvailableTokens(),
                nextTokenIn = _tokenBucketLimiter.GetTimeUntilNextToken().TotalSeconds
            },
            leakyBucket = new
            {
                currentCount = _leakyBucketLimiter.GetCurrentCount(),
                availableSlots = _leakyBucketLimiter.GetAvailableSlots()
            }
        });
    }
}
