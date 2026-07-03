using CoreAlign.Application.Customers.Notes;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Customers;

public class AddCustomerNoteCommandValidatorTests
{
    private readonly AddCustomerNoteCommandValidator _validator = new();

    [Fact]
    public void Valid_note_passes()
    {
        _validator.Validate(new AddCustomerNoteCommand(Guid.NewGuid(), "Aradık, dönüş bekliyor.", Guid.NewGuid()))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_body_fails()
    {
        _validator.Validate(new AddCustomerNoteCommand(Guid.NewGuid(), "", Guid.NewGuid()))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Body_over_4000_chars_fails()
    {
        _validator.Validate(new AddCustomerNoteCommand(Guid.NewGuid(), new string('x', 4001), Guid.NewGuid()))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_customer_id_fails()
    {
        _validator.Validate(new AddCustomerNoteCommand(Guid.Empty, "not", Guid.NewGuid()))
            .IsValid.Should().BeFalse();
    }
}

public class AddCustomerNoteHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerNoteRepository _notes = Substitute.For<ICustomerNoteRepository>();
    private readonly AddCustomerNoteHandler _sut;

    public AddCustomerNoteHandlerTests()
    {
        _sut = new AddCustomerNoteHandler(_customers, _notes);
    }

    [Fact]
    public async Task Adding_note_to_existing_customer_persists_trimmed_body()
    {
        var customer = new Customer("Acme") { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var dto = await _sut.Handle(
            new AddCustomerNoteCommand(customer.Id, "  Teslimat adresi değişti  ", userId),
            CancellationToken.None);

        dto.Body.Should().Be("Teslimat adresi değişti");
        dto.CreatedByUserId.Should().Be(userId);
        await _notes.Received(1).AddAsync(
            Arg.Is<CustomerNote>(n => n.CustomerId == customer.Id && n.Body == "Teslimat adresi değişti"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Adding_note_to_missing_customer_throws_not_found()
    {
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var act = () => _sut.Handle(
            new AddCustomerNoteCommand(Guid.NewGuid(), "not", Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<CustomerNotFoundException>();
        await _notes.DidNotReceive().AddAsync(Arg.Any<CustomerNote>(), Arg.Any<CancellationToken>());
    }
}

public class GetCustomerNotesHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly ICustomerNoteRepository _notes = Substitute.For<ICustomerNoteRepository>();
    private readonly GetCustomerNotesHandler _sut;

    public GetCustomerNotesHandlerTests()
    {
        _sut = new GetCustomerNotesHandler(_customers, _notes);
    }

    [Fact]
    public async Task Listing_notes_returns_dtos_for_existing_customer()
    {
        var customer = new Customer("Acme") { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() };
        _customers.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);
        _notes.GetLatestByCustomerAsync(customer.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<CustomerNote> { new(customer.Id, Guid.NewGuid(), "ilk not") });

        var result = await _sut.Handle(new GetCustomerNotesQuery(customer.Id), CancellationToken.None);

        result.Should().ContainSingle(n => n.Body == "ilk not");
    }

    [Fact]
    public async Task Listing_notes_for_missing_customer_throws_not_found()
    {
        _customers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var act = () => _sut.Handle(new GetCustomerNotesQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<CustomerNotFoundException>();
    }
}
