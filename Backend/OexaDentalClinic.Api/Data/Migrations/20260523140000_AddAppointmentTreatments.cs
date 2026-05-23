using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OexaDentalClinic.Api.Data.Migrations
{
    public partial class AddAppointmentTreatments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentTreatments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", "IdentityColumn"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    ProblemKey = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    AssignedDentistUserId = table.Column<int>(type: "int", nullable: true),
                    ScheduledStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentTreatments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentTreatments_AppointmentId",
                table: "AppointmentTreatments",
                column: "AppointmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppointmentTreatments");
        }
    }
}
