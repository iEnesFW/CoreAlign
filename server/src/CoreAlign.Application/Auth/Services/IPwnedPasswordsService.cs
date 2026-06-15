namespace CoreAlign.Application.Auth.Services;

public interface IPwnedPasswordsService
{
    Task<bool> IsPwnedAsync(string password, CancellationToken cancellationToken = default);
}
