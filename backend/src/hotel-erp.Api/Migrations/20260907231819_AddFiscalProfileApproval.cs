using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalProfileApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalApprovalNote",
                table: "BusinessSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FiscalApprovedAt",
                table: "BusinessSettings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiscalApprovedByUserId",
                table: "BusinessSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalProfileStatus",
                table: "BusinessSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Borrador");

            migrationBuilder.AddColumn<int>(
                name: "FiscalProfileVersion",
                table: "BusinessSettings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "FiscalRetiredAt",
                table: "BusinessSettings",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiscalRetiredByUserId",
                table: "BusinessSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalRetirementReason",
                table: "BusinessSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FiscalValidFrom",
                table: "BusinessSettings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FiscalValidUntil",
                table: "BusinessSettings",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSettings_FiscalProfileStatus",
                table: "BusinessSettings",
                column: "FiscalProfileStatus");

            migrationBuilder.Sql("""
                UPDATE "BusinessSettings"
                SET "BusinessName" = '',
                    "RTN" = '',
                    "Address" = '',
                    "Phone" = '',
                    "Email" = '',
                    "IsvRate" = 0,
                    "TouristTaxRate" = 0,
                    "ShowFiscal" = FALSE,
                    "FiscalProfileStatus" = 'Borrador',
                    "FiscalProfileVersion" = 1
                WHERE "BusinessName" = 'Hotel Maya Central'
                  AND "RTN" = '08019012345678'
                  AND "Phone" = '9999-0000'
                  AND "Email" = 'info@hotelmayacentral.com';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessSettings_FiscalProfileStatus",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalApprovalNote",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalApprovedAt",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalApprovedByUserId",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalProfileStatus",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalProfileVersion",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalRetiredAt",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalRetiredByUserId",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalRetirementReason",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalValidFrom",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "FiscalValidUntil",
                table: "BusinessSettings");
        }
    }
}
