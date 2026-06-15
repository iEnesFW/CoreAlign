namespace CoreAlign.Application.Tags.DTOs;

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public bool IsActive { get; set; }
}
