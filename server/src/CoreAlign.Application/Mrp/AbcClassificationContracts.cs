using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Mrp;

public record ClassifyProductsAbcResultDto(
    int TotalEvaluated,
    int ClassA,
    int ClassB,
    int ClassC,
    int Unclassified,
    int PolicyDefaultsApplied,
    DateTime AsOfUtc);

public record ClassifyProductsAbcCommand(
    DateTime? AsOfDateUtc = null) : IRequest<ClassifyProductsAbcResultDto>, ITransactionalRequest;
