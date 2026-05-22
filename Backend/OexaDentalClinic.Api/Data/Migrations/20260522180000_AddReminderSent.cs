using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OexaDentalClinic.Api.Data.Migrations
{
    public partial class AddReminderSent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReminderSent",
                table: "Appointments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReminderSent", table: "Appointments");
        }
    }
}
