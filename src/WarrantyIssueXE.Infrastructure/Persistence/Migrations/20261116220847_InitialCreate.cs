using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarrantyIssueXE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssuerProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VatNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuerProfile", x => x.Id);
                    table.CheckConstraint("CK_IssuerProfile_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CertificateNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WarrantyPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Imei = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                    IssuedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CustomerFullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    CustomerPhone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CustomerAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductCategoryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductPriceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductPriceCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IssuerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IssuerVatNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IssuerAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IssuerEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    IssuerPhone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CoverageTermMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    CoverageScope = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CoverageDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CoverageScopeStatement = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    PurchasedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RepairedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RepairWorkDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                    table.CheckConstraint("CK_Warranties_CoverageScope", "(Type = 'ProductSale' AND CoverageScope = 'Product') OR (Type = 'Repair' AND CoverageScope = 'RepairWorkAndReplacedParts')");
                    table.CheckConstraint("CK_Warranties_CoverageTerm", "CoverageTermMonths > 0");
                    table.CheckConstraint("CK_Warranties_DateRange", "ExpiresOn >= StartsOn");
                    table.CheckConstraint("CK_Warranties_Imei", "Imei IS NULL OR (length(Imei) = 15 AND Imei NOT GLOB '*[^0-9]*')");
                    table.CheckConstraint("CK_Warranties_ProductPrice", "CAST(ProductPriceAmount AS NUMERIC) >= 0");
                    table.CheckConstraint("CK_Warranties_Type", "Type IN ('ProductSale', 'Repair')");
                    table.CheckConstraint("CK_Warranties_TypeSpecificDetails", "(Type = 'ProductSale' AND PurchasedOn IS NOT NULL AND RepairedOn IS NULL AND RepairWorkDescription IS NULL AND StartsOn = PurchasedOn) OR (Type = 'Repair' AND PurchasedOn IS NULL AND RepairedOn IS NOT NULL AND RepairWorkDescription IS NOT NULL AND StartsOn = RepairedOn)");
                });

            migrationBuilder.CreateTable(
                name: "WarrantyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TermMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    CoverageDescription = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyPlans", x => x.Id);
                    table.CheckConstraint("CK_WarrantyPlans_TermMonths", "TermMonths > 0");
                    table.CheckConstraint("CK_WarrantyPlans_Type", "Type IN ('ProductSale', 'Repair')");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PriceCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_PriceAmount", "CAST(PriceAmount AS NUMERIC) >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyReplacedParts",
                columns: table => new
                {
                    WarrantyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    PartNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyReplacedParts", x => new { x.WarrantyId, x.Position });
                    table.ForeignKey(
                        name: "FK_WarrantyReplacedParts_Warranties_WarrantyId",
                        column: x => x.WarrantyId,
                        principalTable: "Warranties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ArchivedAtUtc",
                table: "Categories",
                column: "ArchivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ArchivedAtUtc",
                table: "Products",
                column: "ArchivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_CertificateNumber",
                table: "Warranties",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_IssuedAtUtcTicks",
                table: "Warranties",
                column: "IssuedAtUtcTicks");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyPlans_ArchivedAtUtc",
                table: "WarrantyPlans",
                column: "ArchivedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssuerProfile");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "WarrantyPlans");

            migrationBuilder.DropTable(
                name: "WarrantyReplacedParts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Warranties");
        }
    }
}
