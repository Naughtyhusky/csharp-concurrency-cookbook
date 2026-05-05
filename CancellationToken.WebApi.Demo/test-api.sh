# CancellationToken Web API 测试命令（curl）

echo "=========================================="
echo "CancellationToken Web API 测试命令"
echo "=========================================="
echo ""

echo "============ 测试1：商品查询（正常完成） ============"
echo ""
echo "命令："
echo "curl \"http://localhost:5000/api/products?simulatedDelayMs=1000\""
echo ""

curl "http://localhost:5000/api/products?simulatedDelayMs=1000"

echo ""
echo ""
echo "============ 测试2：商品查询（客户端取消） ============"
echo ""
echo "提示：发送请求后，在 10 秒内按 Ctrl+C 取消"
echo ""
echo "命令："
echo "curl \"http://localhost:5000/api/products?simulatedDelayMs=10000\""
echo ""

curl "http://localhost:5000/api/products?simulatedDelayMs=10000"

echo ""
echo ""
echo "============ 测试3：订单创建（正常完成） ============"
echo ""
echo "命令："
echo 'curl -X POST http://localhost:5000/api/orders \'
echo '  -H "Content-Type: application/json" \'
echo '  -d '"'"'{"items":[{"productId":1,"productName":"Product 1","quantity":2,"price":99.99}],"simulatedPaymentDelayMs":3000}'"'"

echo ""

curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"items":[{"productId":1,"productName":"Product 1","quantity":2,"price":99.99}],"simulatedPaymentDelayMs":3000}'

echo ""
echo ""
echo "============ 测试4：订单创建（超时） ============"
echo ""
echo "提示：这个测试会等待 30 秒超时"
echo ""
echo "命令："
echo 'curl -X POST http://localhost:5000/api/orders \'
echo '  -H "Content-Type: application/json" \'
echo '  -d '"'"'{"items":[{"productId":1,"productName":"Product 1","quantity":2,"price":99.99}],"simulatedPaymentDelayMs":35000}'"'"

echo ""

curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"items":[{"productId":1,"productName":"Product 1","quantity":2,"price":99.99}],"simulatedPaymentDelayMs":35000}'

echo ""
echo ""
echo "============ 测试5：报表生成（正常完成） ============"
echo ""
echo "命令："
echo 'curl -X POST http://localhost:5000/api/reports/generate \'
echo '  -H "Content-Type: application/json" \'
echo '  -d '"'"'{"reportType":"Sales","startDate":"2024-01-01","endDate":"2024-12-31","simulatedDelayMs":3000}'"'"

echo ""

curl -X POST http://localhost:5000/api/reports/generate \
  -H "Content-Type: application/json" \
  -d '{"reportType":"Sales","startDate":"2024-01-01","endDate":"2024-12-31","simulatedDelayMs":3000}'

echo ""
echo ""
echo "=========================================="
echo "所有测试完成！"
echo "=========================================="
