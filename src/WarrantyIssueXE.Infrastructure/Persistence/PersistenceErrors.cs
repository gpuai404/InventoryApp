using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace WarrantyIssueXE.Infrastructure.Persistence;

internal static class PersistenceErrors
{
    public static InvalidOperationException MissingRecord(string entityName, Guid id) =>
        new($"The {entityName} record '{id:D}' no longer exists.");

    public static string? GetDuplicateBarcodeCode(DbUpdateException exception)
    {
        if (exception.InnerException is not SqliteException
            {
                SqliteErrorCode: 19,
                SqliteExtendedErrorCode: 2067
            })
        {
            return null;
        }

        return exception.Entries
            .Select(entry => entry.Entity)
            .OfType<ProductBarcodeRecord>()
            .Select(barcode => barcode.Code)
            .FirstOrDefault();
    }

    public static bool IsConcurrentIssuerProfileCreation(DbUpdateException exception) =>
        exception.Entries.Any(entry => entry.Entity is IssuerProfileRecord) &&
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 1555 or 2067
        };
}
