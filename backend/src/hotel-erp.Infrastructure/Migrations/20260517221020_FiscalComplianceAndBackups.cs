using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FiscalComplianceAndBackups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExemptAmount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExoneratedAmount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ExonerationOrderNumber",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalHash",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalSnapshotJson",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ISV15Amount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISV18Amount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsIsvExempt",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTouristTaxExempt",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalCorrelativeNumber",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalInvoiceId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SagRegistryNumber",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefinExonerationCertificateNumber",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableAmount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxpayerType",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExonerationOrderNumber",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExonerationValidFrom",
                table: "Guests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExonerationValidTo",
                table: "Guests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIsvExempt",
                table: "Guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTouristTaxExempt",
                table: "Guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SagRegistryNumber",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefinExonerationCertificateNumber",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxpayerType",
                table: "Guests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExonerationOrderNumber",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExonerationValidFrom",
                table: "Customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExonerationValidTo",
                table: "Customers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIsvExempt",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTouristTaxExempt",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SagRegistryNumber",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefinExonerationCertificateNumber",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxpayerType",
                table: "Customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorrelativeNumber",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HondurasTimestamp",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BackupLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LocalPath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GoogleDriveFileId = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadAttempts = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OriginalInvoiceId",
                table: "Invoices",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Hash",
                table: "AuditLogs",
                column: "Hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Invoices_OriginalInvoiceId",
                table: "Invoices",
                column: "OriginalInvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Invoices_OriginalInvoiceId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "BackupLogs");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_OriginalInvoiceId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Hash",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ExemptAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExoneratedAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExonerationOrderNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FiscalHash",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FiscalSnapshotJson",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ISV15Amount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ISV18Amount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsIsvExempt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsTouristTaxExempt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalCorrelativeNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SagRegistryNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SefinExonerationCertificateNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxableAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxpayerType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExonerationOrderNumber",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "ExonerationValidFrom",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "ExonerationValidTo",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "IsIsvExempt",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "IsTouristTaxExempt",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "SagRegistryNumber",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "SefinExonerationCertificateNumber",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "TaxpayerType",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "ExonerationOrderNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ExonerationValidFrom",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ExonerationValidTo",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsIsvExempt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsTouristTaxExempt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SagRegistryNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SefinExonerationCertificateNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxpayerType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CorrelativeNumber",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "HondurasTimestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "AuditLogs");
        }
    }
}
