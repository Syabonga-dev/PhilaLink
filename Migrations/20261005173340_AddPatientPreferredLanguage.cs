using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientPreferredLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "PatientPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "PatientPreferences");
        }
    }
}
