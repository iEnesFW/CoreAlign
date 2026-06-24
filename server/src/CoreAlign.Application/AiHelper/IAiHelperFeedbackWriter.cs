using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAlign.Application.AiHelper;

public interface IAiHelperFeedbackWriter
{
    Task SubmitAsync(Guid answerId, bool isHelpful, string? reason, Guid? tenantId, CancellationToken cancellationToken = default);
}
