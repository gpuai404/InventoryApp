# InventoryApp — Agent Knowledge and Implementation Instructions

## 1. Project purpose

InventoryApp is a .NET 10 MAUI Blazor Hybrid application for a retailer/service business.

Its purpose is to:

1. Maintain a product catalog.
2. Issue product-sale warranties.
3. Issue repair warranties.
4. Store and retrieve issued warranty certificates.
5. Print and reprint certificates.

This is NOT a full inventory, warehouse, POS, checkout, repair-job management, or stock-management system.

Keep the implementation pragmatic and focused on these actual business workflows.

## 2. Source-of-truth rules

The latest decisions made directly by the product owner take precedence over older implementation plans, archived documentation, existing source code, and previous AI-generated recommendations.

The following older assumptions are superseded:

- ProductBarcode management.
- Barcode-based product identification.
- Generic serial-number identification.
- Unnecessary ProductVariant complexity.
- Warranty cancellation and voiding.
- Separate company/retailer selection when using the agreed single-issuer configuration.

In particular, do not follow older implementation-plan instructions that recommend retaining ProductVariant/ProductBarcode or the one-way Issued → Voided lifecycle.

Before implementation, inspect the actual working tree. The previously reviewed `src.zip` is a source snapshot, not proof of the current repository state.

## 3. Confirmed business rules

### Product catalog

Product is the catalog entity and owns its price.

A Product contains:

- ProductId
- Name
- CategoryId
- Description (optional)
- Price (Money)

Use the existing Category catalog if it is already implemented and justified.

Product category determines the appropriate certificate presentation. Employees must not manually select a product category or certificate design during warranty issuance.

The product price is used for both product-sale and repair warranty workflows.

The price must be captured in the warranty's historical product snapshot so later catalog price changes cannot alter existing certificates.

Do not introduce separate product-sale and repair product prices unless a distinct business requirement is established.

### Product identification

Use IMEI as the individual device identifier where applicable.

Do not introduce:

- SerialNumber
- Generic device identifiers
- ProductBarcodes
- Barcode lookup as a required workflow

Do not use ProductVariant merely to represent an individual device.

If the current repository contains a genuine independent product-variation requirement, demonstrate it before retaining a variant model. Do not preserve it solely because the previous implementation used it.

### Exactly two warranty workflows

The application has exactly these issuance workflows:

1. Issue a product-sale warranty.
2. Issue a repair warranty.

They share the Warranty aggregate but have different coverage rules, duration rules, and terms.

### Product-sale warranty

Records the warranty issued when a product is sold.

Relevant information:

- Selected Product
- Product snapshot, including price and category
- IMEI where applicable
- Customer details
- Purchase date
- Product-sale warranty duration
- Product-sale warranty terms
- Issuer snapshot
- Issuance timestamp and certificate number

The product-sale coverage applies to the sold product according to the agreed terms.

### Repair warranty

Records a warranty issued after repair work.

Relevant information:

- Selected Product
- Product snapshot, including price and category
- IMEI where applicable
- Customer details
- Repair completion date
- Repair description
- Replaced parts, where applicable
- Repair warranty duration
- Repair warranty terms
- Issuer snapshot
- Issuance timestamp and certificate number

Repair coverage is strictly limited to:

- The completed repair work.
- The parts replaced as part of that repair.

It does NOT cover the entire device, restart the original product warranty, or extend the original product warranty.

A repair warranty may exist without replaced parts if the work involved labor only.

Do not create individual labor or replaced-part prices. The certificate has one applicable amount, using the established Product.Price value. Do not introduce another repair-price concept that duplicates it without an explicit business requirement.

## 4. Warranty domain model

Warranty is the central aggregate root.

Recommended conceptual structure:

Warranty
- WarrantyId
- WarrantyNumber
- IssuedAt
- StartsOn
- ExpiresOn
- ProductSnapshot
- Imei (where applicable)
- CustomerSnapshot
- IssuerSnapshot
- Coverage

Coverage has exactly two alternatives:

ProductSaleCoverage
- Warranty duration
- Product-sale terms/coverage description
- Relevant sale information

RepairCoverage
- Warranty duration
- Repair terms/coverage description
- Repair description
- ReplacedParts collection

The implementation may use the existing SaleDetails and RepairDetails value objects or a closed coverage hierarchy, provided it enforces exactly these two valid alternatives.

Do not undertake a large inheritance refactor merely for stylistic reasons.

The important requirements are explicit invariants and valid construction.

### Historical snapshots

An issued warranty must remain understandable independently of the current catalog and configuration.

ProductSnapshot:
- ProductId
- Product name
- Category information needed for historical certificate presentation
- Product price
- Other genuinely required historical display information

CustomerSnapshot:
- Customer name
- Optional email
- Optional phone
- Optional address

IssuerSnapshot:
- Business name
- Optional VAT number
- Address
- Contact information

All snapshots are immutable after issuance.

Changes to Product, Customer input, or current IssuerProfile must never modify an issued Warranty.

### Warranty terms and duration

Product-sale and repair warranties have different duration and terms rules.

A reusable WarrantyPlan is optional.

If retained:
- A plan belongs to exactly one warranty type.
- Sale plans cannot be used for repair warranties.
- Repair plans cannot be used for product-sale warranties.
- The final duration and coverage wording must be copied into the issued Warranty.
- Later changes to a plan must not change historical warranties.

A shared WarrantyTerm value object is acceptable if both workflows use the same calendar-month representation and validation. Workflow-specific restrictions should be enforced in the appropriate domain operation.

Do not introduce separate duration types solely to create more classes.

Preserve the approved start-date policy:
- Product-sale warranty: purchase date.
- Repair warranty: repair completion date.

Verify and test expiration calculations, especially month-end and leap-year behavior. Do not silently change existing expiry semantics.

## 5. Warranty immutability and lifecycle

Once issued, a Warranty cannot be corrected, edited, cancelled, or voided.

Do not implement:
- WarrantyStatus
- VoidedAt
- VoidReason
- Void()
- Cancel()
- Revise()
- UpdateWarranty
- Correction workflows

The issuance form can be edited before final confirmation. An issued domain object is created only after the user confirms issuance.

Persist the warranty before printing.

Printing failure must not produce another warranty. Reprinting must use the existing WarrantyId and certificate number, with its original historical information.

Do not introduce a persisted Draft warranty unless the product owner explicitly requests saving incomplete issuance forms.

## 6. Issuer model

Use one configured IssuerProfile for the operating business, rather than a multi-company or multi-tenant system.

IssuerProfile represents current application configuration.

IssuerSnapshot represents the historical issuer identity copied into each Warranty at issuance.

Changes to IssuerProfile affect future warranties only.

Do not introduce:
- CompanyRole
- Supplier/Retailer business roles
- Multiple selectable issuers
- TenantId
- Company memberships
- Company-scoped permissions

The application must prevent warranty issuance if the required issuer profile has not been configured.

Keep historical issuer information intact.

## 7. Domain design principles

Apply pragmatic C# object-oriented design, taking inspiration from Zoran Horvat's Coding Helmet material.

Prioritize:

1. Encapsulation.
2. Strong domain types where they provide real value.
3. Objects that protect their own invariants.
4. Meaningful operations instead of unrestricted setters.
5. Valid construction.
6. Behavior close to the data it operates on.
7. Small, cohesive classes.
8. Explicit aggregate consistency boundaries.
9. Composition and delegation where they simplify responsibilities.

Examples:

- Money owns monetary validation.
- Imei owns IMEI validation.
- WarrantyTerm owns duration and expiry calculations.
- Product protects catalog invariants.
- Warranty protects issuance invariants.
- RepairCoverage protects repair-specific rules.

Avoid anemic entities with public setters.

Do not introduce abstractions merely to follow DDD terminology.

## 8. Project architecture

Keep the existing three-project separation unless the actual repository demonstrates a compelling reason to change it.

### InventoryApp.Domain

Contains:
- Product and Warranty domain objects.
- Value objects.
- Aggregate behavior.
- Business invariants.
- Domain exceptions.
- Strongly typed identifiers.

Must not reference:
- EF Core
- SQLite
- MAUI
- Blazor
- HTTP
- API DTOs
- Presentation ViewModels

### InventoryApp.Application

Contains explicit use cases:

- CreateProduct
- UpdateProduct
- IssueProductSaleWarranty
- IssueRepairWarranty
- GetWarranty
- ListWarranties
- GetProducts
- MaintainWarrantyPlans, if retained

Coordinates domain operations, reads, persistence abstractions, and printing requests.

Use explicit command/query separation where useful.

No MediatR or generic CQRS framework.

Keep the application interfaces focused on actual use cases.

### InventoryApp.Infrastructure

Contains:
- EF Core DbContext.
- SQLite mappings.
- Persistence records.
- Repository implementations.
- Migrations.
- Printing integration.
- Technical implementations of application abstractions.

Keep EF Core mapping separate from Domain entities.

### Presentation / MAUI Blazor Hybrid

Responsible for:
- Form state.
- User input.
- Workflow navigation.
- Validation feedback.
- Review screens.
- Selecting the certificate presentation from Product category and warranty type.
- Invoking issuance and printing use cases.

Do not put domain rules in Razor components.

Do not let the UI manually choose a certificate design or category.

## 9. Review of existing implementation

The previously reviewed November 11 `src.zip` showed the following.

### Keep and build on

- Product encapsulation.
- ProductId and other useful strongly typed IDs.
- Money value object.
- Warranty issuance factories.
- WarrantyTerm.
- WarrantyPlan, if reusable plans remain required.
- CoverageSnapshot and its repair-specific scope statement.
- CustomerSnapshot.
- RetailerSnapshot, adapting it to IssuerSnapshot.
- RepairDetails.
- ReplacedPartSnapshot.
- Defensive copying of collections.
- Separate Infrastructure persistence records.
- Domain independence from EF Core.
- Explicit Application services.

The existing Product class is a good encapsulation example. Its Revise operation creates a valid replacement while preserving identity.

The existing repair scope statement is also worth preserving because it explicitly prevents the repair warranty from appearing to cover the entire device.

### Remove or replace

Previously identified outdated concepts:

- ProductVariant and ProductVariantId, unless a genuine separate variation requirement is demonstrated.
- ProductBarcode and ProductBarcodes table.
- SerialNumber and its persistence columns.
- WarrantyStatus and void lifecycle.
- Company and CompanyRole, in favor of the single issuer profile.
- IssuedByRetailerId if using the agreed single-issuer design.
- SaleDetails.PurchasePrice as a duplicate price source.
- VariantName and SKU in ProductSnapshot when no variant model is retained.

### Fix historical product snapshot

The previous ProductSnapshot contained only product name, variant name and optional SKU.

It must preserve product identity, category information and the price used when the warranty was issued.

### Align price handling

The previous SaleDetails had an independent PurchasePrice, whereas RepairDetails did not contain the same price information.

Align both workflows around Product.Price and capture it in ProductSnapshot.

Do not add separate part/labor pricing.

### Tests

The November 11 source archive did not contain a matching test suite or MAUI host. Inspect the actual repository before creating replacements.

Do not assume old tests for ProductVariant, ProductBarcode, SerialNumber or Warranty voiding remain valid.

## 10. Persistence and migration requirements

Update Domain, Application, Infrastructure, EF configurations and database schema together.

Inspect:
- InventoryDbRecords.cs
- InventoryDbContext.cs
- DomainRecordMappings.cs
- Repositories.cs
- Product and Warranty configurations
- WarrantyPlan configuration
- Existing migrations
- Model snapshot

Remove obsolete product variant/barcode, serial-number, company-role and warranty-void persistence structures where the final model no longer needs them.

Do not edit an already-applied migration.

Before migration work:

1. Inspect the actual SQLite database.
2. Establish whether the existing initial migration has been applied.
3. Back up any database containing real records.
4. Determine whether existing warranty records have historical snapshots that must be preserved.
5. Generate a new migration when needed.
6. Review the migration SQL.
7. Test it against a real SQLite database.

Do not silently discard or arbitrarily merge existing data.

Use real SQLite integration tests for persistence verification. EF Core InMemory is not sufficient for SQLite mapping and migration verification.

## 11. Required tests

### Domain

- Product rejects empty name.
- Product rejects invalid price.
- Product revision preserves identity.
- Product price changes do not alter previously issued snapshots.
- IMEI validation rejects invalid input.
- Product-sale warranty can only be created with sale coverage.
- Repair warranty can only be created with repair coverage.
- Sale and repair plans cannot be interchanged.
- Repair warranty requires repair description.
- Labor-only repair is supported.
- Replaced parts are optional and copied defensively.
- Repair scope cannot represent entire-device coverage.
- Invalid warranty duration and expiry combinations are rejected.
- Issued Warranty has no correction/cancellation/void operations.
- Customer, product and issuer snapshots remain immutable.

### Application

- Product-sale issuance uses selected Product.
- Repair issuance uses selected Product.
- Both capture the Product price.
- Each workflow uses its appropriate coverage rules.
- Each workflow uses its own duration and terms.
- IMEI is captured where applicable.
- Missing issuer configuration prevents issuance.
- Repeated print attempts reuse the same certificate.

### Infrastructure

- Product and category persistence works.
- Both warranty types survive SQLite round-tripping.
- All historical snapshot fields survive.
- Product price is preserved in the issued warranty.
- Replaced parts survive persistence.
- Reopening the database and loading historical warranties does not require current Product or IssuerProfile values.
- Migrations preserve existing historical records.

## 12. Implementation procedure

Before making changes:

1. Inspect the current branch and git status.
2. Read AGENTS.md and the current architecture/business-rule documents.
3. Identify the real MAUI host and test projects.
4. Compare the current working tree with the archived source findings.
5. Identify outdated documentation that conflicts with the latest product-owner decisions.
6. Record baseline build and test results.

Then implement in small, coherent changesets:

**Phase A — Domain correction**
- Simplify Product and remove unnecessary variation/barcode concepts.
- Introduce Imei.
- Correct ProductSnapshot.
- Correct Warranty creation and immutability.
- Align issuer and coverage models.

**Phase B — Application correction**
- Update both issuance use cases.
- Remove outdated requests, DTOs and repository interfaces.
- Ensure both workflows use Product.Price and the appropriate warranty coverage.

**Phase C — Infrastructure correction**
- Update mappings, records and repositories.
- Create safe database migrations.
- Test historical snapshot persistence.

**Phase D — Verification**
- Run dotnet build.
- Run all available dotnet tests.
- Run SQLite integration tests.
- Inspect the final model for obsolete terminology and unused abstractions.

Use the repository's actual SDK and dependency versions. The application target is .NET 10.

## 13. Engineering constraints

Do not:
- Rewrite the application from scratch.
- Introduce MediatR.
- Add a generic repository framework.
- Introduce a generic workflow engine.
- Add domain events without a real consumer requirement.
- Add unnecessary aggregates.
- Create a Customer aggregate solely for certificate customer details.
- Add a Device aggregate solely to hold IMEI.
- Add product barcode or serial-number infrastructure.
- Introduce speculative abstractions.
- Change project names or namespaces unnecessarily.
- Mix domain correction with unrelated UI redesign or certificate styling.

Prefer the smallest change that correctly expresses the real business model.

## 14. Expected agent delivery

At completion, report:

1. Actual repository state inspected and branch used.
2. Domain changes and reasons.
3. Application changes.
4. Infrastructure and migration changes.
5. Tests added and results.
6. Build result.
7. Any existing data migration risks.
8. Decisions that remain unresolved.

Do not claim a feature or test works unless it has been verified.

When requirements conflict, use the latest direct product-owner decisions. If a material business rule remains genuinely ambiguous, explain the conflict and do not invent a new business concept.

**Primary objective: a small, stable, maintainable C# domain model that accurately represents the two real warranty issuance workflows.**
