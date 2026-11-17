# Pre-presentation technical decisions

## Money precision policy

### Current behavior

`Money` accepts any non-negative .NET `decimal` amount and a three-letter ASCII currency code, which it normalizes to uppercase. It deliberately does not round, reject fractional scale, or validate the code against a supported-currency list. Values such as `1234.5678 EUR` therefore remain valid. The SQLite mappings preserve the decimal value and the migration-backed tests verify that this value survives catalog and warranty-snapshot round trips.

This is internally consistent but leaves presentation and input policy undefined. A UI cannot yet know how many decimal places to accept or display, and downstream printing must not silently format a higher-precision stored value as though it had already been rounded. Currency code shape validation also does not prove that a currency is actually supported.

### Smallest correct policy

The product owner should define the application's supported currencies and the allowed minor-unit precision for each one. If the first release supports only EUR retail prices, the likely rule is EUR with two fractional digits, but that is a business decision rather than a safe inference from the current code.

Once decided, enforce the rule in `Money` by rejecting excess precision at input rather than silently rounding. Before tightening validation, decide how existing values such as `1234.5678 EUR` are to be interpreted and migrated. Until those decisions are made, current behavior should remain unchanged and UI/printing code should preserve the stored amount.

## Warranty coverage policy

New product-sale and repair warranties require an administrator-managed warranty plan. The employee selects a plan for the matching workflow; the issuance service copies its duration and coverage description into the immutable warranty snapshot. Employees do not enter one-off coverage terms or wording.

Persisted `WarrantyPlanId` values remain nullable for historical compatibility, and reconstitution continues to support an older plan-less warranty if one exists. This does not create a second path for new issuance.

## Presentation error mapping

Application services keep Domain rule and validation exceptions intact instead of wrapping them in a generic exception. Presentation should map the existing exception types consistently at its operation boundary:

- `DomainValidationException` and `ApplicationValidationException`: field or form validation feedback.
- `DomainRuleViolationException`, `CategoryInUseException`, and `IssuerProfileConflictException`: actionable business-rule feedback without a retry loop.
- `EntityNotFoundException`: the selected record is no longer available; refresh the relevant list or navigate back.
- unexpected persistence or SQLite exceptions: log technical details and show a generic storage failure. Do not expose SQL or local paths.

The UI should use stable domain error codes when it needs rule-specific behavior. It should not parse exception message text, and the Application layer does not need a new generic result or exception hierarchy before a concrete presentation requirement demonstrates one.

## MAUI Blazor Hybrid DbContext lifetime

The existing Infrastructure registrations support the required lifetime without redesign. `InventoryDbContext`, each repository, and `EfUnitOfWork` are scoped. Resolving an Application service within one scope causes all of its persistence collaborators to share that scope's single context.

The MAUI host should keep the root service provider for the application lifetime but create an asynchronous scope for every user operation:

```csharp
public sealed class WarrantyUiOperations(IServiceScopeFactory scopeFactory)
{
    public async Task<WarrantyDto> IssueSaleAsync(
        IssueProductSaleWarrantyRequest request,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<WarrantyCommandService>();
        return await service.IssueProductSaleWarrantyAsync(request, cancellationToken);
    }
}
```

The operation boundary is the complete button action or page request, not the Razor component lifetime. Components may retain DTOs and form state, but must not retain scoped repositories, a unit of work, an Application service that captures those dependencies, or an `InventoryDbContext`. The operation must be awaited before its scope is disposed.

Each independent UI operation creates its own scope. Concurrent operations therefore receive independent contexts and change trackers. Work that must share a unit of work is resolved and completed inside one scope; parallel work must never use the same scope or context. Startup migration remains separate: `MigrateInventoryDatabaseAsync` uses a short-lived factory-created context before UI operations begin.

### Remaining MAUI integration risks

- SQLite permits concurrent readers but serializes writes. The host needs a small, user-visible policy for busy/locked write failures and should prevent accidental double submission; any retry policy must avoid issuing a second warranty after a successful commit.
- Startup migration must complete before UI navigation and must report storage-path, permission, corruption, and migration failures safely.
- Application services still need host-level DI registration. Their lifetimes should be scoped, and they should be resolved only from operation scopes.
- Cancellation, component disposal, and navigation must not abandon an unobserved operation or reuse its disposed services.
- The database path, backup/restore behavior, and platform data-protection expectations remain host decisions.
- The category archival pre-check expresses the required policy, but truly simultaneous catalog writes remain a general concurrency concern. The local single-instance topology reduces the exposure; a future multi-instance design would require database-enforced coordination or a transactional concurrency policy.

## CI

The repository now has one intentionally small GitHub Actions workflow in
`.github/workflows/ci.yml`. It runs on pushes and pull requests, uses a current
.NET 10 SDK, and contains one job:

```yaml
name: CI

on:
  push:
  pull_request:

permissions:
  contents: read

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore WarrantyIssueXE.sln
      - run: dotnet build WarrantyIssueXE.sln --configuration Release --no-restore
      - run: dotnet test WarrantyIssueXE.sln --configuration Release --no-build --no-restore
```

A single Ubuntu job is sufficient for the current three platform-neutral core projects and SQLite tests. Add MAUI workload/platform jobs only after the host exists; deployment, signing, publishing, service containers, and a build matrix would add complexity without validating the current implementation. Pin third-party action references to reviewed versions when the workflow is introduced.
