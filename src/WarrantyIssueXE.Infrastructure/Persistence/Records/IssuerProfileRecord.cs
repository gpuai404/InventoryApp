namespace WarrantyIssueXE.Infrastructure.Persistence;

internal sealed class IssuerProfileRecord
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? VatNumber { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
