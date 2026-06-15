using CoreAlign.Application.Common;
using CoreAlign.Application.Imports.Customers;
using CoreAlign.Application.Imports.GLAccounts;
using CoreAlign.Application.Imports.Products;
using MediatR;

namespace CoreAlign.Application.Imports.Commands;

public record PreviewCustomerImportCommand(Stream FileStream, BulkImportFileFormat Format)
    : IRequest<BulkImportPreviewResult<CustomerImportRow>>;

public record PreviewProductImportCommand(Stream FileStream, BulkImportFileFormat Format)
    : IRequest<BulkImportPreviewResult<ProductImportRow>>;

public record PreviewGLAccountImportCommand(Stream FileStream, BulkImportFileFormat Format)
    : IRequest<BulkImportPreviewResult<GLAccountImportRow>>;

public record CommitImportCommand(string EntityKind, Guid SessionId, bool SkipInvalidRows)
    : IRequest<BulkImportCommitResult>, ITransactionalRequest;
