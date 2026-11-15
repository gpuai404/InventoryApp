# InventoryApp — Phase B: Application Layer Alignment

## Objective

Align the existing Application Layer with the newly implemented and tested Phase A Domain Layer.

Phase A is complete. Do not redesign or unnecessarily modify the Domain Layer.

Application, Infrastructure, and Presentation were intentionally left unchanged during Phase A.

The goal of Phase B is to remove the outdated domain assumptions from Application and implement the two agreed warranty issuance use cases correctly.

## 1. Inspect before modifying

Start by inspecting:

- Current Git branch and working tree.
- Current solution/project structure.
- Actual Phase A Domain implementation.
- Existing Application services and contracts.
- Existing Domain.Tests suite.
- All 15 reported Application compilation errors.

Read the actual source code. Do not use archived implementation plans as evidence of the current code structure.

Identify every reference to:

- ProductVariant
- ProductVariantId
- ProductBarcode
- ProductBarcodeId
- Product SKU
- SerialNumber
- WarrantyStatus
- Void()
- VoidAsync()
- SaleDetails.PurchasePrice
- Old ProductSnapshot constructors
- Company and CompanyRole
- Old Issuer/Retailer relationships

Classify each occurrence as:
1. Application code that must change.
2. Infrastructure references to defer to Phase C.
3. Presentation references to defer to Phase D.
4. Tests and documentation requiring updates.

## 2. Scope restrictions

Modify only:

- InventoryApp.Application
- Existing Application test project, if available.

Do not modify:
- InventoryApp.Domain
- InventoryApp.Infrastructure
- MAUI/Blazor Presentation
- EF Core configurations
- SQLite database
- Migrations

Do not modify the Domain merely to make existing Application code compile.

If a genuine missing Domain capability is discovered, document it and explain why the existing Domain API is insufficient before proposing any change.

## 3. Product application operations

Align product operations with the new Product aggregate.

Product is the only catalog entity.

Operations should use:

- ProductId
- CategoryId
- ProductName
- Description
- Money

Remove variant and barcode application operations, request models, repository interfaces and references.

Do not replace them with another generic identification abstraction.

Preserve product category lookup where needed for certificate presentation and ProductSnapshot creation.

## 4. Product-sale warranty issuance

Implement or adapt the explicit use case:

IssueProductSaleWarranty

Its responsibilities:

1. Receive the product selection and sale information.
2. Load the selected Product.
3. Obtain the applicable product category information.
4. Obtain and validate the configured issuer profile.
5. Obtain customer information.
6. Obtain the appropriate product-sale warranty plan, if plans are retained.
7. Build the historical ProductSnapshot, including Product.Price.
8. Create the Warranty through its product-sale issuance factory.
9. Persist through the existing application persistence abstraction.
10. Return the issued warranty identity and information required by Presentation.

The workflow must not:
- Require ProductVariant.
- Require barcode lookup.
- Accept generic SerialNumber.
- Supply a separate purchase price.
- Allow repair coverage fields.

Use Imei where applicable, according to currently established applicability rules. Do not invent category-specific mandatory IMEI rules.

## 5. Repair warranty issuance

Implement or adapt the explicit use case:

IssueRepairWarranty

Its responsibilities:

1. Receive the product selection and repair information.
2. Load the selected Product.
3. Obtain the applicable product category information.
4. Obtain and validate the configured issuer profile.
5. Obtain customer information.
6. Obtain the appropriate repair warranty plan, if plans are retained.
7. Build the historical ProductSnapshot, including Product.Price.
8. Build repair details.
9. Preserve replaced parts as a collection of descriptions, where applicable.
10. Create Warranty through its repair issuance factory.
11. Persist through the existing application persistence abstraction.
12. Return the issued warranty identity and information required by Presentation.

The repair warranty covers ONLY the completed repair work and replaced parts.

It does not cover the entire device or extend its original warranty.

Do not introduce:
- Individual labor prices.
- Individual part prices.
- Separate repair-price abstractions that duplicate Product.Price.
- Generic service-management infrastructure.

A repair without replaced parts must remain supported.

## 6. Issuer configuration

Align Application with the agreed single-issuer model.

Use an application abstraction for obtaining the configured IssuerProfile.

Do not introduce:
- Multiple selectable companies.
- CompanyRole.
- Retailer selection.
- Multi-tenant company ownership.

If Infrastructure currently implements Company repositories, leave those implementations untouched during Phase B and document the required Phase C changes.

Verify whether a mandatory issuer address is an actual certificate/business requirement. Do not independently weaken or strengthen the Domain invariant.

## 7. Warranty plans and coverage

Keep the two warranty workflows separate at the application use-case level.

A ProductSale plan must never be supplied to repair issuance.

A Repair plan must never be supplied to product-sale issuance.

The Application Layer should coordinate and select the appropriate plan. The Domain remains responsible for enforcing business invariants.

Do not duplicate domain validation in application services.

Do not introduce MediatR, generic command-handler frameworks or a generic workflow engine.

Preserve the current explicit Application service architecture.

## 8. Immutability and printing

Remove application operations for:

- Warranty correction.
- Warranty cancellation.
- Warranty voiding.
- Warranty revision.

Do not introduce update operations for issued warranties.

Keep retrieval and printing separate from issuance.

Printing must use an already-persisted warranty. Reprinting must never create a new warranty or certificate number.

Do not redesign the printing infrastructure during this phase.

## 9. Testing

Add or adapt Application tests for:

### Product-sale issuance
- Loads the selected Product.
- Uses Product.Price.
- Builds the historical ProductSnapshot.
- Uses the correct product-sale plan.
- Rejects a repair plan.
- Captures the correct customer and issuer snapshots.
- Calls the correct Domain issuance factory.

### Repair issuance
- Loads the selected Product.
- Uses Product.Price.
- Builds the historical ProductSnapshot.
- Uses the correct repair plan.
- Rejects a product-sale plan.
- Supports labor-only repair.
- Preserves replaced-part descriptions.
- Uses repair-specific duration and terms.
- Cannot accidentally create entire-device coverage.

### Error handling
- Missing Product.
- Missing plan.
- Wrong plan type.
- Missing issuer configuration.
- Invalid customer/repair information.
- Domain rule violations.
- Persistence failures.

Use the existing testing conventions. Do not create elaborate mocking frameworks.

## 10. Build and verification

At completion:

1. Build InventoryApp.Domain.
2. Run Domain.Tests.
3. Build InventoryApp.Application.
4. Run Application.Tests if available.
5. Inspect all Application references to removed Domain concepts.
6. Report any remaining compilation errors in Infrastructure or Presentation separately.

The complete solution may continue to fail until Phase C and Phase D. Do not modify those layers to force a premature green solution build.

## 11. Expected delivery

Report:

- Files changed.
- Old dependencies removed.
- Updated Application use cases.
- Application abstractions introduced or modified.
- Tests added and results.
- Application compilation result.
- Remaining Infrastructure dependencies.
- Remaining Presentation dependencies.
- Open business questions.

Do not claim successful integration where downstream projects have not yet been updated.

The objective is a clean Application Layer that correctly orchestrates the two real warranty issuance workflows using the Phase A Domain API, without unnecessary architectural abstractions.