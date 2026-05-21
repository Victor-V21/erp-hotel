using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictOnDeleteCaiInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_CAIs_CAIId",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_CAIs_CAIId",
                table: "Invoices",
                column: "CAIId",
                principalTable: "CAIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_CAIs_CAIId",
                table: "Invoices");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_CAIs_CAIId",
                table: "Invoices",
                column: "CAIId",
                principalTable: "CAIs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

