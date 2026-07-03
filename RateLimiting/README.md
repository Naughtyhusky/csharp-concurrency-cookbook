# 限流与并发控制示例

> 本项目演示了限流与并发控制的各种实现方式，包括自定义限流算法和 ASP.NET Core 内置限流中间件。

## 🎯 快速上手

想立即体验限流效果？只需两步：

1. **启动服务器**：`dotnet run`
2. **运行测试脚本**：在新的 PowerShell 窗口执行 `.\test-scripts.ps1`

测试脚本提供交互式菜单，可以直观看到各种限流算法的效果！

---

## 📋 目录结构

```
RateLimiting/
├── Controllers/                    # API 控制器
│   ├── RateLimitDemoController.cs  # ASP.NET Core 内置限流演示
│   ├── CustomRateLimitController.cs# 自定义限流算法演示
│   ├── ConcurrencyController.cs    # SemaphoreSlim 并发控制演示
│   ├── LoginController.cs          # 实战案例1：登录接口保护
│   ├── ReportController.cs         # 实战案例2：高成本接口保护
│   └── ApiGatewayController.cs     # 实战案例3：API网关限流
├── RateLimiters/                   # 自定义限流算法实现
│   ├── FixedWindowRateLimiter.cs   # 固定窗口限流器
│   ├── SlidingWindowRateLimiter.cs # 滑动窗口限流器
│   ├── TokenBucketRateLimiter.cs   # 令牌桶限流器
│   └── LeakyBucketRateLimiter.cs   # 漏桶限流器
├── Services/
│   └── DatabaseService.cs          # 数据库服务（演示并发控制）
├── DistributedLimiters/            # 分布式限流器实现
│   ├── RedisTokenBucketRateLimiter.cs  # Redis令牌桶限流器
│   └── RedisSlidingWindowRateLimiter.cs# Redis滑动窗口限流器
├── test-scripts.ps1                # 🌟 交互式测试脚本（推荐使用）
└── README.md                       # 本文档
```

## 🚀 快速开始

### 1. 运行 Web API

```bash
cd RateLimiting
dotnet run
```

启动后访问：`https://localhost:5001` 或 `http://localhost:5000`

### 2. 使用交互式测试脚本 ⭐ 推荐

本项目提供了一个交互式的 PowerShell 测试脚本 `test-scripts.ps1`，方便快速测试各种限流场景。

**使用步骤**：

1. **启动 API 服务器**（第一个终端窗口）：
   ```bash
   cd RateLimiting
   dotnet run
   ```

2. **运行测试脚本**（第二个 PowerShell 窗口）：
   ```powershell
   cd RateLimiting
   .\test-scripts.ps1
   ```

**测试菜单**：

```
╔════════════════════════════════════════════════════════════════════════════╗
║                      限流与并发控制 - 测试菜单                              ║
╚════════════════════════════════════════════════════════════════════════════╝

请选择要执行的测试：

1. 测试固定窗口限流（边界突刺）
2. 测试滑动窗口限流（平滑限流）
3. 测试令牌桶限流（突发流量）
4. 测试漏桶限流（平滑输出）
5. 测试并发控制（SemaphoreSlim）
6. 测试 ASP.NET Core 内置限流
7. 查看限流器状态
8. 运行所有测试
0. 退出
```

**测试脚本功能**：
- ✅ **交互式菜单**：无需手动输入复杂命令
- ✅ **彩色输出**：通过 ✅ 和 ❌ 直观显示结果
- ✅ **实时统计**：显示限流器状态（计数、令牌数等）
- ✅ **场景演示**：自动演示边界突刺、突发流量等典型场景
- ✅ **一键测试**：选项 8 可运行所有测试

**示例输出**：

```
=== 测试令牌桶限流（突发流量） ===
限制：容量10，每秒补充1个令牌

等待5秒（令牌积累）...

突发15个请求：

[1] ✅ 通过 - 剩余令牌: 9.0
[2] ✅ 通过 - 剩余令牌: 8.0
...
[10] ✅ 通过 - 剩余令牌: 0.0
[11] ❌ 被限流 - 剩余令牌: 0.0
```



## 📊 API 端点说明

### ASP.NET Core 内置限流演示

| 端点 | 限流策略 | 说明 |
|------|---------|------|
| `GET /api/RateLimitDemo/fixed` | 固定窗口 | 每10秒最多100次 |
| `GET /api/RateLimitDemo/sliding` | 滑动窗口 | 每10秒最多100次（更平滑）|
| `GET /api/RateLimitDemo/token` | 令牌桶 | 容量100，每秒补充10个 |
| `GET /api/RateLimitDemo/concurrency` | 并发限流 | 最多10个并发请求 |
| `GET /api/RateLimitDemo/per-ip` | 按IP限流 | 每个IP每分钟10次 |
| `GET /api/RateLimitDemo/per-user` | 按用户限流 | VIP 1000次/分，普通100次/分 |
| `GET /api/RateLimitDemo/health` | 无限流 | 健康检查，不限流 |
| `GET /api/RateLimitDemo/report/generate` | 并发限流 | 高成本操作，限制并发 |

### 自定义限流算法演示

| 端点 | 限流算法 | 说明 |
|------|---------|------|
| `GET /api/CustomRateLimit/fixed-window` | 固定窗口 | 每10秒最多5次 |
| `GET /api/CustomRateLimit/sliding-window` | 滑动窗口 | 每10秒最多5次 |
| `GET /api/CustomRateLimit/token-bucket` | 令牌桶 | 容量10，每秒补充1个 |
| `GET /api/CustomRateLimit/leaky-bucket` | 漏桶 | 容量10，每秒漏出1个 |
| `GET /api/CustomRateLimit/status` | 状态查询 | 查看所有限流器状态 |

### 并发控制演示

| 端点 | 说明 |
|------|------|
| `GET /api/Concurrency/query` | 单个数据库查询 |
| `POST /api/Concurrency/batch-query` | 批量查询（自动限制并发） |
| `GET /api/Concurrency/active-connections` | 查看活跃连接数 |

### 实战案例演示

#### 案例1：登录接口保护（防暴力破解）

| 端点 | 说明 |
|------|------|
| `POST /api/Login` | 登录接口（每IP每分钟最多5次） |
| `GET /api/Login/status` | 查看当前IP的限流状态 |

#### 案例2：高成本接口保护（报告生成）

| 端点 | 说明 |
|------|------|
| `POST /api/Report/generate-sales` | 生成销售报告（并发限制5个） |
| `POST /api/Report/generate-users` | 生成用户报告（并发限制5个） |
| `GET /api/Report/{reportId}/status` | 查询报告生成状态（无限流） |

#### 案例3：API网关限流（按订阅等级）

| 端点 | 说明 |
|------|------|
| `GET /api/ApiGateway/protected/data` | 受保护数据（需API Key，单机版） |
| `GET /api/ApiGateway/protected/data-distributed` | 受保护数据（需Redis，分布式版） |
| `GET /api/ApiGateway/key-info` | 查看API Key信息 |
| `GET /api/ApiGateway/test-keys` | 获取测试用API Keys |

**测试用API Keys**：
- `free-key-123` - 免费版：容量100，每秒补充1个令牌
- `pro-key-456` - 专业版：容量1000，每秒补充10个令牌
- `enterprise-key-789` - 企业版：容量10000，每秒补充100个令牌

## 🧪 测试场景

> 💡 **推荐方式**：直接运行 `.\test-scripts.ps1` 进行交互式测试，以下是手动测试的命令示例。

### 1. 测试固定窗口的边界突刺问题

```bash
# 使用 PowerShell 快速发送请求
for ($i=1; $i -le 20; $i++) {
	Invoke-WebRequest -Uri "http://localhost:5000/api/CustomRateLimit/fixed-window" -Method GET
	Write-Host "请求 $i 完成"
}
```

**预期结果**：前5次通过，之后被限流，10秒后重置。

### 2. 测试滑动窗口的平滑限流

```bash
# 每0.5秒发送一次请求，持续20次
for ($i=1; $i -le 20; $i++) {
	$response = Invoke-WebRequest -Uri "http://localhost:5000/api/CustomRateLimit/sliding-window" -Method GET
	Write-Host "请求 $i : $($response.StatusCode)"
	Start-Sleep -Milliseconds 500
}
```

**预期结果**：请求会被平滑地限流，而不是在窗口边界突然拒绝。

### 3. 测试令牌桶的突发流量

```bash
# 先等待5秒（令牌积累）
Start-Sleep -Seconds 5

# 快速发送15个请求
for ($i=1; $i -le 15; $i++) {
	$response = Invoke-WebRequest -Uri "http://localhost:5000/api/CustomRateLimit/token-bucket" -Method GET
	Write-Host "请求 $i : $($response.StatusCode)"
}
```

**预期结果**：前10次通过（桶中有10个令牌），之后被限流。

### 4. 测试并发控制

```bash
# 批量查询（50个查询，但最多10个并发）
$sqls = @()
for ($i=1; $i -le 50; $i++) {
	$sqls += "SELECT * FROM users WHERE id = $i"
}

$body = $sqls | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:5000/api/Concurrency/batch-query" -Method POST -Body $body -ContentType "application/json"
```

**预期结果**：50个查询会被分批执行，同时最多10个在执行。

### 5. 测试按IP限流

```bash
# 快速发送15次请求（超过每分钟10次的限制）
for ($i=1; $i -le 15; $i++) {
	$response = Invoke-WebRequest -Uri "http://localhost:5000/api/RateLimitDemo/per-ip" -Method GET
	Write-Host "请求 $i : $($response.StatusCode)"
}
```

**预期结果**：前10次通过，之后返回 429 (Too Many Requests)。

## 📈 性能对比

| 算法 | 吞吐量 | 内存占用 | 突发流量支持 | 精确度 |
|------|--------|---------|-------------|--------|
| 固定窗口 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌ | ⭐⭐⭐ |
| 滑动窗口 | ⭐⭐⭐ | ⭐⭐⭐ | ⚠️ | ⭐⭐⭐⭐⭐ |
| 令牌桶 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ✅ | ⭐⭐⭐⭐ |
| 漏桶 | ⭐⭐⭐ | ⭐⭐⭐⭐ | ❌ | ⭐⭐⭐⭐ |

## 🔍 核心概念

### 1. 固定窗口（Fixed Window）

**优点**：
- 实现简单
- 内存占用低
- 性能高

**缺点**：
- 边界突刺问题：窗口切换时可能瞬间通过双倍请求

**适用场景**：
- 简单限流
- 对精确度要求不高
- 单机应用

### 2. 滑动窗口（Sliding Window）

**优点**：
- 精确限流
- 无边界突刺问题
- 可实时统计

**缺点**：
- 内存占用高（记录每个请求时间戳）
- 性能较低

**适用场景**：
- 精确限流
- 流量不大
- 需要实时统计

### 3. 令牌桶（Token Bucket）⭐ 推荐

**优点**：
- 允许突发流量
- 平滑限流
- 内存占用低
- 性能好

**缺点**：
- 实现稍复杂

**适用场景**：
- **大多数场景（推荐）**
- 需要支持突发流量
- API 网关

### 4. 漏桶（Leaky Bucket）

**优点**：
- 输出速率平滑
- 保护下游系统

**缺点**：
- 无法应对突发流量
- 延迟较高

**适用场景**：
- 保护下游系统
- 需要平滑输出
- 消息队列消费

## 🛠️ 实战建议

### 1. 选择合适的算法

- **API 接口**：令牌桶（允许突发）
- **登录接口**：固定窗口（按IP）
- **数据库操作**：并发限流（SemaphoreSlim）
- **消息队列**：漏桶（平滑输出）

### 2. 限流层次

```
┌─────────────────────────────────────┐
│       网关层限流（Nginx/Kong）        │  ← 防 DDoS
├─────────────────────────────────────┤
│      应用层限流（ASP.NET Core）       │  ← 业务限流
├─────────────────────────────────────┤
│    并发控制（SemaphoreSlim）          │  ← 资源保护
├─────────────────────────────────────┤
│      数据库限流（连接池）             │  ← 最后防线
└─────────────────────────────────────┘
```

### 3. 监控指标

- **限流命中率**：被限流的请求占比
- **令牌桶剩余量**：监控系统负载
- **队列长度**：是否需要扩容
- **响应时间**：限流是否影响性能

### 4. 降级策略

- **立即拒绝**：返回 429，告知重试时间
- **排队等待**：设置队列，延迟处理
- **返回缓存**：降级到缓存数据
- **降级服务**：返回简化的响应
