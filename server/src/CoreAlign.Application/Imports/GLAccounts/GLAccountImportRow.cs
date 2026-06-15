namespace CoreAlign.Application.Imports.GLAccounts;

public class GLAccountImportRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Asset";
    public bool IsPostable { get; set; } = true;
    public string? ParentCode { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
}
