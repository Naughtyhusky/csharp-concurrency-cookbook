
using AsyncLocalDemo.Middleware;
using AsyncLocalDemo.Services;

namespace AsyncLocalDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── 服务注册 ────────────────────────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // 注册业务 Service（不再需要 IHttpContextAccessor）
            builder.Services.AddScoped<IOrderService, OrderService>();

            // 演示：注册 IHttpContextAccessor（仅用于说明对比，业务代码已不再使用它）
            // builder.Services.AddHttpContextAccessor();

            // ── 管道配置 ────────────────────────────────────────────────────────────
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            // ✅ 第一步：注册 TraceMiddleware（放在最前端，让所有后续中间件都能取到 TraceId）
            app.UseTracing();

            // ASP.NET Core 内置认证（如果配置了 JWT 等，UseAuthentication 会填充 HttpContext.User）
            app.UseAuthentication();

            // ✅ 第二步：注册 CurrentUserMiddleware（放在 UseAuthentication 之后，从 Claims 填充 CurrentUser）
            // 注意：必须在 UseAuthentication 之后，否则 HttpContext.User 还未被填充
            app.UseCurrentUser();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
