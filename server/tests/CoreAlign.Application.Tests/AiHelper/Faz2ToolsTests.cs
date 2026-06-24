using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper.Tools;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities.Observability;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tests.AiHelper;

public class Faz2ToolsTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IErrorLogRepository _errors = Substitute.For<IErrorLogRepository>();
    private readonly IAiReadableResourceRegistry _registry = new AiReadableResourceRegistry();

    private static AiToolContext InternalStaff() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new[] { "TenantAdmin" }, "tr");

    private static AiToolContext PortalCustomer() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new[] { "Customer" }, "tr");

    private static AiToolContext PortalCustomerScoped() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new[] { "Customer" }, "tr", null, null, Guid.NewGuid());

    // ---- get_record (generic detail) ----

    [Fact]
    public void GetRecord_available_for_internal_staff_only()
    {
        var tool = new GetRecordTool(_mediator, _registry);
        tool.IsAvailable(InternalStaff()).Should().BeTrue();
        tool.IsAvailable(PortalCustomer()).Should().BeFalse();
    }

    [Fact]
    public void GetRecord_description_lists_many_resource_types()
    {
        var tool = new GetRecordTool(_mediator, _registry);
        tool.Description.Should().Contain("order").And.Contain("invoice").And.Contain("customer").And.Contain("journal_entry");
    }

    [Fact]
    public async Task GetRecord_unknown_type_returns_error_without_dispatch()
    {
        var tool = new GetRecordTool(_mediator, _registry);
        var result = await tool.ExecuteAsync("{\"recordType\":\"banana\",\"id\":\"" + Guid.NewGuid() + "\"}", InternalStaff(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        await _mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecord_bad_id_returns_error_without_dispatch()
    {
        var tool = new GetRecordTool(_mediator, _registry);
        var result = await tool.ExecuteAsync("{\"recordType\":\"order\"}", InternalStaff(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        await _mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecord_dispatches_detail_query_and_returns_ok()
    {
        _mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(new { id = "x", total = 5400 });
        var tool = new GetRecordTool(_mediator, _registry);

        var result = await tool.ExecuteAsync("{\"recordType\":\"order\",\"id\":\"" + Guid.NewGuid() + "\"}", InternalStaff(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _mediator.Received(1).Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecord_null_result_returns_not_found_error()
    {
        _mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns((object?)null);
        var tool = new GetRecordTool(_mediator, _registry);

        var result = await tool.ExecuteAsync("{\"recordType\":\"vendor\",\"id\":\"" + Guid.NewGuid() + "\"}", InternalStaff(), CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void GetRecord_available_for_scoped_portal_customer()
    {
        var tool = new GetRecordTool(_mediator, _registry);
        tool.IsAvailable(PortalCustomerScoped()).Should().BeTrue();
        tool.IsAvailable(PortalCustomer()).Should().BeFalse();
    }

    [Fact]
    public async Task GetRecord_portal_customer_routes_order_to_ownership_enforced_portal_query()
    {
        object? captured = null;
        _mediator.Send(Arg.Do<object>(o => captured = o), Arg.Any<CancellationToken>()).Returns(new { id = "x" });
        var tool = new GetRecordTool(_mediator, _registry);

        var result = await tool.ExecuteAsync(
            "{\"recordType\":\"order\",\"id\":\"" + Guid.NewGuid() + "\"}", PortalCustomerScoped(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        captured.Should().BeOfType<GetCustomerPortalOrderByIdQuery>(
            "portal customers MUST go through the ownership-enforced portal query, never the tenant-scoped one");
    }

    [Fact]
    public async Task GetRecord_internal_staff_routes_order_to_tenant_query()
    {
        object? captured = null;
        _mediator.Send(Arg.Do<object>(o => captured = o), Arg.Any<CancellationToken>()).Returns(new { id = "x" });
        var tool = new GetRecordTool(_mediator, _registry);

        await tool.ExecuteAsync(
            "{\"recordType\":\"order\",\"id\":\"" + Guid.NewGuid() + "\"}", InternalStaff(), CancellationToken.None);

        captured.Should().BeOfType<CoreAlign.Application.Orders.Queries.GetOrderByIdQuery>();
    }

    [Fact]
    public async Task GetRecord_portal_customer_cannot_read_internal_only_type()
    {
        var tool = new GetRecordTool(_mediator, _registry);
        var result = await tool.ExecuteAsync(
            "{\"recordType\":\"customer\",\"id\":\"" + Guid.NewGuid() + "\"}", PortalCustomerScoped(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        await _mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ---- search_records (generic search) ----

    [Fact]
    public async Task SearchRecords_non_searchable_type_returns_error()
    {
        var tool = new SearchRecordsTool(_mediator, _registry);
        var result = await tool.ExecuteAsync("{\"recordType\":\"gl_account\",\"query\":\"x\"}", InternalStaff(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        await _mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchRecords_dispatches_search_for_searchable_type()
    {
        _mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>()).Returns(new { total = 0, items = Array.Empty<object>() });
        var tool = new SearchRecordsTool(_mediator, _registry);

        var result = await tool.ExecuteAsync("{\"recordType\":\"order\",\"query\":\"5400\"}", InternalStaff(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _mediator.Received(1).Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ---- get_recent_errors ----

    [Fact]
    public void RecentErrors_tool_available_for_internal_staff_only()
    {
        var tool = new GetRecentErrorsTool(_errors);
        tool.IsAvailable(InternalStaff()).Should().BeTrue();
        tool.IsAvailable(PortalCustomer()).Should().BeFalse();
    }

    [Fact]
    public async Task RecentErrors_tool_scopes_query_to_caller_tenant()
    {
        _errors.QueryAsync(Arg.Any<ErrorLogQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ErrorLogEntry>)Array.Empty<ErrorLogEntry>(), 0));
        var context = InternalStaff();
        var tool = new GetRecentErrorsTool(_errors);

        var result = await tool.ExecuteAsync("{\"pathContains\":\"invoice\"}", context, CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _errors.Received(1).QueryAsync(
            Arg.Is<ErrorLogQuery>(q => q.TenantId == context.TenantId && q.PathContains == "invoice"),
            Arg.Any<CancellationToken>());
    }
}
