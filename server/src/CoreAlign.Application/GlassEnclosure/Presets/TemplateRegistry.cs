using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.GlassEnclosure.Presets;

public class TemplateRegistry : ITemplateRegistry
{
    private readonly IReadOnlyDictionary<EnclosureSubtype, IEnclosurePreset> _bySubtype;
    private readonly IReadOnlyList<IEnclosurePreset> _all;

    public TemplateRegistry(IEnumerable<IEnclosurePreset> presets)
    {
        _all = presets.ToArray();
        _bySubtype = _all.ToDictionary(p => p.Subtype);
    }

    public IEnclosurePreset Resolve(EnclosureSubtype subtype) =>
        _bySubtype.TryGetValue(subtype, out var preset)
            ? preset
            : throw new EnclosurePresetNotFoundException(subtype);

    public IEnclosurePreset? Find(EnclosureSubtype subtype) =>
        _bySubtype.TryGetValue(subtype, out var preset) ? preset : null;

    public IReadOnlyList<IEnclosurePreset> ListByCategory(EnclosureCategory category) =>
        _all.Where(p => p.Category == category).ToArray();

    public IReadOnlyList<IEnclosurePreset> All => _all;
}
