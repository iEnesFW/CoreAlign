using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Exceptions;

public class EnclosurePresetNotFoundException : DomainException
{
    public EnclosureSubtype Subtype { get; }

    public EnclosurePresetNotFoundException(EnclosureSubtype subtype)
        : base("GlassEnclosure.Preset.NotFound")
    {
        Subtype = subtype;
    }
}
