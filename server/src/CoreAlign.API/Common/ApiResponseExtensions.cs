using CoreAlign.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Common;

public static class ApiResponseExtensions
{
    public static IActionResult ToOk<T>(this T data) =>
        new OkObjectResult(ApiResponse<T>.Success(data));

    public static IActionResult ToCreated<T>(this T data) =>
        new ObjectResult(ApiResponse<T>.Success(data)) { StatusCode = StatusCodes.Status201Created };

    public static IActionResult ToAccepted<T>(this T data) =>
        new ObjectResult(ApiResponse<T>.Success(data)) { StatusCode = StatusCodes.Status202Accepted };
}
