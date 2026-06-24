using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CoreAlign.Infrastructure.Persistence;

public sealed class FieldEncryptionModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => (context.GetType(),
            (context as CoreAlignDbContext)?.FieldEncryptionActive ?? false,
            designTime);
}
