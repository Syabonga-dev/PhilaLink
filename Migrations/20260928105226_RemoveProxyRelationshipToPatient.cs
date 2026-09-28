using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProxyRelationshipToPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationshipToPatient",
                table: "Proxies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelationshipToPatient",
                table: "Proxies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
