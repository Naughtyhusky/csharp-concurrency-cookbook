# 快速启动指南

## 🚀 5 分钟快速体验

### 步骤1：启动 Web API 项目

打开终端，执行：

```bash
cd CancellationToken.WebApi.Demo
dotnet run
```

你会看到：

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
...
```

### 步骤2：测试客户端取消（最直观）

**使用 curl**（推荐）：

1. **保持步骤1的 API 服务运行**（不要关闭那个终端窗口）
2. **打开新的终端窗口**（第二个窗口）
3. 在新窗口执行命令：

```bash
curl "http://localhost:5000/api/products?simulatedDelayMs=10000"
```

4. **在 10 秒内，在这个新窗口（curl 命令所在窗口）按 Ctrl+C**
   - ⚠️ **注意**：是在执行 curl 的窗口按 Ctrl+C，不是 API 服务的窗口
   - ✅ 效果：取消这次 HTTP 请求（客户端断开连接）
   - ❌ 不会关闭 API 服务

5. 回到第一个终端（API 服务），观察日志，应该看到：

```
info: 商品查询被取消（客户端断开或手动取消）
```

**说明**：
- Ctrl+C 只会取消 curl 命令（这次请求）
- API 服务继续运行，可以继续接收其他请求
- 这模拟了用户在浏览器中关闭标签页的场景

**使用 PowerShell**（Windows）：

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/products?simulatedDelayMs=10000" -Method Get
# 按 Ctrl+C 取消
```

**使用 Postman**：

1. 发送 GET 请求：`http://localhost:5000/api/products?simulatedDelayMs=10000`
2. 点击 Postman 的 "Cancel" 按钮
3. 观察 API 日志

### 步骤3：测试超时控制

执行命令（会等待 30 秒）：

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [{"productId": 1, "productName": "Product 1", "quantity": 2, "price": 99.99}],
    "simulatedPaymentDelayMs": 35000
  }'
```

**预期结果**：
- 30 秒后自动超时
- 返回：`"errorCode": "TIMEOUT"`, HTTP 408
- 日志显示："订单创建超时（30秒）"

### 步骤4：测试优雅停机（关闭程序）

> ⚠️ **重要**：这个测试会关闭整个 API 服务

1. 确保 API 服务正在运行（步骤1）
2. 观察日志，应该看到后台服务每 5 秒处理一批订单：

```
info: 订单后台处理服务已启动
info: 开始处理待处理订单...
info:   处理订单 1001...
info:   订单 1001 处理完成
info:   处理订单 1002...
```

3. **在 API 服务的控制台窗口（步骤1的窗口）按 Ctrl+C**
   - ⚠️ **注意**：这次是在 API 服务的窗口按 Ctrl+C，会关闭程序
   - ✅ 效果：触发应用优雅停机

4. 观察日志：

```
（按 Ctrl+C）

info: Application is shutting down...
info: 收到停止信号，正在退出...
info: 订单后台处理服务已停止
```

**关键点**：
- 当前正在处理的订单会完成
- 未开始的订单不会处理
- 服务优雅退出

**测试完成后**：
- 如果想继续测试其他场景，需要重新运行 `dotnet run` 启动 API 服务

---

## 🧪 自动化测试脚本

### PowerShell 脚本（Windows）

```powershell
.\test-api.ps1
```

### Bash 脚本（Linux/macOS）

```bash
chmod +x test-api.sh
./test-api.sh
```

---


## ❓ 常见问题

### Q1: API 无法启动？

A: 检查端口 5000 是否被占用：

```bash
# Windows
netstat -ano | findstr :5000

# Linux/macOS
lsof -i :5000
```

### Q2: curl 命令不存在？

A: 
- **Windows**：Windows 10/11 已内置 curl
- **Linux/macOS**：系统已内置
- 或使用 Postman、PowerShell 的 `Invoke-RestMethod`

### Q3: 如何查看详细日志？

A: 日志默认输出到控制台，日志级别为 Information。

要启用详细日志，修改 `Program.cs`：

```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

---

**祝体验愉快！** 🎉
