using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_MovementDate_CreatedAt",
                table: "CashMovements");

            migrationBuilder.Sql(
                """
                UPDATE "CashMovements"
                SET "MovementDate" = "MovementDate" - INTERVAL '6 hours'
                WHERE "MovementType" IN ('Apertura', 'Cierre')
                  AND ABS(EXTRACT(EPOCH FROM ("CreatedAt" - "MovementDate"))) < 300;
                """);

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RefundDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CashRegisterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountingEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.CheckConstraint("CK_Refunds_Amount_Positive", "\"Amount\" > 0");
                    table.CheckConstraint("CK_Refunds_Currency_HNL", "\"Currency\" = 'HNL'");
                    table.CheckConstraint("CK_Refunds_MethodFields", "(\"Method\" = 'Efectivo' AND \"CashRegisterId\" IS NOT NULL AND \"ExternalReference\" = '') OR (\"Method\" IN ('Tarjeta', 'Transferencia') AND \"CashRegisterId\" IS NULL AND length(btrim(\"ExternalReference\")) >= 3)");
                    table.CheckConstraint("CK_Refunds_Status", "\"Status\" IN ('Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_Refunds_AccountingEntries_AccountingEntryId",
                        column: x => x.AccountingEntryId,
                        principalTable: "AccountingEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_CashRegisters_CashRegisterId",
                        column: x => x.CashRegisterId,
                        principalTable: "CashRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundApplications", x => x.Id);
                    table.CheckConstraint("CK_RefundApplications_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_RefundApplications_Invoices_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundApplications_Refunds_RefundId",
                        column: x => x.RefundId,
                        principalTable: "Refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_CreatedAt_Id",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundApplications_CreditNoteId",
                table: "RefundApplications",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundApplications_RefundId_CreditNoteId",
                table: "RefundApplications",
                columns: new[] { "RefundId", "CreditNoteId" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_AccountingEntryId",
                table: "Refunds",
                column: "AccountingEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CashRegisterId",
                table: "Refunds",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentId",
                table: "Refunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RecordedByUserId",
                table: "Refunds",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundDate",
                table: "Refunds",
                column: "RefundDate");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundNumber",
                table: "Refunds",
                column: "RefundNumber",
                unique: true,
                filter: "NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundApplications");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_CreatedAt_Id",
                table: "CashMovements");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_MovementDate_CreatedAt",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "MovementDate", "CreatedAt" });
        }
    }
}
