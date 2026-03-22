using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatellitePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LookupRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "correction_reason_lookups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_correction_reason_lookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "price_source_lookups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_source_lookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sector_lookups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sector_lookups", x => x.Id);
                });

            var manualPriceSourceId = "11111111-2222-3333-4444-555555555551";
            var importPriceSourceId = "11111111-2222-3333-4444-555555555552";
            var otherPriceSourceId = "11111111-2222-3333-4444-555555555553";

            migrationBuilder.Sql($"""
                INSERT INTO price_source_lookups ("Id", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT * FROM (
                    VALUES
                        ('{manualPriceSourceId}'::uuid, 'MANUAL', 'Manual', TRUE, NOW(), NOW()),
                        ('{importPriceSourceId}'::uuid, 'IMPORT', 'Import', TRUE, NOW(), NOW()),
                        ('{otherPriceSourceId}'::uuid, 'OTHER', 'Other', TRUE, NOW(), NOW())
                ) AS v("Id", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt")
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM price_source_lookups p
                    WHERE p."Code" = v."Code"
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO correction_reason_lookups ("Id", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT * FROM (
                    VALUES
                        ('22222222-3333-4444-5555-666666666661'::uuid, 'BROKER_ERROR', 'Broker Reported Error', TRUE, NOW(), NOW()),
                        ('22222222-3333-4444-5555-666666666662'::uuid, 'DATA_ENTRY', 'Data Entry Correction', TRUE, NOW(), NOW()),
                        ('22222222-3333-4444-5555-666666666663'::uuid, 'CORPORATE_ACTION', 'Corporate Action Adjustment', TRUE, NOW(), NOW())
                ) AS v("Id", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt")
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM correction_reason_lookups c
                    WHERE c."Code" = v."Code"
                );
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrectionReasonLookupId",
                table: "trades",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceSourceLookupId",
                table: "price_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SectorLookupId",
                table: "instruments",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO sector_lookups ("Id", "Code", "Name", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT
                    (
                        substr(md5(src.sector_name), 1, 8) || '-' ||
                        substr(md5(src.sector_name), 9, 4) || '-' ||
                        substr(md5(src.sector_name), 13, 4) || '-' ||
                        substr(md5(src.sector_name), 17, 4) || '-' ||
                        substr(md5(src.sector_name), 21, 12)
                    )::uuid,
                    UPPER(REPLACE(src.sector_name, ' ', '_')),
                    src.sector_name,
                    TRUE,
                    NOW(),
                    NOW()
                FROM (
                    SELECT DISTINCT TRIM("Sector") AS sector_name
                    FROM instruments
                    WHERE "Sector" IS NOT NULL AND TRIM("Sector") <> ''
                ) AS src
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM sector_lookups s
                    WHERE s."Name" = src.sector_name
                );
                """);

            migrationBuilder.Sql("""
                UPDATE instruments i
                SET "SectorLookupId" = s."Id"
                FROM sector_lookups s
                WHERE i."Sector" IS NOT NULL
                  AND TRIM(i."Sector") <> ''
                  AND TRIM(i."Sector") = s."Name";
                """);

            migrationBuilder.Sql($"""
                UPDATE price_snapshots
                SET "PriceSourceLookupId" = CASE "Source"
                    WHEN 1 THEN '{manualPriceSourceId}'::uuid
                    WHEN 2 THEN '{importPriceSourceId}'::uuid
                    ELSE '{otherPriceSourceId}'::uuid
                END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "PriceSourceLookupId",
                table: "price_snapshots",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Source",
                table: "price_snapshots");

            migrationBuilder.CreateIndex(
                name: "IX_trades_CorrectionReasonLookupId",
                table: "trades",
                column: "CorrectionReasonLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_price_snapshots_PriceSourceLookupId",
                table: "price_snapshots",
                column: "PriceSourceLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_instruments_SectorLookupId",
                table: "instruments",
                column: "SectorLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_correction_reason_lookups_Code",
                table: "correction_reason_lookups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_correction_reason_lookups_Name",
                table: "correction_reason_lookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_source_lookups_Code",
                table: "price_source_lookups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_price_source_lookups_Name",
                table: "price_source_lookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sector_lookups_Code",
                table: "sector_lookups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sector_lookups_Name",
                table: "sector_lookups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_instruments_sector_lookups_SectorLookupId",
                table: "instruments",
                column: "SectorLookupId",
                principalTable: "sector_lookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_price_snapshots_price_source_lookups_PriceSourceLookupId",
                table: "price_snapshots",
                column: "PriceSourceLookupId",
                principalTable: "price_source_lookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_trades_correction_reason_lookups_CorrectionReasonLookupId",
                table: "trades",
                column: "CorrectionReasonLookupId",
                principalTable: "correction_reason_lookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_instruments_sector_lookups_SectorLookupId",
                table: "instruments");

            migrationBuilder.DropForeignKey(
                name: "FK_price_snapshots_price_source_lookups_PriceSourceLookupId",
                table: "price_snapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_trades_correction_reason_lookups_CorrectionReasonLookupId",
                table: "trades");

            migrationBuilder.DropTable(
                name: "correction_reason_lookups");

            migrationBuilder.DropTable(
                name: "price_source_lookups");

            migrationBuilder.DropTable(
                name: "sector_lookups");

            migrationBuilder.DropIndex(
                name: "IX_trades_CorrectionReasonLookupId",
                table: "trades");

            migrationBuilder.DropIndex(
                name: "IX_price_snapshots_PriceSourceLookupId",
                table: "price_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_instruments_SectorLookupId",
                table: "instruments");

            migrationBuilder.DropColumn(
                name: "CorrectionReasonLookupId",
                table: "trades");

            migrationBuilder.DropColumn(
                name: "PriceSourceLookupId",
                table: "price_snapshots");

            migrationBuilder.DropColumn(
                name: "SectorLookupId",
                table: "instruments");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "price_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
