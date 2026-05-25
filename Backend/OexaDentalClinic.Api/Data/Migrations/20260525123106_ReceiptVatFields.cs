using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OexaDentalClinic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptVatFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalBeforeVat",
                table: "Receipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "Receipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubtotalBeforeVat",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "Receipts");
        }
    }
}
