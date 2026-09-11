using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenInvoiceAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT "ReferenceId"
                        FROM "AccountingEntries"
                        WHERE "ReferenceId" IS NOT NULL AND NOT "IsDeleted"
                        GROUP BY "ReferenceId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Existen asientos contables activos duplicados para el mismo documento; corríjalos antes de aplicar HardenInvoiceAdjustments.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalInvoiceItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems",
                column: "OriginalInvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_ReferenceId",
                table: "AccountingEntries",
                column: "ReferenceId",
                unique: true,
                filter: "\"ReferenceId\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems",
                column: "OriginalInvoiceItemId",
                principalTable: "InvoiceItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_OriginalInvoiceItemId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_AccountingEntries_ReferenceId",
                table: "AccountingEntries");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceItemId",
                table: "InvoiceItems");
        }
    }
}
