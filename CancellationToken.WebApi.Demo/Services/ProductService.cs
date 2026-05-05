using CancellationToken.WebApi.Demo.Models;

namespace CancellationToken.WebApi.Demo.Services;

/// <summary>
/// 商品服务接口
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 查询商品列表（支持复杂筛选）
    /// </summary>
    Task<PagedResponse<Product>> QueryProductsAsync(
        ProductQueryRequest request, 
        System.Threading.CancellationToken cancellationToken);

    /// <summary>
    /// 根据 ID 获取商品
    /// </summary>
    Task<Product?> GetProductByIdAsync(
        int id, 
        System.Threading.CancellationToken cancellationToken);
}

/// <summary>
/// 商品服务实现
/// 
/// 演示场景：
/// 1. 用户在商品列表页进行复杂筛选（多条件查询）
/// 2. 查询可能需要几秒钟（连表、全文搜索等）
/// 3. 用户点击"取消"按钮，应该立即停止查询
/// 
/// 关键点：
/// - ASP.NET Core 会在客户端断开时自动取消 CancellationToken
/// - 服务层必须将 Token 传递给所有异步操作
/// - 使用 Task.Delay 模拟长时间数据库查询
/// </summary>
public class ProductService(ILogger<ProductService> logger) : IProductService
{
    private readonly ILogger<ProductService> _logger = logger;
    private static readonly List<Product> _products = GenerateSampleProducts();

    public async Task<PagedResponse<Product>> QueryProductsAsync(
        ProductQueryRequest request, 
        System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "开始查询商品：Category={Category}, SearchKeyword={SearchKeyword}, Page={Page}", 
            request.Category, 
            request.SearchKeyword, 
            request.Page);

        try
        {
            // 🔥 关键点1：在开始长时间操作前检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 模拟复杂的数据库查询（多表连接、全文搜索等）
            _logger.LogInformation("模拟复杂查询，延迟 {Delay}ms...", request.SimulatedDelayMs);

            // 🔥 关键点2：长时间操作必须传递 CancellationToken
            await Task.Delay(request.SimulatedDelayMs, cancellationToken);

            // 🔥 关键点3：在处理数据前再次检查取消
            cancellationToken.ThrowIfCancellationRequested();

            // 筛选数据
            var query = _products.AsQueryable();

            if (!string.IsNullOrEmpty(request.Category))
            {
                query = query.Where(p => p.Category == request.Category);
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchKeyword))
            {
                query = query.Where(p => 
                    p.Name.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = query.Count();
            var data = query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            _logger.LogInformation("查询完成，返回 {Count} 条记录", data.Count);

            return new PagedResponse<Product>
            {
                Data = data,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        catch (OperationCanceledException)
        {
            // 🔥 关键点4：记录取消日志（区分正常流程和异常）
            _logger.LogInformation("商品查询已取消");
            throw; // 重新抛出，让控制器处理
        }
    }

    public async Task<Product?> GetProductByIdAsync(
        int id, 
        System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogInformation("查询商品 ID={Id}", id);

        // 模拟数据库查询
        await Task.Delay(100, cancellationToken);

        var product = _products.FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            _logger.LogWarning("商品不存在 ID={Id}", id);
        }

        return product;
    }

    /// <summary>
    /// 生成示例商品数据
    /// </summary>
    private static List<Product> GenerateSampleProducts()
    {
        var categories = new[] { "Electronics", "Clothing", "Books", "Food", "Home" };
        var products = new List<Product>();

        for (int i = 1; i <= 100; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Category = categories[i % categories.Length],
                Price = (decimal)(10 + i * 9.99),
                Stock = 100 + i,
                Description = $"This is a description for Product {i}",
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }

        return products;
    }
}
