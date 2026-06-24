namespace CoreAlign.Application.Common;

public interface ITraceableResponse
{
    string? TraceId { get; set; }
}

public class ApiResponse<T> : ITraceableResponse
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; set; }
    public int StatusCode { get; set; }
    public string? TraceId { get; set; }

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

    public static ApiResponse<T> Failure(List<string> errors, int statusCode, string traceId)
    {
        return new ApiResponse<T> { IsSuccess = false, Errors = errors, StatusCode = statusCode, TraceId = traceId };
    }
}
