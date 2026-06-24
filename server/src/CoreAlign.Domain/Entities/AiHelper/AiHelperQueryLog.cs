using System;
using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.AiHelper;

public class AiHelperQueryLog : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid ConversationId { get; set; }

    public bool IsAnonymous { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string? RoutePath { get; set; }

    public string ChatModel { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    public decimal TopScore { get; set; }

    public string RetrievedJson { get; set; } = "[]";

    public string AnswerText { get; set; } = string.Empty;
}
