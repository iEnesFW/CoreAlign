using CoreAlign.Application.Payroll.Employees;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Payroll;

public class CreateEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CreateEmployeeHandler _sut;

    public CreateEmployeeHandlerTests()
    {
        _sut = new CreateEmployeeHandler(_employees, _sequences, _uow);
    }

    private static CreateEmployeeCommand ValidCommand() => new(
        FirstName: "Ada",
        LastName: "Yilmaz",
        NationalId: "12345678901",
        HireDate: new DateOnly(2026, 1, 15),
        BaseSalaryGross: 50000m);

    [Fact]
    public async Task Create_assigns_PER_sequence_number()
    {
        _sequences.GetAsync(DocumentSequenceType.EmployeeNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.EmployeeNumber, "PER", 2026, 1, 5));
        Employee? captured = null;
        await _employees.AddAsync(Arg.Do<Employee>(e => captured = e), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(ValidCommand(), default);

        captured.Should().NotBeNull();
        captured!.EmployeeNumber.Should().Be("PER-2026-00001");
        result.EmployeeNumber.Should().Be("PER-2026-00001");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_seeds_sequence_when_missing()
    {
        _sequences.GetAsync(DocumentSequenceType.EmployeeNumber, Arg.Any<CancellationToken>())
            .Returns((DocumentSequence?)null);

        var result = await _sut.Handle(ValidCommand(), default);

        result.EmployeeNumber.Should().StartWith("PER-");
        await _sequences.Received(1).AddAsync(
            Arg.Is<DocumentSequence>(s => s.Type == DocumentSequenceType.EmployeeNumber && s.Prefix == "PER"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_masks_national_id_in_result()
    {
        _sequences.GetAsync(DocumentSequenceType.EmployeeNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.EmployeeNumber, "PER", 2026, 1, 5));

        var result = await _sut.Handle(ValidCommand(), default);

        result.NationalIdMasked.Should().Be("123******01");
        result.NationalIdMasked.Should().NotBe("12345678901");
    }

    [Fact]
    public async Task Create_rejects_duplicate_national_id()
    {
        _employees.NationalIdExistsAsync("12345678901", null, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _sut.Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<DuplicateEmployeeNationalIdException>();
        await _employees.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }
}

public class TerminateEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly TerminateEmployeeHandler _sut;

    public TerminateEmployeeHandlerTests()
    {
        _sut = new TerminateEmployeeHandler(_employees, _uow);
    }

    private static Employee NewEmployee() =>
        new("PER-2026-00001", "Ada", "Yilmaz", "12345678901", new DateOnly(2026, 1, 1), 50000m) { Id = Guid.NewGuid() };

    [Fact]
    public async Task Terminate_transitions_status_and_records_reason()
    {
        var employee = NewEmployee();
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _sut.Handle(
            new TerminateEmployeeCommand(employee.Id, new DateOnly(2026, 6, 30), "Resignation"), default);

        result.Status.Should().Be(EmploymentStatus.Terminated);
        result.TerminationDate.Should().Be(new DateOnly(2026, 6, 30));
        result.TerminationReason.Should().Be("Resignation");
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Terminate_twice_is_blocked_by_fsm_guard()
    {
        var employee = NewEmployee();
        employee.Terminate(new DateOnly(2026, 6, 30), "First");
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var act = () => _sut.Handle(
            new TerminateEmployeeCommand(employee.Id, new DateOnly(2026, 7, 31), "Second"), default);

        await act.Should().ThrowAsync<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public async Task Terminate_missing_employee_throws_not_found()
    {
        _employees.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var act = () => _sut.Handle(
            new TerminateEmployeeCommand(Guid.NewGuid(), new DateOnly(2026, 6, 30), null), default);

        await act.Should().ThrowAsync<EmployeeNotFoundException>();
    }
}

public class EmployeeDeductionHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static Employee NewEmployee() =>
        new("PER-2026-00001", "Ada", "Yilmaz", "12345678901", new DateOnly(2026, 1, 1), 50000m) { Id = Guid.NewGuid() };

    [Fact]
    public async Task Add_advance_deduction_carries_remaining_balance()
    {
        var employee = NewEmployee();
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = new AddDeductionHandler(_employees, _uow);

        var result = await sut.Handle(new AddDeductionCommand(
            employee.Id, DeductionType.Advance, new DateOnly(2026, 2, 1),
            Amount: 1000m, RemainingBalance: 6000m, Priority: 1), default);

        result.Deductions.Should().ContainSingle();
        var deduction = result.Deductions.Single();
        deduction.Amount.Should().Be(1000m);
        deduction.Percent.Should().BeNull();
        deduction.RemainingBalance.Should().Be(6000m);
    }
}

public class SalaryComponentHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private static Employee NewEmployee() =>
        new("PER-2026-00001", "Ada", "Yilmaz", "12345678901", new DateOnly(2026, 1, 1), 50000m) { Id = Guid.NewGuid() };

    [Fact]
    public async Task Deactivate_unknown_component_throws_not_found()
    {
        var employee = NewEmployee();
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = new DeactivateSalaryComponentHandler(_employees, _uow);

        var act = () => sut.Handle(
            new DeactivateSalaryComponentCommand(employee.Id, Guid.NewGuid(), new DateOnly(2026, 6, 1)), default);

        await act.Should().ThrowAsync<SalaryComponentNotFoundException>();
    }
}
