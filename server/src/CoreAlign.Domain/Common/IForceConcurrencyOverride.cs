namespace CoreAlign.Domain.Common;

public interface IForceConcurrencyOverride
{
    bool ForceOverwrite { get; }
}
