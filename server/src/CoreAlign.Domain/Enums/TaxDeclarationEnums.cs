namespace CoreAlign.Domain.Enums;

public enum TaxDeclarationType
{
    Kdv1 = 1,
    Kdv2 = 2,
    BabsBeyani = 3
}

public enum TaxDeclarationStatus
{
    Draft = 0,
    Generated = 1,
    Submitted = 2,
    Accepted = 3,
    Rejected = 4
}
