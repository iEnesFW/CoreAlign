using CoreAlign.API.Controllers;
using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.Application.Tests.Invoices;

public class InvoicesControllerEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly InvoicesController _sut;

    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid InvoiceId = Guid.NewGuid();

    public InvoicesControllerEndpointTests()
    {
        _sut = new InvoicesController(_mediator);
    }

    [Fact]
    public async Task CreateStandalone_dispatches_command_and_returns_created_invoice()
    {
        var command = new CreateStandaloneInvoiceCommand(
            CustomerId: CustomerId,
            IssueDate: DateTime.UtcNow,
            Currency: "TRY",
            Lines: new List<StandaloneInvoiceLineInput>
            {
                new(null, "SVC", "Consulting", null, 1m, 100m, TaxRatePercent: 20m),
            },
            DueDays: 14);

        var dto = new InvoiceDto
        {
            Id = InvoiceId,
            InvoiceNumber = "INV-STD-0001",
            Type = InvoiceType.SalesInvoice,
            Status = InvoiceStatus.Issued,
            CustomerId = CustomerId,
            Currency = "TRY",
            Total = 120m,
        };
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.CreateStandaloneAsync(command, CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        var envelope = created.Value.Should().BeOfType<ApiResponse<InvoiceDto>>().Subject;
        envelope.IsSuccess.Should().BeTrue();
        envelope.Data!.Id.Should().Be(InvoiceId);
        envelope.Data.Status.Should().Be(InvoiceStatus.Issued);
        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueCreditNote_dispatches_command_built_from_route_and_body()
    {
        var lineId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();
        var request = new IssueCreditNoteRequest(
            new[] { new IssueCreditNoteLineInput(lineId, 2m) },
            Reason: "Returned goods",
            ReturnRequestId: returnRequestId);

        var dto = new InvoiceDto
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "CN-0001",
            Type = InvoiceType.CreditNote,
            Status = InvoiceStatus.Issued,
            OriginInvoiceId = InvoiceId,
            CustomerId = CustomerId,
            Currency = "TRY",
        };
        _mediator.Send(
                Arg.Is<IssueCreditNoteCommand>(c =>
                    c.InvoiceId == InvoiceId
                    && c.Reason == "Returned goods"
                    && c.ReturnRequestId == returnRequestId
                    && c.Lines.Count == 1
                    && c.Lines[0].InvoiceLineId == lineId
                    && c.Lines[0].Quantity == 2m),
                Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.IssueCreditNoteAsync(InvoiceId, request, CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        var envelope = created.Value.Should().BeOfType<ApiResponse<InvoiceDto>>().Subject;
        envelope.Data!.Type.Should().Be(InvoiceType.CreditNote);
        envelope.Data.OriginInvoiceId.Should().Be(InvoiceId);
        await _mediator.Received(1).Send(
            Arg.Is<IssueCreditNoteCommand>(c => c.InvoiceId == InvoiceId && c.ReturnRequestId == returnRequestId),
            Arg.Any<CancellationToken>());
    }
}
