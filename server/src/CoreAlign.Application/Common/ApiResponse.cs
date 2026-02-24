namespace CoreAlign.Application.Common;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }

    public static ApiResponse<T> Success(T data, int statusCode = 200)
    {
        return new ApiResponse<T> { IsSuccess = true, Data = data, StatusCode = statusCode };
    }

    public static ApiResponse<T> Failure(string error, int statusCode = 400)
    {
        return new ApiResponse<T> { IsSuccess = false, Errors = new List<string> { error }, StatusCode = statusCode };
    }

    public static ApiResponse<T> Failure(List<string> errors, int statusCode = 400)
    {
        return new ApiResponse<T> { IsSuccess = false, Errors = errors, StatusCode = statusCode };
    }
}
