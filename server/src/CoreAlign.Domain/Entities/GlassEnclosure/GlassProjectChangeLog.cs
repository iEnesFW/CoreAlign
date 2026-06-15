using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassProjectChangeLog : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public int SceneVersionFrom { get; private set; }
    public int SceneVersionTo { get; private set; }
    public GlassChangeLogKind ChangeKind { get; private set; }
    public string ChangeSummary { get; private set; } = string.Empty;
    public string? ChangeDiffJson { get; private set; }
    public Guid UserId { get; private set; }

    protected GlassProjectChangeLog() { }

    public GlassProjectChangeLog(
        Guid projectId,
        int sceneVersionFrom,
        int sceneVersionTo,
        GlassChangeLogKind changeKind,
        string changeSummary,
        Guid userId,
        string? changeDiffJson = null)
    {
        ProjectId = projectId;
        SceneVersionFrom = sceneVersionFrom;
        SceneVersionTo = sceneVersionTo;
        ChangeKind = changeKind;
        ChangeSummary = changeSummary;
        UserId = userId;
        ChangeDiffJson = changeDiffJson;
    }
}
