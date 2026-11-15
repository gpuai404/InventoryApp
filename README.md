# Warranty Issuance

A single-company .NET MAUI + Blazor Hybrid application for maintaining a product catalog and issuing durable warranty certificates for product sales and repairs.

## Confirmed product decisions

- **Client:** .NET MAUI with Blazor Hybrid.
- **Database:** SQLite.
- **Business scope:** one operating company. There is no multi-company, tenant, company registration, company switching, or company-level data isolation feature.
- **Administration:** one global administrator manages the application. Do not add company memberships or per-company permissions.
- **Initial topology:** one application installation/active instance with its own local SQLite database. Treat tenant count and active application-instance count as one by design; do not add tenant/instance configuration switches or infrastructure yet. Multiple devices or instances are future requirements and need an explicit architecture decision. Never share a live SQLite file over a network drive.

## Current core implementation

The repository contains three core projects:

- `src/WarrantyIssueXE.Domain`
- `src/WarrantyIssueXE.Application`
- `src/WarrantyIssueXE.Infrastructure`

Infrastructure uses Entity Framework Core with SQLite. A root solution, central build/package configuration, and Domain, Application, and SQLite test projects are present. The MAUI/Blazor presentation host and administrator authentication remain subsequent work.

The active model has one configured `IssuerProfile`. Warranty plans are installation-wide, and each issued warranty owns an immutable issuer snapshot.

Products and variants can be maintained without barcodes. A barcode is an optional, globally unique variant lookup value and is assigned through an explicit catalogue command; warranty issuance does not require or snapshot it.

## Important domain distinction

An issued warranty is a historical business record. Product, issuer, customer, and coverage details are captured as snapshots at issuance. Voiding is a controlled lifecycle transition; ordinary editing of an issued certificate is not part of the observed model.

## Documentation

Start with the product, architecture, documentation under `docs/`.
