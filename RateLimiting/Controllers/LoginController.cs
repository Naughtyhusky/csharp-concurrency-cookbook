using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimiting.Controllers;

/// <summary>
/// 实战案例 1：保护登录接口
/// 防止暴力破解，限制每个 IP 的登录尝试次数
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LoginController(ILogger<LoginController> logger) : ControllerBase
{
    private readonly ILogger<LoginController> _logger = logger;

    // 模拟用户数据库
    private static readonly Dictionary<string, string> _users = new()
    {
        { "admin", "123456" },
        { "user1", "password1" },
        { "user2", "password2" }
    };

    /// <summary>
    /// 登录接口 - 应用限流保护
    /// 每个 IP 每分钟最多尝试 5 次
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        _logger.LogInformation($"登录尝试：用户={request.Username}, IP={ip}");

        // 模拟登录逻辑
        if (_users.TryGetValue(request.Username, out var password) && password == request.Password)
        {
            _logger.LogInformation($"登录成功：用户={request.Username}, IP={ip}");
            return Ok(new
            {
                success = true,
                message = "登录成功",
                token = Guid.NewGuid().ToString("N")
            });
        }

        _logger.LogWarning($"登录失败：用户={request.Username}, IP={ip}");
        return Unauthorized(new
        {
            success = false,
            message = "用户名或密码错误"
        });
    }

    /// <summary>
    /// 获取登录限流状态（测试用）
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return Ok(new
        {
            ip = ip,
            message = "当前 IP 登录限流：每分钟最多 5 次尝试"
        });
    }
}

public record LoginRequest(string Username, string Password);
