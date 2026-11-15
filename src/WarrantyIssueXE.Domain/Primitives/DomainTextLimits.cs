namespace WarrantyIssueXE.Domain.Primitives;

/// <summary>
/// Text limits that are part of the domain contract and are mirrored by persistence.
/// </summary>
public static class DomainTextLimits
{
    public const int Name = 200;
    public const int Description = 2000;
    public const int ContactAddress = 500;
    public const int ContactEmail = 320;
    public const int ContactPhone = 50;
    /// <summary>
    /// A VAT number is normalized as text only. Country-specific validation is
    /// intentionally outside the domain until a verified requirement exists.
    /// </summary>
    public const int VatNumber = 50;
    public const int CurrencyCode = 3;
    public const int Imei = 15;
    public const int WarrantyCertificateNumber = 100;
    public const int WarrantyScopeStatement = 2000;
    public const int ReplacedPartNumber = 100;
}
