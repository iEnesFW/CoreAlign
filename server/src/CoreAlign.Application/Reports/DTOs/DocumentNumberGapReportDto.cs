namespace CoreAlign.Application.Reports.DTOs;

public class DocumentNumberGapReportDto
{
    public int? Year { get; set; }
    public int TypeCount { get; set; }
    public long TotalGap { get; set; }
    public IReadOnlyList<DocumentNumberGapRowDto> Rows { get; set; } = new List<DocumentNumberGapRowDto>();
}

public class DocumentNumberGapRowDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int Year { get; set; }
    public long Expected { get; set; }
    public long UsedCount { get; set; }
    public long MaxUsed { get; set; }
    public long GapCount { get; set; }
    public IReadOnlyList<long> MissingNumbers { get; set; } = new List<long>();
}
