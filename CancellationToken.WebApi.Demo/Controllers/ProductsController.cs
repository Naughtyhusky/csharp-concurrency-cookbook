using CancellationToken.WebApi.Demo.Models;
using CancellationToken.WebApi.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace CancellationToken.WebApi.Demo.Controllers;

/// <summary>
/// 商品控制器
/// 
/// 演示场景：复杂查询 + 客户端取消
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService,ILogger<ProductsController> logger) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly ILogger<ProductsController> _logger = logger;

    /// <summary>
    /// 查询商品列表（支持复杂筛选）
    /// </summary>
    /// <remarks>
    /// 演示场景：
    /// 1. 用户在商品列表页进行筛选（类别、价格区间、关键词等）
    /// 2. 查询可能需要几秒钟（模拟复杂的数据库查询）
    /// 3. 用户可以点击"取消"按钮，或关闭浏览器
    /// 
    /// 测试方法：
    /// 1. 发送请求：GET /api/products?simulatedDelayMs=10000
    /// 2. 在10秒内取消请求（Postman 点击 Cancel，或 curl 按 Ctrl+C）
    /// 3. 观察日志：应该看到"商品查询已取消"
    /// 
    /// ASP.NET Core 自动取消的场景：
    /// - 客户端断开连接（用户关闭浏览器/标签页）
    /// - 请求超时（超过 Kestrel 配置的超时时间）
    /// - 应用关闭（Ctrl+C 或 docker stop）
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<Product>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 499)] // 499 = Client Closed Request
    public async Task<IActionResult> Query(
        [FromQuery] ProductQueryRequest request,
        System.Threading.CancellationToken cancellationToken) // 🔥 ASP.NET Core 自动注入
    {
        try
        {
            _logger.LogInformation("收到商品查询请求");

            // 🔥 关键点：将 cancellationToken 传递给服务层
            var result = await _productService.QueryProductsAsync(request, cancellationToken);

            return Ok(ApiResponse<PagedResponse<Product>>.SuccessResult(result));
        }
        catch (OperationCanceledException)
        {
            // 🔥 关键点：捕获取消异常，返回特定状态码
            _logger.LogInformation("商品查询被取消（客户端断开或手动取消）");

            // 返回 499 状态码（Client Closed Request）
            // 这是 Nginx 使用的非标准状态码，用于表示客户端主动断开
            return StatusCode(499, ApiResponse<object>.CancelledResult("查询已取消"));
        }
    }

    /// <summary>
    /// 根据 ID 获取商品
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Product>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(
        int id, 
        System.Threading.CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id, cancellationToken);

            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailResult("商品不存在", "PRODUCT_NOT_FOUND"));
            }

            return Ok(ApiResponse<Product>.SuccessResult(product));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("商品查询被取消");
            return StatusCode(499, ApiResponse<object>.CancelledResult());
        }
    }
}
