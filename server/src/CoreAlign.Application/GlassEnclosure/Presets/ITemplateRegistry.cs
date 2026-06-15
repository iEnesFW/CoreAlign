using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public interface ITemplateRegistry
{
    IEnclosurePreset Resolve(EnclosureSubtype subtype);
    IEnclosurePreset? Find(EnclosureSubtype subtype);
    IReadOnlyList<IEnclosurePreset> ListByCategory(EnclosureCategory category);
    IReadOnlyList<IEnclosurePreset> All { get; }
}
