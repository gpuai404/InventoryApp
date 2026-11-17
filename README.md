# WarrantyIssueXE

WarrantyIssueXE is the core of a single-company warranty-certificate application for a retailer or repair business. It maintains a product catalog and issues immutable historical warranties for exactly two workflows: product sales and repairs.

The intended client is a .NET 10 MAUI Blazor Hybrid application using a local SQLite database. This repository does **not** yet contain the MAUI host, Razor components, a complete user interface, or certificate rendering and printing.

## Current structure

- `src/WarrantyIssueXE.Domain` — entities, strongly typed identifiers, value objects, snapshots, warranty details, and business invariants.
- `src/WarrantyIssueXE.Application` — explicit catalog, issuer-profile, warranty-plan, and warranty issuance/query services plus persistence, clock, and certificate-number ports.
- `src/WarrantyIssueXE.Infrastructure` — EF Core SQLite records and mappings, repositories, unit of work, dependency injection, migration support, and technical service implementations.
- `tests/WarrantyIssueXE.Domain.Tests` — domain behavior and invariant tests.
- `tests/WarrantyIssueXE.Application.Tests` — application-service tests using focused fakes.
- `tests/WarrantyIssueXE.Infrastructure.Tests` — real SQLite persistence, migration, dependency-injection, and application-to-infrastructure integration tests.

Dependencies point inward:

```text
Infrastructure -> Application -> Domain
                         tests -> implemented layer(s)
```

Domain has no EF Core, SQLite, MAUI, or presentation dependency. Application coordinates use cases through focused ports. Infrastructure implements those ports without exposing persistence records to Domain or Application.

## Technical stack

- .NET 10 and C# with nullable reference types enabled
- Entity Framework Core 10 with the SQLite provider
- SQLite with migration-based schema creation
- xUnit and Microsoft.NET.Test.Sdk
- Central package version management through `Directory.Packages.props`

## Implemented behavior

- Category and product catalog commands and queries, including soft archival.
- Category archival is rejected while active products reference the category. Products can be reassigned to another active category first.
- One current issuer profile, copied into each issued warranty as a historical snapshot.
- Product-sale and repair warranty issuance from administrator-managed, workflow-specific warranty plans.
- Optional IMEI, customer snapshots, product/category/price snapshots, issuer snapshots, and repair replaced-part snapshots.
- Immutable issued warranties that remain readable after catalog or issuer changes.
- One EF Core `InitialCreate` migration and tested SQLite repository implementations.

Product variants, barcode lookup, generic serial-number lookup, and warranty voiding are not part of the current model.

## Build and test

Install a .NET 10 SDK, then run from the repository root:

```bash
dotnet restore WarrantyIssueXE.sln
dotnet build WarrantyIssueXE.sln --no-restore
dotnet test WarrantyIssueXE.sln --no-build --no-restore
```

The test suite uses temporary SQLite databases and does not require a separately installed database server.

## SQLite migrations

The current migration is in `src/WarrantyIssueXE.Infrastructure/Persistence/Migrations`. A future host should:

1. Choose an application-data path on the target platform. A live SQLite file must not be shared over a network drive.
2. Register Infrastructure with `AddInventoryInfrastructure(databasePath, businessTimeZone)`.
3. Apply migrations once during startup with `MigrateInventoryDatabaseAsync()` before enabling UI operations.
4. Create one dependency-injection scope per application operation and dispose it when the operation completes.

Production startup should use migrations, not `EnsureCreated`, so the migration history remains authoritative.

## Before the first usable MAUI release

The remaining work includes the MAUI Blazor Hybrid host and host-level Application-service registrations, platform-specific database-path selection, startup migration/error handling, operation-scoped UI invocation, forms and navigation for the implemented workflows, certificate rendering/printing and reprinting, user-facing validation and concurrency handling, backup/recovery behavior, packaging, and platform testing. Authentication and any multi-device topology require explicit product and architecture decisions; neither exists today.

See [Pre-presentation technical decisions](docs/PRE_PRESENTATION_TECHNICAL_DECISIONS.md) for the Money precision assessment, MAUI DbContext lifetime design, current integration risks, and CI design.
