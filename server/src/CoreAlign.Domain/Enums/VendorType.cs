namespace CoreAlign.Domain.Enums;

public enum VendorType
{
    /// <summary>Şahıs şirketi / Bireysel — uses NationalId (TC Kimlik).</summary>
    Individual = 1,
    /// <summary>Tüzel kişi — Şirket — uses TaxNumber (VKN).</summary>
    Business = 2,
}
