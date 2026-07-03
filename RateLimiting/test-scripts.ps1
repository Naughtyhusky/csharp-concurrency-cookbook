# 限流与并发控制 - 测试脚本

## 使用说明
# 1. 先启动 API 服务器：dotnet run
# 2. 在新的 PowerShell 窗口中运行这些测试脚本

## 基础配置
$baseUrl = "http://localhost:5000"

## ============================================
## 测试 1：固定窗口限流 - 演示边界突刺问题
## ============================================
function Test-FixedWindow {
	Write-Host "`n=== 测试固定窗口限流（边界突刺问题） ===" -ForegroundColor Cyan
	Write-Host "限制：每10秒最多5次请求`n"

	# 快速发送10次请求
	for ($i = 1; $i -le 10; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/CustomRateLimit/fixed-window" -Method GET
			$content = $response.Content | ConvertFrom-Json
			Write-Host "[$i] ✅ 通过 - 当前计数: $($content.currentCount)" -ForegroundColor Green
		}
		catch {
			$statusCode = $_.Exception.Response.StatusCode.value__
			Write-Host "[$i] ❌ 被限流 (HTTP $statusCode)" -ForegroundColor Red
		}
		Start-Sleep -Milliseconds 100
	}

	Write-Host "`n等待窗口重置（10秒）..."
	Start-Sleep -Seconds 10

	# 再次发送5次请求
	Write-Host "窗口重置后，再发送5次：`n"
	for ($i = 11; $i -le 15; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/CustomRateLimit/fixed-window" -Method GET
			Write-Host "[$i] ✅ 通过" -ForegroundColor Green
		}
		catch {
			Write-Host "[$i] ❌ 被限流" -ForegroundColor Red
		}
		Start-Sleep -Milliseconds 100
	}
}

## ============================================
## 测试 2：滑动窗口限流 - 平滑限流
## ============================================
function Test-SlidingWindow {
	Write-Host "`n=== 测试滑动窗口限流（平滑限流） ===" -ForegroundColor Cyan
	Write-Host "限制：每10秒最多5次请求`n"

	# 每2秒发送一次请求，持续20秒
	for ($i = 1; $i -le 10; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/CustomRateLimit/sliding-window" -Method GET
			$content = $response.Content | ConvertFrom-Json
			Write-Host "[$i] ✅ 通过 - 当前计数: $($content.currentCount)" -ForegroundColor Green
		}
		catch {
			Write-Host "[$i] ❌ 被限流" -ForegroundColor Red
		}
		Start-Sleep -Seconds 2
	}
}

## ============================================
## 测试 3：令牌桶限流 - 突发流量
## ============================================
function Test-TokenBucket {
	Write-Host "`n=== 测试令牌桶限流（突发流量） ===" -ForegroundColor Cyan
	Write-Host "限制：容量10，每秒补充1个令牌`n"

	Write-Host "等待5秒（令牌积累）..."
	Start-Sleep -Seconds 5

	Write-Host "`n突发15个请求：`n"
	for ($i = 1; $i -le 15; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/CustomRateLimit/token-bucket" -Method GET
			$content = $response.Content | ConvertFrom-Json
			Write-Host "[$i] ✅ 通过 - 剩余令牌: $([math]::Round($content.availableTokens, 2))" -ForegroundColor Green
		}
		catch {
			$content = $_.ErrorDetails.Message | ConvertFrom-Json
			Write-Host "[$i] ❌ 被限流 - 剩余令牌: $([math]::Round($content.availableTokens, 2))" -ForegroundColor Red
		}
		Start-Sleep -Milliseconds 100
	}
}

## ============================================
## 测试 4：漏桶限流 - 平滑输出
## ============================================
function Test-LeakyBucket {
	Write-Host "`n=== 测试漏桶限流（平滑输出） ===" -ForegroundColor Cyan
	Write-Host "限制：容量10，每秒漏出1个请求`n"

	# 快速发送15个请求
	for ($i = 1; $i -le 15; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/CustomRateLimit/leaky-bucket" -Method GET
			$content = $response.Content | ConvertFrom-Json
			Write-Host "[$i] ✅ 进入桶 - 桶内: $($content.currentCount)" -ForegroundColor Green
		}
		catch {
			$content = $_.ErrorDetails.Message | ConvertFrom-Json
			Write-Host "[$i] ❌ 桶满，丢弃 - 桶内: $($content.currentCount)" -ForegroundColor Red
		}
		Start-Sleep -Milliseconds 100
	}
}

## ============================================
## 测试 5：并发控制 - SemaphoreSlim
## ============================================
function Test-Concurrency {
	Write-Host "`n=== 测试并发控制（SemaphoreSlim） ===" -ForegroundColor Cyan
	Write-Host "限制：最多10个并发数据库连接`n"

	# 创建50个SQL查询
	$sqls = @()
	for ($i = 1; $i -le 50; $i++) {
		$sqls += "SELECT * FROM users WHERE id = $i"
	}

	$body = $sqls | ConvertTo-Json
	Write-Host "发送50个查询请求（批量）..."

	try {
		$startTime = Get-Date
		$response = Invoke-WebRequest -Uri "$baseUrl/api/Concurrency/batch-query" -Method POST -Body $body -ContentType "application/json"
		$endTime = Get-Date
		$duration = ($endTime - $startTime).TotalSeconds

		$result = $response.Content | ConvertFrom-Json
		Write-Host "`n✅ 批量查询完成" -ForegroundColor Green
		Write-Host "   总查询数: $($result.totalQueries)"
		Write-Host "   耗时: $([math]::Round($result.duration, 2)) 秒"
		Write-Host "   实际耗时: $([math]::Round($duration, 2)) 秒"
		Write-Host "`n💡 说明：50个查询，每个耗时1秒，如果串行执行需要50秒"
		Write-Host "         但由于限制最多10个并发，实际耗时约5秒（50/10）"
	}
	catch {
		Write-Host "❌ 请求失败: $($_.Exception.Message)" -ForegroundColor Red
	}
}

## ============================================
## 测试 6：ASP.NET Core 内置限流
## ============================================
function Test-AspNetCoreRateLimit {
	Write-Host "`n=== 测试 ASP.NET Core 内置限流 ===" -ForegroundColor Cyan

	# 测试固定窗口
	Write-Host "`n【固定窗口】每10秒最多100次：`n"
	for ($i = 1; $i -le 5; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/RateLimitDemo/fixed" -Method GET
			Write-Host "[$i] ✅ 通过" -ForegroundColor Green
		}
		catch {
			Write-Host "[$i] ❌ 被限流" -ForegroundColor Red
		}
	}

	# 测试令牌桶
	Write-Host "`n【令牌桶】容量100，每秒补充10个：`n"
	for ($i = 1; $i -le 5; $i++) {
		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/RateLimitDemo/token" -Method GET
			Write-Host "[$i] ✅ 通过" -ForegroundColor Green
		}
		catch {
			Write-Host "[$i] ❌ 被限流" -ForegroundColor Red
		}
	}

	# 测试并发限流（需要等待）
	Write-Host "`n【并发限流】最多10个并发请求：`n"
	Write-Host "发送3个耗时2秒的请求（会立即返回）..."

	$jobs = @()
	for ($i = 1; $i -le 3; $i++) {
		$jobs += Start-Job -ScriptBlock {
			param($url, $index)
			try {
				$response = Invoke-WebRequest -Uri $url -Method GET
				return "[$index] ✅ 通过"
			}
			catch {
				return "[$index] ❌ 失败"
			}
		} -ArgumentList "$baseUrl/api/RateLimitDemo/concurrency", $i
	}

	# 等待所有任务完成
	$jobs | Wait-Job | Receive-Job | ForEach-Object {
		Write-Host $_
	}
	$jobs | Remove-Job
}

## ============================================
## 测试 7：查看限流器状态
## ============================================
function Test-Status {
	Write-Host "`n=== 查看所有限流器状态 ===" -ForegroundColor Cyan

	try {
		$response = Invoke-WebRequest -Uri "$baseUrl/api/CustomRateLimit/status" -Method GET
		$status = $response.Content | ConvertFrom-Json

		Write-Host "`n固定窗口限流器："
		Write-Host "  当前计数: $($status.fixedWindow.currentCount)"
		Write-Host "  重置时间: $([math]::Round($status.fixedWindow.resetIn, 2)) 秒"

		Write-Host "`n滑动窗口限流器："
		Write-Host "  当前计数: $($status.slidingWindow.currentCount)"

		Write-Host "`n令牌桶限流器："
		Write-Host "  可用令牌: $([math]::Round($status.tokenBucket.availableTokens, 2))"
		Write-Host "  下个令牌: $([math]::Round($status.tokenBucket.nextTokenIn, 2)) 秒"

		Write-Host "`n漏桶限流器："
		Write-Host "  桶内数量: $($status.leakyBucket.currentCount)"
		Write-Host "  可用槽位: $($status.leakyBucket.availableSlots)"
	}
	catch {
		Write-Host "❌ 获取状态失败: $($_.Exception.Message)" -ForegroundColor Red
	}
}

## ============================================
## 测试 8：登录接口保护（实战案例1）
## ============================================
function Test-LoginProtection {
	Write-Host "`n=== 测试登录接口保护（防暴力破解） ===" -ForegroundColor Cyan
	Write-Host "限制：每个 IP 每分钟最多 5 次登录尝试`n"

	# 先查看状态
	try {
		$statusResponse = Invoke-WebRequest -Uri "$baseUrl/api/Login/status" -Method GET
		$statusData = $statusResponse.Content | ConvertFrom-Json
		Write-Host "当前 IP: $($statusData.ip)"
		Write-Host "限流规则: $($statusData.message)`n"
	}
	catch {
		Write-Host "❌ 获取状态失败`n" -ForegroundColor Red
	}

	# 模拟暴力破解：快速尝试 8 次登录
	Write-Host "模拟暴力破解：快速尝试 8 次登录`n"
	for ($i = 1; $i -le 8; $i++) {
		$loginData = @{
			username = "admin"
			password = "wrong_password_$i"
		} | ConvertTo-Json

		try {
			$response = Invoke-WebRequest -Uri "$baseUrl/api/Login" -Method POST -Body $loginData -ContentType "application/json"
			Write-Host "[$i] ✅ 尝试成功（但密码错误）" -ForegroundColor Yellow
		}
		catch {
			$statusCode = $_.Exception.Response.StatusCode.value__
			if ($statusCode -eq 429) {
				Write-Host "[$i] 🛡️ 被限流保护！(HTTP 429)" -ForegroundColor Red
			}
			elseif ($statusCode -eq 401) {
				Write-Host "[$i] ✅ 允许尝试（但密码错误）" -ForegroundColor Yellow
			}
			else {
				Write-Host "[$i] ❌ 错误: HTTP $statusCode" -ForegroundColor Red
			}
		}
		Start-Sleep -Milliseconds 200
	}

	Write-Host "`n💡 说明：前 5 次失败的登录尝试会被允许"
	Write-Host "         第 6 次开始触发限流保护，返回 HTTP 429"
	Write-Host "         1 分钟后才能重新尝试"
}

## ============================================
## 测试 9：高成本接口保护（实战案例2）
## ============================================
function Test-ReportGeneration {
	Write-Host "`n=== 测试报告生成接口保护 ===" -ForegroundColor Cyan
	Write-Host "限制：最多 5 个并发报告生成任务`n"

	Write-Host "同时发起 3 个销售报告生成任务（每个耗时 5-10 秒）...`n"

	$jobs = @()
	for ($i = 1; $i -le 3; $i++) {
		$jobs += Start-Job -ScriptBlock {
			param($url, $index)
			$reportData = @{
				startDate = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
				endDate = (Get-Date).ToString("yyyy-MM-dd")
			} | ConvertTo-Json

			try {
				$startTime = Get-Date
				$response = Invoke-WebRequest -Uri $url -Method POST -Body $reportData -ContentType "application/json"
				$endTime = Get-Date
				$duration = ($endTime - $startTime).TotalSeconds
				$result = $response.Content | ConvertFrom-Json
				return "[$index] ✅ 报告生成成功 - 耗时: $([math]::Round($duration, 1))秒, 销售额: $($result.data.totalSales)"
			}
			catch {
				$statusCode = $_.Exception.Response.StatusCode.value__
				if ($statusCode -eq 429) {
					return "[$index] ⏳ 排队中... (HTTP 429)"
				}
				return "[$index] ❌ 失败: HTTP $statusCode"
			}
		} -ArgumentList "$baseUrl/api/Report/generate-sales", $i
	}

	# 等待所有任务完成
	$jobs | Wait-Job | Receive-Job | ForEach-Object {
		Write-Host $_
	}
	$jobs | Remove-Job

	Write-Host "`n💡 说明：报告生成是高成本操作，限制最多 5 个并发"
	Write-Host "         超出的请求会排队等待，不会直接拒绝"
}

## ============================================
## 测试 10：API 网关限流（实战案例3）
## ============================================
function Test-ApiGatewayRateLimit {
	Write-Host "`n=== 测试 API 网关限流（按订阅等级） ===" -ForegroundColor Cyan

	# 先获取测试 API Keys
	Write-Host "获取测试 API Keys...`n"
	try {
		$keysResponse = Invoke-WebRequest -Uri "$baseUrl/api/ApiGateway/test-keys" -Method GET
		$keys = $keysResponse.Content | ConvertFrom-Json
		Write-Host "免费版 Key: $($keys.freeKey) - 容量100, 每秒补充1个令牌"
		Write-Host "专业版 Key: $($keys.proKey) - 容量1000, 每秒补充10个令牌"
		Write-Host "企业版 Key: $($keys.enterpriseKey) - 容量10000, 每秒补充100个令牌`n"
	}
	catch {
		Write-Host "❌ 获取 API Keys 失败`n" -ForegroundColor Red
		return
	}

	# 测试免费版限流
	Write-Host "【测试免费版 API Key】快速发送 5 个请求：`n"
	$freeKey = "free-key-123"

	for ($i = 1; $i -le 5; $i++) {
		try {
			$headers = @{ "X-API-Key" = $freeKey }
			$response = Invoke-WebRequest -Uri "$baseUrl/api/ApiGateway/protected/data" -Method GET -Headers $headers
			$result = $response.Content | ConvertFrom-Json
			Write-Host "[$i] ✅ 免费版请求成功 - 订阅等级: $($result.subscription)" -ForegroundColor Green
		}
		catch {
			$statusCode = $_.Exception.Response.StatusCode.value__
			if ($statusCode -eq 429) {
				Write-Host "[$i] ❌ 免费版限流触发 (HTTP 429)" -ForegroundColor Red
			}
			else {
				Write-Host "[$i] ❌ 错误: HTTP $statusCode" -ForegroundColor Red
			}
		}
		Start-Sleep -Milliseconds 100
	}

	Write-Host "`n【测试专业版 API Key】快速发送 5 个请求：`n"
	$proKey = "pro-key-456"

	for ($i = 1; $i -le 5; $i++) {
		try {
			$headers = @{ "X-API-Key" = $proKey }
			$response = Invoke-WebRequest -Uri "$baseUrl/api/ApiGateway/protected/data" -Method GET -Headers $headers
			$result = $response.Content | ConvertFrom-Json
			Write-Host "[$i] ✅ 专业版请求成功 - 订阅等级: $($result.subscription)" -ForegroundColor Green
		}
		catch {
			Write-Host "[$i] ❌ 专业版限流触发" -ForegroundColor Red
		}
		Start-Sleep -Milliseconds 100
	}

	Write-Host "`n💡 说明：不同订阅等级有不同的令牌桶配置"
	Write-Host "         免费版容量小，专业版容量更大，企业版容量最大"
}

## ============================================
## 主菜单
## ============================================
function Show-Menu {
	Write-Host "`n╔════════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
	Write-Host "║                      限流与并发控制 - 测试菜单                              ║" -ForegroundColor Yellow
	Write-Host "╚════════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
	Write-Host "`n请选择要执行的测试：`n"
	Write-Host "【自定义限流算法】"
	Write-Host "1. 测试固定窗口限流（边界突刺）"
	Write-Host "2. 测试滑动窗口限流（平滑限流）"
	Write-Host "3. 测试令牌桶限流（突发流量）"
	Write-Host "4. 测试漏桶限流（平滑输出）"
	Write-Host ""
	Write-Host "【并发控制】"
	Write-Host "5. 测试并发控制（SemaphoreSlim）"
	Write-Host "6. 测试 ASP.NET Core 内置限流"
	Write-Host ""
	Write-Host "【实战案例】"
	Write-Host "8. 测试登录接口保护（防暴力破解）"
	Write-Host "9. 测试报告生成接口保护（高成本操作）"
	Write-Host "10. 测试 API 网关限流（按订阅等级）"
	Write-Host ""
	Write-Host "【其他】"
	Write-Host "7. 查看限流器状态"
	Write-Host "99. 运行所有测试"
	Write-Host "0. 退出`n"
}

## ============================================
## 主程序
## ============================================
while ($true) {
	Show-Menu
	$choice = Read-Host "请输入选项"

	switch ($choice) {
		"1" { Test-FixedWindow }
		"2" { Test-SlidingWindow }
		"3" { Test-TokenBucket }
		"4" { Test-LeakyBucket }
		"5" { Test-Concurrency }
		"6" { Test-AspNetCoreRateLimit }
		"7" { Test-Status }
		"8" { Test-LoginProtection }
		"9" { Test-ReportGeneration }
		"10" { Test-ApiGatewayRateLimit }
		"99" {
			Write-Host "`n🚀 开始运行所有测试...`n" -ForegroundColor Cyan
			Test-FixedWindow
			Test-SlidingWindow
			Test-TokenBucket
			Test-LeakyBucket
			Test-Concurrency
			Test-AspNetCoreRateLimit
			Test-Status
			Test-LoginProtection
			Test-ReportGeneration
			Test-ApiGatewayRateLimit
			Write-Host "`n✅ 所有测试完成！" -ForegroundColor Green
		}
		"0" {
			Write-Host "`n再见！" -ForegroundColor Green
			exit
		}
		default {
			Write-Host "`n❌ 无效的选项，请重新输入" -ForegroundColor Red
		}
	}

	Write-Host "`n按任意键继续..."
	$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
