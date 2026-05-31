namespace AsyncLocalDemo.Infrastructure
{
    /// <summary>
    /// 当前登录用户信息。
    /// 这个类是不可变的（init-only properties），确保一旦从 Claims 中解析出来就不会被意外修改。
    /// </summary>
    public sealed class UserInfo
    {
        public required string UserId { get; init; }
        public required string UserName { get; init; }
        public required string Email { get; init; }
        public required IReadOnlyList<string> Roles { get; init; }
        public bool IsAuthenticated { get; init; } = true;

        /// <summary>
        /// 判断用户是否拥有指定角色
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public bool IsInRole(string role) =>
            Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

        public override string ToString() =>
            $"[UserId={UserId}, UserName={UserName}, Roles={string.Join(",", Roles)}]";
    }
}
