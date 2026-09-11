using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AtomicFolioSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "CashMovements"
                        WHERE "ReferenceId" IS NOT NULL AND NOT "IsDeleted"
                        GROUP BY "ReferenceId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'No se puede asegurar la unicidad de caja: existen movimientos activos con ReferenceId duplicado.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "AppliedDiscountId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppliedDiscountNameSnapshot",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedDiscountPercentageSnapshot",
                table: "Invoices",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FolioId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FolioItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AppliedDiscountId",
                table: "Invoices",
                column: "AppliedDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FolioId",
                table: "Invoices",
                column: "FolioId",
                unique: true,
                filter: "\"FolioId\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_FolioItemId",
                table: "InvoiceItems",
                column: "FolioItemId",
                unique: true,
                filter: "\"FolioItemId\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_ReferenceId",
                table: "CashMovements",
                column: "ReferenceId",
                unique: true,
                filter: "\"ReferenceId\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_FolioItems_FolioItemId",
                table: "InvoiceItems",
                column: "FolioItemId",
                principalTable: "FolioItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Discounts_AppliedDiscountId",
                table: "Invoices",
                column: "AppliedDiscountId",
                principalTable: "Discounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Folios_FolioId",
                table: "Invoices",
                column: "FolioId",
                principalTable: "Folios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_FolioItems_FolioItemId",
                table: "InvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Discounts_AppliedDiscountId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Folios_FolioId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_AppliedDiscountId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_FolioId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_FolioItemId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_ReferenceId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "AppliedDiscountId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AppliedDiscountNameSnapshot",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AppliedDiscountPercentageSnapshot",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FolioId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FolioItemId",
                table: "InvoiceItems");
        }
    }
}
