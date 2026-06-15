using CoreAlign.Application.B2B;
using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.GlassEnclosure.Bom;

public sealed class BomLineBackfillHandler : IRequestHandler<BomLineBackfillCommand, BomLineBackfillResult>
{
    private readonly IGlassProjectBOMLineRepository _bomLines;
    private readonly IGlassTypeRepository _glassTypes;
    private readonly IHardwareItemRepository _hardware;
    private readonly IProfileItemRepository _profiles;
    private readonly ICatalogProductLinker _linker;
    private readonly ICurrentUserAccessor _currentUser;

    public BomLineBackfillHandler(
        IGlassProjectBOMLineRepository bomLines,
        IGlassTypeRepository glassTypes,
        IHardwareItemRepository hardware,
        IProfileItemRepository profiles,
        ICatalogProductLinker linker,
        ICurrentUserAccessor currentUser)
    {
        _bomLines = bomLines;
        _glassTypes = glassTypes;
        _hardware = hardware;
        _profiles = profiles;
        _linker = linker;
        _currentUser = currentUser;
    }

    public async Task<BomLineBackfillResult> Handle(BomLineBackfillCommand request, CancellationToken cancellationToken)
    {
        _ = _currentUser.UserIdOrThrow();

        var unlinked = await _bomLines.ListUnlinkedAsync(cancellationToken);
        var alreadyLinked = 0;
        var linked = 0;
        var couldNotLink = 0;
        var issues = new List<BomLineBackfillIssue>();

        foreach (var line in unlinked)
        {
            if (line.ProductId.HasValue)
            {
                alreadyLinked++;
                continue;
            }

            if (!line.RefId.HasValue)
            {
                couldNotLink++;
                issues.Add(new BomLineBackfillIssue(line.Id, line.Kind.ToString(), null, "bom.refid-missing"));
                continue;
            }

            var (catalogItem, kind, reasonKey) = await ResolveCatalogItemAsync(line.Kind, line.RefId.Value, cancellationToken);
            if (catalogItem is null)
            {
                couldNotLink++;
                issues.Add(new BomLineBackfillIssue(line.Id, line.Kind.ToString(), line.RefId, reasonKey));
                continue;
            }

            var linkage = await _linker.EnsureLinkedAsync(catalogItem, kind, cancellationToken);
            if (linkage.ProductId == Guid.Empty)
            {
                couldNotLink++;
                issues.Add(new BomLineBackfillIssue(line.Id, line.Kind.ToString(), line.RefId, "catalog.link-failed"));
                continue;
            }

            line.UpdateProductLink(linkage.ProductId);
            _bomLines.Update(line);
            linked++;
        }

        return new BomLineBackfillResult(
            TotalScanned: unlinked.Count,
            AlreadyLinked: alreadyLinked,
            Linked: linked,
            CouldNotLink: couldNotLink,
            Issues: issues);
    }

    private async Task<(ICatalogLinkable? Item, CatalogItemKind Kind, string ReasonKey)> ResolveCatalogItemAsync(
        GlassBOMLineKind lineKind,
        Guid refId,
        CancellationToken cancellationToken)
    {
        switch (lineKind)
        {
            case GlassBOMLineKind.GlassPiece:
            {
                var glass = await _glassTypes.GetByIdAsync(refId, cancellationToken);
                return glass is null
                    ? (null, CatalogItemKind.Glass, "catalog.glass-not-found")
                    : (glass, CatalogItemKind.Glass, string.Empty);
            }
            case GlassBOMLineKind.HardwarePiece:
            {
                var hardware = await _hardware.GetByIdAsync(refId, cancellationToken);
                return hardware is null
                    ? (null, CatalogItemKind.Hardware, "catalog.hardware-not-found")
                    : (hardware, CatalogItemKind.Hardware, string.Empty);
            }
            case GlassBOMLineKind.ProfileCut:
            {
                var profile = await _profiles.GetByIdAsync(refId, cancellationToken);
                return profile is null
                    ? (null, CatalogItemKind.Profile, "catalog.profile-not-found")
                    : (profile, CatalogItemKind.Profile, string.Empty);
            }
            default:
                return (null, CatalogItemKind.Glass, "bom.kind-not-catalog-bound");
        }
    }
}
