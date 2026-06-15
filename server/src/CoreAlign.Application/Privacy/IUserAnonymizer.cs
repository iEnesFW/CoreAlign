using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Privacy;

public interface IUserAnonymizer
{
    Task AnonymizeAsync(User user, DateTime nowUtc, CancellationToken cancellationToken = default);
}
