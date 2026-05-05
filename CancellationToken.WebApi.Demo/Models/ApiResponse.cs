namespace CancellationToken.WebApi.Demo.Models;

/// <summary>
/// 统一 API 响应格式
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> FailResult(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    public static ApiResponse<T> CancelledResult(string message = "操作已取消")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = "OPERATION_CANCELLED"
        };
    }
}
