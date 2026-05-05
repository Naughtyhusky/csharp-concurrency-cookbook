# CancellationToken Web API 演示项目

> **配套博客**：第 06 章《CancellationToken：优雅地取消异步操作》  
> **GitHub**：[csharp-concurrency-cookbook](https://github.com/Naughtyhusky/csharp-concurrency-cookbook)

---

## 📋 项目简介

这是一个完整的 ASP.NET Core Web API 项目，演示 CancellationToken 在真实业务场景中的应用。

### 为什么需要这个项目？

在实际的 Web API 开发中，我们经常遇到这些场景：

1. **用户在列表页筛选商品**，查询可能需要几秒，用户点"取消"应该立即停止
2. **用户提交订单**，支付接口可能很慢，应该设置超时自动取消
3. **用户生成报表**，可能需要几十秒，用户应该能随时取消
4. **应用关闭时**，后台服务应该优雅停止，而不是强制中断

本项目通过 4 个典型的业务场景，演示如何正确使用 CancellationToken。

---

## 🎯 演示场景

### 场景1：商品查询（复杂筛选 + 客户端取消）

**业务场景**：
- 用户在商品列表页进行复杂筛选（类别、价格区间、关键词等）
- 查询可能需要几秒钟（数据库多表连接、全文搜索）
- 用户点击"取消"按钮，或关闭浏览器标签页
- 系统应该立即停止查询，释放数据库连接

**技术要点**：
- ✅ ASP.NET Core 自动注入 CancellationToken
- ✅ 客户端断开时自动取消
- ✅ 服务层传递 Token 给数据访问层
- ✅ 返回 499 状态码（Client Closed Request）

**API 接口**：
```bash
GET /api/products?category=Electronics&simulatedDelayMs=5000
```

---

### 场景2：订单创建（长时间操作 + 超时控制）

**业务场景**：
- 用户提交订单，需要：验证 → 扣库存 → 调用支付接口 → 更新状态
- 支付接口可能需要 5-30 秒（第三方服务）
- 系统设置 30 秒超时，超时后自动取消
- 用户可以手动取消订单
- 取消时需要回滚已执行的操作（恢复库存等）

**技术要点**：
- ✅ 使用 LinkedTokenSource 组合用户取消 + 超时
- ✅ 使用 when 子句区分取消原因（用户 vs 超时）
- ✅ 取消时执行清理逻辑（回滚事务）
- ✅ 返回不同的状态码（499 = 用户取消，408 = 超时）

**API 接口**：
```bash
POST /api/orders
{
  "items": [
    { "productId": 1, "productName": "Product 1", "quantity": 2, "price": 99.99 }
  ],
  "simulatedPaymentDelayMs": 10000
}
```

---

### 场景3：报表生成（CPU 密集型任务 + 取消）

**业务场景**：
- 用户请求生成销售报表（需要分析大量数据）
- 报表生成是 CPU 密集型任务，可能需要几十秒
- 用户可以随时取消报表生成
- 系统应该定期检查取消信号，并显示进度

**技术要点**：
- ✅ 使用 Task.Run 处理 CPU 密集型任务
- ✅ 在循环中定期检查 CancellationToken（每批次检查一次）
- ✅ 提供进度报告（日志输出）
- ✅ 取消时记录已处理的数据量

**API 接口**：
```bash
POST /api/reports/generate
{
  "reportType": "Sales",
  "startDate": "2024-01-01",
  "endDate": "2024-12-31",
  "simulatedDelayMs": 10000
}
```

---

### 场景4：后台服务（优雅停机）

**业务场景**：
- 后台服务持续运行，定期处理待处理的订单
- 用户按 Ctrl+C 停止应用
- 后台服务应该：完成当前订单 → 记录日志 → 优雅退出

**技术要点**：
- ✅ 继承 BackgroundService 基类
- ✅ ExecuteAsync 接收 stoppingToken
- ✅ 循环中检查 stoppingToken.IsCancellationRequested
- ✅ 捕获 OperationCanceledException 并优雅退出

---

## 🚀 快速开始

### 1. 启动项目

```bash
cd CancellationToken.WebApi.Demo
dotnet run
```

启动后，你会看到：

```
============================================================
CancellationToken Web API Demo
============================================================

API 文档：http://localhost:5000/openapi/v1.json

可用的接口：
  GET    /api/products                 - 查询商品列表
  GET    /api/products/{id}            - 获取商品详情
  POST   /api/orders                   - 创建订单
  POST   /api/orders/batch             - 批量创建订单
  POST   /api/reports/generate         - 生成报表
  POST   /api/reports/generate-multiple - 生成多个报表

测试命令示例：
  curl http://localhost:5000/api/products?simulatedDelayMs=5000
  (在 5 秒内按 Ctrl+C 取消)

按 Ctrl+C 停止应用（观察优雅停机）
============================================================
```

### 2. 测试 API 接口

你可以使用以下工具测试接口：

- **curl**：命令行工具（推荐，下面的示例都使用 curl）
- **Postman**：图形化工具，方便测试
- **VS Code REST Client**：在 VS Code 中测试
- **浏览器**：GET 请求可以直接在浏览器中访问

---

## 🧪 测试步骤

### 测试1：商品查询 - 客户端取消

> ⚠️ **重要说明**：此测试需要两个终端窗口
> - 窗口1：运行 API 服务（`dotnet run`）
> - 窗口2：执行测试命令（curl）

**使用 curl**：

```bash
# 在新的终端窗口（窗口2）执行
curl "http://localhost:5000/api/products?simulatedDelayMs=10000"

# 在 10 秒内，在这个窗口（执行 curl 的窗口）按 Ctrl+C
# ⚠️ 注意：不是在 API 服务的窗口按 Ctrl+C
```

**预期结果**：
- 服务端日志显示："商品查询已取消"
- curl 返回错误（连接中断）
- ✅ API 服务继续运行（不会关闭）

**使用 Postman**：

1. 发送请求：`GET http://localhost:5000/api/products?simulatedDelayMs=10000`
2. 在 10 秒内点击 Postman 的 "Cancel" 按钮
3. 观察服务端日志

---

### 测试2：订单创建 - 超时取消

**场景1：正常完成（3秒）**

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": 1, "productName": "Product 1", "quantity": 2, "price": 99.99}
    ],
    "simulatedPaymentDelayMs": 3000
  }'
```

**预期结果**：
- 3 秒后返回：`"message": "订单创建成功"`
- 日志显示完整流程：验证 → 扣库存 → 支付 → 更新状态

**场景2：超时取消（35秒，超过30秒限制）**

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": 1, "productName": "Product 1", "quantity": 2, "price": 99.99}
    ],
    "simulatedPaymentDelayMs": 35000
  }'
```

**预期结果**：
- 30 秒后自动取消
- 返回：`"errorCode": "TIMEOUT"`, HTTP 408
- 日志显示："订单创建超时（30秒）"

**场景3：用户取消（10秒延迟，5秒后手动取消）**

```bash
# 在新的终端窗口执行
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": 1, "productName": "Product 1", "quantity": 2, "price": 99.99}
    ],
    "simulatedPaymentDelayMs": 10000
  }'
# 在 10 秒内，在这个窗口（执行 curl 的窗口）按 Ctrl+C
```

**预期结果**：
- 立即取消
- 日志显示："订单创建被用户取消"
- 日志显示："正在回滚..."
- ✅ API 服务继续运行

---

### 测试3：报表生成 - CPU 密集型任务取消

**场景1：正常生成（5秒）**

```bash
curl -X POST http://localhost:5000/api/reports/generate \
  -H "Content-Type: application/json" \
  -d '{
    "reportType": "Sales",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "simulatedDelayMs": 5000
  }'
```

**预期结果**：
- 5 秒后返回报表数据
- 日志显示进度：10%, 20%, ..., 100%

**场景2：用户取消（20秒延迟，10秒后取消）**

```bash
curl -X POST http://localhost:5000/api/reports/generate \
  -H "Content-Type: application/json" \
  -d '{
    "reportType": "Sales",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "simulatedDelayMs": 20000
  }'
# 在 20 秒内按 Ctrl+C
```

**预期结果**：
- 立即停止
- 日志显示："报表生成已取消，已处理 5000 条记录"

---

### 测试4：后台服务 - 优雅停机

**步骤**：

1. 启动应用：`dotnet run`
2. 观察日志：应该看到后台服务每 5 秒处理一批订单
3. 按 Ctrl+C 停止应用
4. 观察日志：应该看到"收到停止信号，正在退出..."

**预期日志**：

```
info: OrderBackgroundService[0]
      订单后台处理服务已启动
info: OrderBackgroundService[0]
      开始处理待处理订单...
info: OrderBackgroundService[0]
      发现 3 个待处理订单
info: OrderBackgroundService[0]
        处理订单 1001...
info: OrderBackgroundService[0]
        订单 1001 处理完成
info: OrderBackgroundService[0]
        处理订单 1002...

（此时按 Ctrl+C）

info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
info: OrderBackgroundService[0]
      收到停止信号，正在退出...
info: OrderBackgroundService[0]
      订单后台处理服务已停止
```

**关键点**：
- 当前正在处理的订单（1002）会完成
- 未开始的订单（1003）不会处理
- 服务记录日志后退出

---

## 📊 项目结构

```
CancellationToken.WebApi.Demo/
├── Controllers/
│   ├── ProductsController.cs          # 商品查询（客户端取消）
│   ├── OrdersController.cs            # 订单处理（超时控制）
│   └── ReportsController.cs           # 报表生成（CPU 密集型）
├── Services/
│   ├── IProductService.cs / ProductService.cs
│   ├── IOrderService.cs / OrderService.cs
│   └── IReportService.cs / ReportService.cs
├── Models/
│   ├── Product.cs                     # 商品实体 + 查询请求
│   ├── Order.cs                       # 订单实体 + 创建请求
│   ├── Report.cs                      # 报表实体 + 请求
│   └── ApiResponse.cs                 # 统一响应格式
├── BackgroundServices/
│   └── OrderBackgroundService.cs      # 后台服务（优雅停机）
├── Program.cs                          # 启动配置
└── README.md                           # 本文件
```

---

## 🔑 关键代码解析

### 1. ASP.NET Core 自动注入 CancellationToken

```csharp
[HttpGet]
public async Task<IActionResult> Query(
    [FromQuery] ProductQueryRequest request,
    CancellationToken cancellationToken)  // 🔥 ASP.NET Core 自动注入
{
    // cancellationToken 会在以下情况自动取消：
    // 1. 客户端断开连接（用户关闭浏览器）
    // 2. 请求超时（超过 Kestrel 配置的超时时间）
    // 3. 应用关闭（Ctrl+C）

    var result = await _productService.QueryProductsAsync(request, cancellationToken);
    return Ok(result);
}
```

### 2. 组合多个取消源

```csharp
// 创建超时 Token（30秒）
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// 组合用户取消 + 超时
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken,      // 用户取消
    timeoutCts.Token);      // 超时

// 传递组合后的 Token
var result = await _orderService.CreateOrderAsync(request, linkedCts.Token);
```

### 3. 区分取消原因

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // 用户取消
    return StatusCode(499, "操作已取消");
}
catch (OperationCanceledException)
{
    // 超时取消
    return StatusCode(408, "操作超时");
}
```

### 4. 后台服务优雅停机

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await ProcessOrdersAsync(stoppingToken);
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
    // 循环退出，服务优雅停止
}
```

---

## 💡 学习要点

### ASP.NET Core 的自动取消机制

ASP.NET Core 会在以下情况自动取消 CancellationToken：

| 场景 | 何时触发 | 示例 |
|------|----------|------|
| **客户端断开** | 用户关闭浏览器/标签页 | 下载大文件时关闭页面 |
| **请求超时** | 超过 Kestrel 配置的超时时间 | 默认无限制，需手动配置 |
| **应用关闭** | Ctrl+C 或 docker stop | 优雅停机 |

### 传递性的重要性

```
HTTP 请求
  └─> ProductsController
        └─> ProductService
              └─> DbContext.SaveChangesAsync(token)  ← 必须传递！
```

如果任何一个环节不传递 Token，后续操作都无法取消！

### 状态码建议

| 场景 | 状态码 | 说明 |
|------|--------|------|
| 用户取消 | 499 | Client Closed Request（Nginx 使用） |
| 超时 | 408 | Request Timeout |
| 成功 | 200 | OK |
| 错误 | 500 | Internal Server Error |

---

## 🎓 扩展阅读

### 相关博客章节

- **第 1 章**：为什么需要手动控制取消？
- **第 2.4 章**：传递性的重要性（必读）
- **第 2.5 章**：异步编程模式下的最佳实践
- **第 7 章**：底层实现原理

### 相关示例

- **Example10**：Token 的传递性（控制台示例）
- **Example11**：异步编程模式（控制台示例）
- **本项目**：Web API 完整示例

---

## 🐛 常见问题

### Q1: 为什么返回 499 而不是 400？

A: 499 是 Nginx 使用的非标准状态码，表示"客户端主动断开连接"。这与 400（客户端错误）有本质区别：

- 400：客户端发送了错误的请求
- 499：客户端主动取消了请求（不是错误）

### Q2: 如何配置请求超时？

A: 在 Program.cs 中配置 Kestrel：

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
});
```

### Q3: 后台服务如何确保优雅停机？

A: 确保以下几点：

1. 使用 `stoppingToken` 参数
2. 循环中检查 `stoppingToken.IsCancellationRequested`
3. 所有异步操作都传递 `stoppingToken`
4. 捕获 `OperationCanceledException` 并记录日志

---

## 📚 参考资源

- **ASP.NET Core 文档**：[取消长时间运行的操作](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/cancellation-requests)
- **BackgroundService 文档**：[托管服务](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/host/hosted-services)
- **GitHub 仓库**：[csharp-concurrency-cookbook](https://github.com/Naughtyhusky/csharp-concurrency-cookbook)

---

**版本**：1.0.0  
**更新日期**：2026-05-05  
**作者**：Naughtyhusky
