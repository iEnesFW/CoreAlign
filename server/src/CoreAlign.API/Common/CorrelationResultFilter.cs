using CoreAlign.API.Middleware;
using CoreAlign.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreAlign.API.Common;

public sealed class CorrelationResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult { Value: ITraceableResponse traceable }
            && string.IsNullOrEmpty(traceable.TraceId)
            && context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var cid)
            && cid is string correlationId)
        {
            traceable.TraceId = correlationId;
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
