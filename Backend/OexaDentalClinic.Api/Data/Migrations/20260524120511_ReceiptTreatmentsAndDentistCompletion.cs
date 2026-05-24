using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace OexaDentalClinic.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptTreatmentsAndDentistCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubmittedByDentistUserId",
                table: "ReceiptMedications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DentistCompletedAt",
                table: "AppointmentTreatments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceiptTreatments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    AppointmentTreatmentId = table.Column<int>(type: "int", nullable: true),
                    ProblemKey = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DentistUserId = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptTreatments", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptTreatments");

            migrationBuilder.DropColumn(
                name: "SubmittedByDentistUserId",
                table: "ReceiptMedications");

            migrationBuilder.DropColumn(
                name: "DentistCompletedAt",
                table: "AppointmentTreatments");
        }
    }
}
