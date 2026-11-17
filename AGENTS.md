# WarrantyIssueXE agent instructions

## Authority and scope

Inspect the current working tree before changing it. Direct, current product-owner instructions take precedence over older plans and archived reviews. Preserve unrelated work and keep changes small.

WarrantyIssueXE is intended to become a .NET 10 MAUI Blazor Hybrid application for one retailer/service business. It maintains a product catalog, issues product-sale and repair warranties, stores immutable warranty certificates, and will eventually print and reprint them. It is not a general inventory, warehouse, POS, checkout, stock, repair-job, multi-company, or multi-tenant system.

## Confirmed business model

- `Product` is the catalog item and owns its `Money` price and `CategoryId`.
- A category cannot be archived while active products reference it. Reassign or archive those products first; do not cascade archival or physically delete records.
- Product category selects certificate presentation. Employees do not choose a category or design during issuance.
- IMEI is the only individual-device identifier currently supported, and it is optional where not applicable.
- Do not add product variants, barcodes, barcode lookup, generic serial numbers, or device aggregates without a new explicit requirement.
- There is one current `IssuerProfile`. Issuance copies it to an immutable `IssuerSnapshot`.
- There are exactly two issuance workflows: product sale and repair.
- Both workflows use and snapshot `Product.Price`; do not introduce separate sale, repair, labor, or part prices.
- Repair coverage is limited to the completed work and replaced parts. Labor-only repairs are valid.
- Every new warranty is issued from an active administrator-managed warranty plan; employees do not enter one-off terms or wording.
- Product-sale and repair plans cannot be interchanged. Final term and wording are copied into the warranty.
- Product-sale coverage starts on purchase date. Repair coverage starts on repair completion date. Preserve existing expiry semantics.
- Issued warranties are immutable. Do not add editing, correction, draft persistence, cancellation, status, or voiding.
- Persist before printing. Reprinting must use the existing warranty ID, certificate number, and snapshots.

Historical `ProductSnapshot`, `CustomerSnapshot`, `IssuerSnapshot`, coverage, and repair details must remain understandable without current master records. Later catalog, plan, or issuer changes must not alter issued warranties.

## Architecture

- `WarrantyIssueXE.Domain` owns entities, value objects, identifiers, snapshots, aggregate behavior, and invariants. It must not reference EF Core, SQLite, MAUI, Blazor, HTTP, DTOs, or presentation models.
- `WarrantyIssueXE.Application` owns explicit use cases and focused technical ports. Keep command/query separation pragmatic. Do not add MediatR or a generic CQRS/repository framework.
- `WarrantyIssueXE.Infrastructure` owns EF Core SQLite records, mappings, repositories, migrations, and technical implementations. Keep persistence records separate from domain objects.
- Presentation will own form state, navigation, review screens, validation feedback, certificate presentation selection, and operation invocation. Do not move domain rules into Razor components.

Use encapsulation, valid construction, strong domain types where useful, meaningful behavior, immutable snapshots, and focused classes. Do not introduce abstractions or architectural patterns without a concrete requirement.

## Persistence and UI lifetime

Use real SQLite tests for persistence and migrations; EF Core InMemory is not a substitute. Do not edit an applied migration. Inspect and protect real data before adding a migration.

The future MAUI host must create one short-lived DI scope per UI operation. Repositories and unit of work in that scope share one `InventoryDbContext`; dispose the scope after the operation. Never retain a context or scoped persistence service for a component lifetime, and never share a scope across concurrent operations.

## Money

Current `Money` behavior accepts non-negative decimal amounts without rounding or scale restriction and normalizes three-letter currency codes. Do not silently change this. Supported currencies and minor-unit precision require a product decision; document the impact before enforcing a new rule.

## Verification

For relevant changes, run the solution build and all tests, including migration-backed SQLite tests. Report actual results and unresolved business or migration risks. Do not claim unverified behavior, and do not commit or push unless explicitly requested.
