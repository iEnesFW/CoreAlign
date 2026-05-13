using CoreAlign.Application.Common;
using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

public class GetCustomerSummaryQueryHandler : IRequestHandler<GetCustomerSummaryQuery, CustomerSummaryDto>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerSummaryQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerSummaryDto> Handle(GetCustomerSummaryQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var (orderCount, orderTotal) = await _customerRepository.GetOrderTotalsAsync(customer.Id, cancellationToken);
        var (invoiceCount, invoiced, paid, outstanding, currency) = await _customerRepository.GetInvoiceTotalsAsync(customer.Id, cancellationToken);

        return new CustomerSummaryDto
        {
            CustomerId = customer.Id,
            OrderCount = orderCount,
            TotalOrderAmount = orderTotal,
            InvoiceCount = invoiceCount,
            TotalInvoiced = invoiced,
            TotalPaid = paid,
            Outstanding = outstanding,
            Currency = currency
        };
    }
}
