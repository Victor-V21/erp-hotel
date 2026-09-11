using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCardSettlementLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BankDepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WithholdingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SettlementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountingEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSettlements", x => x.Id);
                    table.CheckConstraint("CK_CardSettlements_Components_NonNegative", "\"BankDepositAmount\" >= 0 AND \"CommissionAmount\" >= 0 AND \"WithholdingAmount\" >= 0");
                    table.CheckConstraint("CK_CardSettlements_Components_Total", "\"BankDepositAmount\" + \"CommissionAmount\" + \"WithholdingAmount\" = \"GrossAmount\"");
                    table.CheckConstraint("CK_CardSettlements_Currency_HNL", "\"Currency\" = 'HNL'");
                    table.CheckConstraint("CK_CardSettlements_GrossAmount_Positive", "\"GrossAmount\" > 0");
                    table.CheckConstraint("CK_CardSettlements_Reference", "length(btrim(\"ExternalReference\")) >= 3");
                    table.CheckConstraint("CK_CardSettlements_Status", "\"Status\" IN ('Confirmado', 'Anulado')");
                    table.ForeignKey(
                        name: "FK_CardSettlements_AccountingEntries_AccountingEntryId",
                        column: x => x.AccountingEntryId,
                        principalTable: "AccountingEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardSettlements_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardSettlementApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CardSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardSettlementApplications", x => x.Id);
                    table.CheckConstraint("CK_CardSettlementApplications_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_CardSettlementApplications_CardSettlements_CardSettlementId",
                        column: x => x.CardSettlementId,
                        principalTable: "CardSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardSettlementApplications_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlementApplications_CardSettlementId_PaymentId",
                table: "CardSettlementApplications",
                columns: new[] { "CardSettlementId", "PaymentId" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlementApplications_PaymentId",
                table: "CardSettlementApplications",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlements_AccountingEntryId",
                table: "CardSettlements",
                column: "AccountingEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlements_ExternalReference",
                table: "CardSettlements",
                column: "ExternalReference",
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlements_RecordedByUserId",
                table: "CardSettlements",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlements_SettlementDate",
                table: "CardSettlements",
                column: "SettlementDate");

            migrationBuilder.CreateIndex(
                name: "IX_CardSettlements_SettlementNumber",
                table: "CardSettlements",
                column: "SettlementNumber",
                unique: true,
                filter: "NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardSettlementApplications");

            migrationBuilder.DropTable(
                name: "CardSettlements");
        }
    }
}
