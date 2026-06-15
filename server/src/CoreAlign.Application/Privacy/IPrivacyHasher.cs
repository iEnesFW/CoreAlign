namespace CoreAlign.Application.Privacy;

public interface IPrivacyHasher
{
    string Hash(Guid tenantId, string? value);
}
