namespace CoreAlign.Application.Reports.DTOs;

public class DuplicateReportDto
{
    public string Entity { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int GroupCount { get; set; }
    public IReadOnlyList<DuplicateGroupDto> Groups { get; set; } = new List<DuplicateGroupDto>();
}

public class DuplicateGroupDto
{
    public string KeyValue { get; set; } = string.Empty;
    public int Count { get; set; }
    public IReadOnlyList<DuplicateMemberDto> Members { get; set; } = new List<DuplicateMemberDto>();
}

public class DuplicateMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
