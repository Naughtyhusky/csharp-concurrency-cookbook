using System.Security.Claims;

namespace AsyncLocalDemo.Infrastructure
{
    /// <summary>
    /// 基于 AsyncLocal&lt;T&gt; 的当前登录用户上下文。
    ///
    /// 解决的痛点：
    ///   传统做法中，Service 层获取当前用户必须注入 IHttpContextAccessor，
    ///   几乎每个 Service 都要写：
    ///     public OrderService(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor) { }
    ///   这不仅繁琐，还让 Service 层和 HTTP 基础设施产生了不必要的耦合。
    ///
    /// 新方案：
    ///   在 CurrentUserMiddleware 中（认证完成后），从 HttpContext.User 解析 Claims，
    ///   调用 CurrentUser.Set() 写入 AsyncLocal；
    ///   业务代码直接用 CurrentUser.Info 或 CurrentUser.UserId 获取，零注入，零耦合。
    ///
    /// 为什么是线程安全的？
    ///   AsyncLocal 的写时复制语义保证每个请求拥有独立的 ExecutionContext 副本，
    ///   并发请求之间的用户信息完全隔离，不存在串号（A 请求看到 B 请求用户信息）的问题。
    /// </summary>
    public static class CurrentUser
    {
        private static readonly AsyncLocal<UserInfo?> _userInfo = new();

        /// <summary>
        /// 当前登录用户信息。匿名请求时为 null。
        /// </summary>
        public static UserInfo? Info => _userInfo.Value;

        /// <summary>
        /// 当前用户 ID。未登录时返回 null。
        /// </summary>
        public static string? UserId => _userInfo.Value?.UserId;

        /// <summary>
        /// 当前用户名。未登录时返回 "Anonymous"。
        /// </summary>
        public static string UserName => _userInfo.Value?.UserName ?? "Anonymous";

        /// <summary>
        /// 当前用户是否已登录
        /// </summary>
        public static bool IsAuthenticated => _userInfo.Value?.IsAuthenticated == true;

        /// <summary>
        /// 判断当前用户是否拥有指定角色
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public static bool IsInRole(string role) => _userInfo.Value?.IsInRole(role) == true;

        /// <summary>
        /// 由 CurrentUserMiddleware 在认证完成后调用，将 Claims 解析并存入 AsyncLocal。
        /// 业务代码无需（也不应该）调用此方法。
        /// </summary>
        internal static void Set(ClaimsPrincipal? principal)
        {
            if (principal?.Identity?.IsAuthenticated != true)
            {
                _userInfo.Value = null;
                return;
            }

            _userInfo.Value = new UserInfo
            {
                UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirstValue("userId")
                         ?? "unknown",
                UserName = principal.FindFirstValue(ClaimTypes.Name)
                           ?? principal.FindFirstValue("name")
                           ?? "unknown",
                Email = principal.FindFirstValue(ClaimTypes.Email)
                        ?? principal.FindFirstValue("email")
                        ?? string.Empty,
                Roles = principal.FindAll(ClaimTypes.Role)
                                 .Select(c => c.Value)
                                 .ToList()
                                 .AsReadOnly(),
                IsAuthenticated = true
            };
        }

        /// <summary>
        /// 仅用于单元测试或集成测试中模拟当前用户，不要在生产业务代码中调用。
        /// </summary>
        public static void SetForTesting(UserInfo? userInfo)
        {
            _userInfo.Value = userInfo;
        }
    }
}
