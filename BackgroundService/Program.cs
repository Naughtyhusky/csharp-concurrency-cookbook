using BackgroundService.Configuration;
using BackgroundService.Services;

namespace BackgroundService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 绑定配置
            builder.Services.Configure<BackgroundServicesOptions>(
                builder.Configuration.GetSection(BackgroundServicesOptions.SectionName));

            // Add services to the container.
            builder.Services.AddControllers();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // 注册后台任务队列（单例）
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            // 注册健康检查
            builder.Services.AddHealthChecks()
                .AddCheck<BackgroundQueueHealthCheck>("background-queue");

            // 注册后台服务
            // 注意：默认情况下所有服务都会启动，如果只想测试某个服务，可以注释掉其他的

            // 1. 简单的 IHostedService 示例（使用 Timer）
            // builder.Services.AddHostedService<SimpleHostedService>();

            // 2. 使用 PeriodicTimer 的定时任务
            builder.Services.AddHostedService<TimedBackgroundService>();

            // 3. 队列处理服务（配合 OrderController 使用）
            builder.Services.AddHostedService<QueuedBackgroundService>();

            // 4. 数据同步服务
            builder.Services.AddHostedService<DataSyncService>();

            // 5. 邮件发送服务（带重试机制）
            // 注意：如果同时启动 QueuedBackgroundService 和 EmailSenderService，
            // 它们会竞争同一个队列中的任务
            // builder.Services.AddHostedService<EmailSenderService>();

            // 6. 优雅停止示例服务
            // builder.Services.AddHostedService<GracefulShutdownService>();

            // 7. 并发控制服务（需要手动指定最大并发数）
            // builder.Services.AddSingleton<ConcurrentTaskService>(sp =>
            //     new ConcurrentTaskService(
            //         sp.GetRequiredService<IBackgroundTaskQueue>(),
            //         sp.GetRequiredService<ILogger<ConcurrentTaskService>>(),
            //         maxConcurrency: 3
            //     ));
            // builder.Services.AddHostedService(sp => sp.GetRequiredService<ConcurrentTaskService>());

            // 配置优雅停止超时（默认 5 秒）
            builder.Host.ConfigureHostOptions(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromSeconds(30); // 等待 30 秒
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // 映射健康检查端点
            app.MapHealthChecks("/health");

            app.Run();
        }
    }
}
