using System;
using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.AiHelper;

public class AiHelperFeedback : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid AnswerId { get; set; }

    public bool IsHelpful { get; set; }

    public string? Reason { get; set; }
}
