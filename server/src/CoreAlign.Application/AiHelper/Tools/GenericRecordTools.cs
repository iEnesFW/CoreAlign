using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Domain.Exceptions;
using MediatR;

namespace CoreAlign.Application.AiHelper.Tools;

public sealed class GetRecordTool : IAiTool
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMediator _mediator;
    private readonly IAiReadableResourceRegistry _registry;

    public GetRecordTool(IMediator mediator, IAiReadableResourceRegistry registry)
    {
        _mediator = mediator;
        _registry = registry;
    }

    public string Name => "get_record";

    public string Description =>
        "Fetch the full detail of a single CoreAlign record by its type and id, to inspect or analyze it (totals, line items, " +
        "status, dates, etc.). Returns the real stored data for the current tenant. Available record types: " +
        string.Join(", ", _registry.All.Where(r => r.DetailQuery is not null).Select(r => r.Name)) + ".";

    public string ParametersJsonSchema =>
        """{"type":"object","properties":{"recordType":{"type":"string","description":"one of the available record types"},"id":{"type":"string","description":"the record's GUID identifier"}},"required":["recordType","id"]}""";

    public bool IsAvailable(AiToolContext context) => context.IsInternalStaff || context.IsPortalCustomer;

    public async Task<AiToolResult> ExecuteAsync(string argumentsJson, AiToolContext context, CancellationToken ct)
    {
        var recordType = AiToolArguments.ReadString(argumentsJson, "recordType");
        var resource = _registry.Resolve(recordType);
        var detailQuery = context.IsInternalStaff ? resource?.DetailQuery
            : context.IsPortalCustomer ? resource?.PortalDetailQuery
            : null;
        if (detailQuery is null)
        {
            var available = context.IsInternalStaff
                ? _registry.All.Where(r => r.DetailQuery is not null)
                : _registry.All.Where(r => r.PortalDetailQuery is not null);
            return AiToolResult.Error(
                $"Record type '{recordType}' is not available to you. Available: {string.Join(", ", available.Select(r => r.Name))}.");
        }
        if (!AiToolArguments.TryReadGuid(argumentsJson, "id", out var id))
        {
            return AiToolResult.Error("A valid 'id' (GUID) argument is required.");
        }

        try
        {
            var result = await _mediator.Send(detailQuery(id), ct).ConfigureAwait(false);
            if (result is null)
            {
                return AiToolResult.Error($"No {recordType} record was found with that id.");
            }
            return AiToolResult.Ok(JsonSerializer.Serialize(result, JsonOptions));
        }
        catch (DomainException ex)
        {
            return AiToolResult.Error(ex.Message);
        }
        catch (Exception)
        {
            return AiToolResult.Error("The record could not be retrieved.");
        }
    }
}

public sealed class SearchRecordsTool : IAiTool
{
    private const int DefaultTake = 10;
    private const int MaxTake = 25;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMediator _mediator;
    private readonly IAiReadableResourceRegistry _registry;

    public SearchRecordsTool(IMediator mediator, IAiReadableResourceRegistry registry)
    {
        _mediator = mediator;
        _registry = registry;
    }

    public string Name => "search_records";

    public string Description =>
        "Search/list CoreAlign records of a type by free text (e.g. a number, customer name) and return a short list so you can " +
        "find a record's id, then call get_record. Searchable types: " +
        string.Join(", ", _registry.All.Where(r => r.SearchQuery is not null).Select(r => r.Name)) + ".";

    public string ParametersJsonSchema =>
        """{"type":"object","properties":{"recordType":{"type":"string","description":"one of the searchable record types"},"query":{"type":"string","description":"free text to search for"},"take":{"type":"integer","description":"max results (default 10, max 25)"}},"required":["recordType","query"]}""";

    public bool IsAvailable(AiToolContext context) => context.IsInternalStaff;

    public async Task<AiToolResult> ExecuteAsync(string argumentsJson, AiToolContext context, CancellationToken ct)
    {
        var recordType = AiToolArguments.ReadString(argumentsJson, "recordType");
        var resource = _registry.Resolve(recordType);
        if (resource?.SearchQuery is null)
        {
            return AiToolResult.Error(
                $"Record type '{recordType}' is not searchable. Searchable: {string.Join(", ", _registry.All.Where(r => r.SearchQuery is not null).Select(r => r.Name))}.");
        }

        var search = AiToolArguments.ReadString(argumentsJson, "query");
        if (string.IsNullOrWhiteSpace(search))
        {
            return AiToolResult.Error("A non-empty 'query' argument is required.");
        }

        var take = DefaultTake;
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson!);
            if (doc.RootElement.TryGetProperty("take", out var t) && t.ValueKind == JsonValueKind.Number && t.TryGetInt32(out var tk))
            {
                take = Math.Clamp(tk, 1, MaxTake);
            }
        }
        catch (JsonException)
        {
        }

        try
        {
            var result = await _mediator.Send(resource.SearchQuery(search, take), ct).ConfigureAwait(false);
            return AiToolResult.Ok(JsonSerializer.Serialize(result, JsonOptions));
        }
        catch (DomainException ex)
        {
            return AiToolResult.Error(ex.Message);
        }
        catch (Exception)
        {
            return AiToolResult.Error("The search could not be completed.");
        }
    }
}
