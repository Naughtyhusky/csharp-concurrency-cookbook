using AsyncLocalDemo.Infrastructure;

namespace AsyncLocalDemo.Middleware
{
    /// <summary>
    /// 当前用户解析中间件。
    ///
    /// 职责：
    ///   在认证中间件（UseAuthentication）执行完毕后，从 HttpContext.User（ClaimsPrincipal）
    ///   中提取用户信息，调用 CurrentUser.Set() 写入 AsyncLocal。
    ///   此后，业务代码（Controller、Service、Repository）均可直接通过 CurrentUser.Info 获取用户信息，
    ///   无需注入 IHttpContextAccessor。
    ///
    /// 注册顺序：必须放在 UseAuthentication() 之后、UseAuthorization() 之前。
    ///
    ///   app.UseAuthentication();
    ///   app.UseCurrentUser();      ← 在这里
    ///   app.UseAuthorization();
    /// </summary>
    public sealed class CurrentUserMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // 认证中间件已经处理完毕，此时 HttpContext.User 已经包含了解析后的 Claims
            CurrentUser.Set(context.User);

            await next(context);
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class CurrentUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
            => app.UseMiddleware<CurrentUserMiddleware>();
    }
}
