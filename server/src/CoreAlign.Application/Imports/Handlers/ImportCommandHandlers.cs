using CoreAlign.Application.Imports.Commands;
using CoreAlign.Application.Imports.Customers;
using CoreAlign.Application.Imports.GLAccounts;
using CoreAlign.Application.Imports.Products;
using MediatR;

namespace CoreAlign.Application.Imports.Handlers;

public class PreviewCustomerImportHandler : IRequestHandler<PreviewCustomerImportCommand, BulkImportPreviewResult<CustomerImportRow>>
{
    private readonly CustomerBulkImporter _importer;

    public PreviewCustomerImportHandler(CustomerBulkImporter importer)
    {
        _importer = importer;
    }

    public Task<BulkImportPreviewResult<CustomerImportRow>> Handle(PreviewCustomerImportCommand request, CancellationToken cancellationToken)
        => _importer.PreviewAsync(request.FileStream, request.Format, cancellationToken);
}

public class PreviewProductImportHandler : IRequestHandler<PreviewProductImportCommand, BulkImportPreviewResult<ProductImportRow>>
{
    private readonly ProductBulkImporter _importer;

    public PreviewProductImportHandler(ProductBulkImporter importer)
    {
        _importer = importer;
    }

    public Task<BulkImportPreviewResult<ProductImportRow>> Handle(PreviewProductImportCommand request, CancellationToken cancellationToken)
        => _importer.PreviewAsync(request.FileStream, request.Format, cancellationToken);
}

public class PreviewGLAccountImportHandler : IRequestHandler<PreviewGLAccountImportCommand, BulkImportPreviewResult<GLAccountImportRow>>
{
    private readonly GLAccountBulkImporter _importer;

    public PreviewGLAccountImportHandler(GLAccountBulkImporter importer)
    {
        _importer = importer;
    }

    public Task<BulkImportPreviewResult<GLAccountImportRow>> Handle(PreviewGLAccountImportCommand request, CancellationToken cancellationToken)
        => _importer.PreviewAsync(request.FileStream, request.Format, cancellationToken);
}

public class CommitImportCommandHandler : IRequestHandler<CommitImportCommand, BulkImportCommitResult>
{
    private readonly CustomerBulkImporter _customers;
    private readonly ProductBulkImporter _products;
    private readonly GLAccountBulkImporter _glAccounts;

    public CommitImportCommandHandler(
        CustomerBulkImporter customers,
        ProductBulkImporter products,
        GLAccountBulkImporter glAccounts)
    {
        _customers = customers;
        _products = products;
        _glAccounts = glAccounts;
    }

    public Task<BulkImportCommitResult> Handle(CommitImportCommand request, CancellationToken cancellationToken)
    {
        return request.EntityKind switch
        {
            "customers" => _customers.CommitAsync(request.SessionId, request.SkipInvalidRows, cancellationToken),
            "products" => _products.CommitAsync(request.SessionId, request.SkipInvalidRows, cancellationToken),
            "gl-accounts" => _glAccounts.CommitAsync(request.SessionId, request.SkipInvalidRows, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown import entity kind '{request.EntityKind}'.")
        };
    }
}
