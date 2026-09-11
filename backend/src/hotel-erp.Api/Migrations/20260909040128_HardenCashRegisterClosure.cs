using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenCashRegisterClosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "CashRegisters"
                        WHERE length("Name") > 50
                           OR ("Description" IS NOT NULL AND length("Description") > 250)
                    ) THEN
                        RAISE EXCEPTION 'Hay cajas cuyo nombre o descripción excede los nuevos límites; corríjalas antes de aplicar HardenCashRegisterClosure.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "CashRegisters"
                        WHERE NOT "IsDeleted"
                        GROUP BY "Name"
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Hay cajas activas con nombres duplicados; deben reconciliarse antes de aplicar HardenCashRegisterClosure.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM "CashMovements"
                        WHERE "Amount" < 0
                           OR "BalanceAfter" < 0
                           OR "Amount" <> round("Amount", 2)
                           OR "BalanceAfter" <> round("BalanceAfter", 2)
                           OR abs("Amount") > 9999999999999999.99
                           OR abs("BalanceAfter") > 9999999999999999.99
                    ) THEN
                        RAISE EXCEPTION 'Hay movimientos de caja negativos, con más de dos decimales o fuera de numeric(18,2); deben corregirse antes de aplicar HardenCashRegisterClosure.';
                    END IF;
                END $migration$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId",
                table: "CashMovements");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CashRegisters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CashRegisters",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceAfter",
                table: "CashMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CashMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "CountedAmount",
                table: "CashMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                table: "CashMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedAmount",
                table: "CashMovements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CashMovements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                WITH sequenced AS (
                    SELECT
                        "Id",
                        "Amount",
                        lag("BalanceAfter") OVER (
                            PARTITION BY "CashRegisterId"
                            ORDER BY "MovementDate", "CreatedAt", "Id"
                        ) AS "PreviousBalance"
                    FROM "CashMovements"
                )
                UPDATE "CashMovements" AS movement
                SET
                    "ExpectedAmount" = COALESCE(sequenced."PreviousBalance", 0),
                    "CountedAmount" = sequenced."Amount",
                    "Difference" = round(sequenced."Amount" - COALESCE(sequenced."PreviousBalance", 0), 2),
                    "Notes" = 'Cierre histórico migrado desde una versión sin observaciones estructuradas.',
                    "Amount" = 0
                FROM sequenced
                WHERE movement."Id" = sequenced."Id"
                  AND movement."MovementType" = 'Cierre';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_Name",
                table: "CashRegisters",
                column: "Name",
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId_MovementDate_CreatedAt",
                table: "CashMovements",
                columns: new[] { "CashRegisterId", "MovementDate", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CashMovements_Amount_NonNegative",
                table: "CashMovements",
                sql: "\"Amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CashMovements_Balance_NonNegative",
                table: "CashMovements",
                sql: "\"BalanceAfter\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "CashMovements"
                SET "Amount" = "CountedAmount"
                WHERE "MovementType" = 'Cierre'
                  AND "CountedAmount" IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_Name",
                table: "CashRegisters");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_CashRegisterId_MovementDate_CreatedAt",
                table: "CashMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CashMovements_Amount_NonNegative",
                table: "CashMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CashMovements_Balance_NonNegative",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "CountedAmount",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "Difference",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "ExpectedAmount",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CashMovements");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CashRegisters",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CashRegisters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceAfter",
                table: "CashMovements",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CashMovements",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashRegisterId",
                table: "CashMovements",
                column: "CashRegisterId");
        }
    }
}
