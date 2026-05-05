# CancellationToken Web API 测试脚本
# 使用 PowerShell 测试各个接口

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "CancellationToken Web API 测试脚本" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 检查 API 是否运行
Write-Host "检查 API 是否运行..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/products" -Method Get -TimeoutSec 2 -ErrorAction Stop
    Write-Host "✅ API 正在运行" -ForegroundColor Green
} catch {
    Write-Host "❌ API 未运行，请先启动：dotnet run --project CancellationToken.WebApi.Demo" -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试1：商品查询（正常完成）" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/products?simulatedDelayMs=1000" -Method Get
Write-Host "✅ 查询成功，返回 $($response.data.data.Count) 条记录" -ForegroundColor Green

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试2：订单创建（正常完成）" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

$orderRequest = @{
    items = @(
        @{
            productId = 1
            productName = "Product 1"
            quantity = 2
            price = 99.99
        }
    )
    simulatedPaymentDelayMs = 2000
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/orders" -Method Post -Body $orderRequest -ContentType "application/json"
Write-Host "✅ 订单创建成功，订单号：$($response.data.order.orderNo)" -ForegroundColor Green

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试3：报表生成（正常完成）" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

$reportRequest = @{
    reportType = "Sales"
    startDate = "2024-01-01"
    endDate = "2024-12-31"
    simulatedDelayMs = 2000
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/reports/generate" -Method Post -Body $reportRequest -ContentType "application/json"
Write-Host "✅ 报表生成成功，处理了 $($response.data.processedRecords) 条记录" -ForegroundColor Green

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试4：订单创建（超时测试）" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "提示：这个测试会等待 30 秒超时，如需取消请按 Ctrl+C" -ForegroundColor Yellow
Write-Host ""

$orderRequest = @{
    items = @(
        @{
            productId = 1
            productName = "Product 1"
            quantity = 2
            price = 99.99
        }
    )
    simulatedPaymentDelayMs = 35000  # 35秒，超过30秒超时限制
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/orders" -Method Post -Body $orderRequest -ContentType "application/json" -TimeoutSec 35
    Write-Host "❌ 测试失败：应该超时但成功了" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 408) {
        Write-Host "✅ 测试通过：订单创建超时（HTTP 408）" -ForegroundColor Green
    } else {
        Write-Host "⚠️ 收到错误：$($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "所有测试完成！" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示：" -ForegroundColor Yellow
Write-Host "- 要测试客户端取消，请使用 curl 命令并在执行过程中按 Ctrl+C" -ForegroundColor Yellow
Write-Host "- 要测试后台服务优雅停机，请启动应用后按 Ctrl+C" -ForegroundColor Yellow
Write-Host ""
