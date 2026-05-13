using CoreAlign.Application.Customers.Commands;
using CoreAlign.Application.Customers.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IDocumentSequenceRepository _sequenceRepository = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateCustomerCommandHandler _sut;

    public CreateCustomerCommandHandlerTests()
    {
        _sequenceRepository
            .ConsumeAsync(DocumentSequenceType.CustomerCode, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("CUS-2026-00001");
        _sut = new CreateCustomerCommandHandler(_customerRepository, _sequenceRepository, _unitOfWork);
    }

    [Fact]
    public async Task Creates_customer_with_active_default_and_assigns_code()
    {
        var command = new CreateCustomerCommand(
            Name: "Acme Inc.",
            Email: "billing@acme.com",
            Phone: "+1 555 010 9999",
            TaxNumber: "TX-001",
            Notes: null);

        var result = await _sut.Handle(command, default);

        result.Should().NotBeNull();
        result.Name.Should().Be("Acme Inc.");
        result.IsActive.Should().BeTrue();
        result.Code.Should().Be("CUS-2026-00001");
        await _customerRepository.Received(1).AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Persists_optional_fields_when_provided()
    {
        Customer? captured = null;
        await _customerRepository.AddAsync(Arg.Do<Customer>(c => captured = c), Arg.Any<CancellationToken>());

        var command = new CreateCustomerCommand(
            Name: "Globex",
            Notes: "VIP client");

        await _sut.Handle(command, default);

        captured.Should().NotBeNull();
        captured!.Email.Should().BeNull();
        captured.Notes.Should().Be("VIP client");
    }

    [Fact]
    public async Task Skips_code_generation_when_explicit_code_passed()
    {
        Customer? captured = null;
        await _customerRepository.AddAsync(Arg.Do<Customer>(c => captured = c), Arg.Any<CancellationToken>());

        var command = new CreateCustomerCommand(
            Name: "Manual Coded",
            Code: "MANUAL-001");

        await _sut.Handle(command, default);

        captured!.Code.Should().Be("MANUAL-001");
        await _sequenceRepository.DidNotReceive().ConsumeAsync(
            Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
