using CoreAlign.Application.Tags.DTOs;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tags.Mapping;

public static class TagMapper
{
    public static TagDto ToDto(Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        ColorHex = tag.ColorHex,
        IsActive = tag.IsActive
    };
}
