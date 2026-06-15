namespace CoreAlign.Domain.Enums;

public enum ProviderCategory
{
    EFatura = 0,
    Payment = 1,
    LaserMeter = 2,
    LabelPrinter = 3,
    CncExport = 4,
    CadImport = 5,
    Freight = 6,
    BankReconciliation = 7,
    Calendar = 8,
    Export = 9,
    Sms = 10,
    WhatsApp = 11,
    Email = 12,
    Push = 13
}

public enum ProviderHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
    NotConfigured = 4
}
