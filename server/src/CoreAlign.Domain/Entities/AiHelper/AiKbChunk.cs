using System;
using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.AiHelper;

public class AiKbChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int Ordinal { get; set; }
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string Locale { get; set; } = string.Empty;
    public AiKbScope Scope { get; set; }
    public Guid? TenantId { get; set; }
    public string? RequiredRole { get; set; }
    public int TokenCount { get; set; }

    public AiKbDocument? Document { get; set; }
}
