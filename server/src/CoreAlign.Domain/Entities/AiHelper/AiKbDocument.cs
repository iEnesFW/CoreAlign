using System;
using System.Collections.Generic;
using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.AiHelper;

public class AiKbDocument : BaseEntity
{
    public AiKbSourceType SourceType { get; set; }
    public string SourceRef { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public AiKbScope Scope { get; set; }
    public Guid? TenantId { get; set; }
    public string? RequiredRole { get; set; }
    public string ContentHash { get; set; } = string.Empty;

    public ICollection<AiKbChunk> Chunks { get; set; } = new List<AiKbChunk>();
}
