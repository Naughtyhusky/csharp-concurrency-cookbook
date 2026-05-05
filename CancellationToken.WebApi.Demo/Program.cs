using CancellationToken.WebApi.Demo.BackgroundServices;
using CancellationToken.WebApi.Demo.Services;

namespace CancellationToken.WebApi.Demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ========== 配置服务 ==========

            // 注册业务服务
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IReportService, ReportService>();

            // 注册后台服务（演示优雅停机）
            builder.Services.AddHostedService<OrderBackgroundService>();

            // 配置控制器
            builder.Services.AddControllers();

            // 配置 OpenAPI/Swagger
            builder.Services.AddOpenApi();

            // 配置 CORS（允许前端测试）
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // 配置日志（显示详细日志）
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            // ========== 配置 Kestrel（可选）==========
            // 配置请求超时等参数
            builder.WebHost.ConfigureKestrel(options =>
            {
                // 请求体最大大小：10MB
                options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;

                // 请求超时：60秒（默认）
                // options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
            });

            var app = builder.Build();

            // ========== 配置中间件管道 ==========

            // 开发环境：启用 OpenAPI
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // 启用 CORS
            app.UseCors();

            // 启用授权
            app.UseAuthorization();

            // 映射控制器
            app.MapControllers();

            // ========== 启动应用 ==========

            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("CancellationToken Web API Demo");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();
            Console.WriteLine("API 文档：http://localhost:5000/openapi/v1.json");
            Console.WriteLine();
            Console.WriteLine("可用的接口：");
            Console.WriteLine("  GET    /api/products                 - 查询商品列表");
            Console.WriteLine("  GET    /api/products/{id}            - 获取商品详情");
            Console.WriteLine("  POST   /api/orders                   - 创建订单");
            Console.WriteLine("  POST   /api/orders/batch             - 批量创建订单");
            Console.WriteLine("  POST   /api/reports/generate         - 生成报表");
            Console.WriteLine("  POST   /api/reports/generate-multiple - 生成多个报表");
            Console.WriteLine();
            Console.WriteLine("测试命令示例：");
            Console.WriteLine("  curl http://localhost:5000/api/products?simulatedDelayMs=5000");
            Console.WriteLine("  (在 5 秒内按 Ctrl+C 取消)");
            Console.WriteLine();
            Console.WriteLine("按 Ctrl+C 停止应用（观察优雅停机）");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            app.Run();
        }
    }
}
