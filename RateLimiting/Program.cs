using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using RateLimiting.Services;
using StackExchange.Redis;

namespace RateLimiting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // 注册数据库服务（演示 SemaphoreSlim 并发控制）
            builder.Services.AddSingleton<DatabaseService>();

            // （可选）注册 Redis 连接
            // 如果需要测试分布式限流，取消下面的注释并配置 Redis
            // builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            // {
            //     return ConnectionMultiplexer.Connect("localhost:6379");
            // });

            // 配置限流策略
            builder.Services.AddRateLimiter(options =>
            {
                // 1. 固定窗口限流（每10秒最多100次请求）
                options.AddFixedWindowLimiter("fixed", limiterOptions =>
                {
                    limiterOptions.Window = TimeSpan.FromSeconds(10);
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.QueueLimit = 10;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // 2. 滑动窗口限流（每10秒最多100次请求，分为10个段）
                options.AddSlidingWindowLimiter("sliding", limiterOptions =>
                {
                    limiterOptions.Window = TimeSpan.FromSeconds(10);
                    limiterOptions.PermitLimit = 100;
                    limiterOptions.SegmentsPerWindow = 10;
                    limiterOptions.QueueLimit = 10;
                });

                // 3. 令牌桶限流（容量100，每秒补充10个令牌）
                options.AddTokenBucketLimiter("token", limiterOptions =>
                {
                    limiterOptions.TokenLimit = 100;
                    limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
                    limiterOptions.TokensPerPeriod = 10;
                    limiterOptions.AutoReplenishment = true;
                    limiterOptions.QueueLimit = 5;
                });

                // 4. 并发限流（最多10个并发请求）
                options.AddConcurrencyLimiter("concurrency", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 10;
                    limiterOptions.QueueLimit = 5;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // 5. 【实战案例1】登录接口限流：按IP，每分钟最多5次
                options.AddPolicy("login", httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 5,  // 防止暴力破解：每分钟最多5次尝试
                            QueueLimit = 0    // 不排队，直接拒绝
                        });
                });

                // 6. 【实战案例2】报告生成限流：并发限制，最多5个同时生成
                options.AddConcurrencyLimiter("report", limiterOptions =>
                {
                    limiterOptions.PermitLimit = 5;      // 最多5个并发
                    limiterOptions.QueueLimit = 10;      // 允许10个排队
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                // 7. 按IP限流（每个IP每分钟最多10次）
                options.AddPolicy("perIp", httpContext =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 10
                        });
                });

                // 8. 按用户限流（VIP用户和普通用户不同限额）
                options.AddPolicy("perUser", httpContext =>
                {
                    var userId = httpContext.User.Identity?.Name ?? "anonymous";
                    var isVip = httpContext.User.IsInRole("VIP");

                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: userId,
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = isVip ? 1000 : 100,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            TokensPerPeriod = isVip ? 1000 : 100,
                            AutoReplenishment = true
                        });
                });

                // 9. 全局限流（所有请求共享，每10秒1000次）
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "global",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromSeconds(10),
                            PermitLimit = 1000
                        });
                });

                // 自定义限流响应
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    double? retryAfter = null;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                    {
                        retryAfter = retryAfterValue.TotalSeconds;
                    }

                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "请求过于频繁，请稍后再试",
                        retryAfter = retryAfter,
                        timestamp = DateTime.UtcNow
                    }, cancellationToken);
                };
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // 必须在 UseAuthorization 之后
            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }
    }
}
