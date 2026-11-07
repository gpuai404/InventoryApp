namespace InventoryApp.Infrastructure.Persistence;

internal static class PersistenceErrors
{
    public static InvalidOperationException MissingRecord(string entityName, Guid id) =>
        new($"The {entityName} record '{id:D}' no longer exists.");
}
