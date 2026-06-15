using CoreAlign.Application.Invoices.Commands;
using CoreAlign.Application.Invoices.Validators;
using CoreAlign.Application.Returns.Commands;
using CoreAlign.Application.Returns.Validators;
using CoreAlign.Domain.Enums;
using FluentValidation.TestHelper;

namespace CoreAlign.Application.Tests.Returns;

public class ReturnRequestValidatorTests
{
    [Fact]
    public void CreateReturnRequest_requires_order_and_at_least_one_line()
    {
        var v = new CreateReturnRequestCommandValidator();

        var result = v.TestValidate(new CreateReturnRequestCommand(
            OrderId: Guid.Empty,
            Reason: ReturnReasonCode.Other,
            ReasonText: null,
            Lines: Array.Empty<CreateReturnRequestLineInput>()));

        result.ShouldHaveValidationErrorFor(x => x.OrderId);
        result.ShouldHaveValidationErrorFor(x => x.Lines);
    }

    [Fact]
    public void CreateReturnRequest_line_quantity_must_be_positive()
    {
        var v = new CreateReturnRequestCommandValidator();

        var result = v.TestValidate(new CreateReturnRequestCommand(
            OrderId: Guid.NewGuid(),
            Reason: ReturnReasonCode.Defective,
            ReasonText: null,
            Lines: new[] { new CreateReturnRequestLineInput(Guid.NewGuid(), 0m) }));

        result.ShouldHaveValidationErrorFor("Lines[0].QuantityReturned");
    }

    [Fact]
    public void Receive_requires_warehouse()
    {
        var v = new ReceiveReturnedItemsCommandValidator();

        var result = v.TestValidate(new ReceiveReturnedItemsCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.WarehouseId);
    }

    [Fact]
    public void IssueCreditNote_requires_lines_with_positive_quantity()
    {
        var v = new IssueCreditNoteCommandValidator();

        var result = v.TestValidate(new IssueCreditNoteCommand(
            Guid.NewGuid(),
            new[] { new IssueCreditNoteLineInput(Guid.NewGuid(), 0m) }));

        result.ShouldHaveValidationErrorFor("Lines[0].Quantity");
    }

    [Fact]
    public void IssueCreditNote_passes_for_well_formed_input()
    {
        var v = new IssueCreditNoteCommandValidator();

        var result = v.TestValidate(new IssueCreditNoteCommand(
            Guid.NewGuid(),
            new[] { new IssueCreditNoteLineInput(Guid.NewGuid(), 2m) },
            Reason: "Customer return"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
