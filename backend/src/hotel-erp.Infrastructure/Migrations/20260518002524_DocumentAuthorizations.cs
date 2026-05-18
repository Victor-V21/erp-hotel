using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DocumentAuthorizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationDueDateSnapshot",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationRangeSnapshot",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CAINumberSnapshot",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentAuthorizationId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CAINumber = table.Column<string>(type: "text", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InitialRange = table.Column<string>(type: "text", nullable: false),
                    FinalRange = table.Column<string>(type: "text", nullable: false),
                    CurrentCorrelative = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuthorizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DocumentAuthorizationId",
                table: "Invoices",
                column: "DocumentAuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuthorizations_DocumentType_CAINumber",
                table: "DocumentAuthorizations",
                columns: new[] { "DocumentType", "CAINumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuthorizations_DocumentType_Status",
                table: "DocumentAuthorizations",
                columns: new[] { "DocumentType", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_DocumentAuthorizations_DocumentAuthorizationId",
                table: "Invoices",
                column: "DocumentAuthorizationId",
                principalTable: "DocumentAuthorizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_DocumentAuthorizations_DocumentAuthorizationId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "DocumentAuthorizations");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_DocumentAuthorizationId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AuthorizationDueDateSnapshot",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AuthorizationRangeSnapshot",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CAINumberSnapshot",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DocumentAuthorizationId",
                table: "Invoices");
        }
    }
}
