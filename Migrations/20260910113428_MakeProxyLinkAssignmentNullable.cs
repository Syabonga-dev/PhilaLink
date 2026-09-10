using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalProject.Migrations
{
    /// <inheritdoc />
    public partial class MakeProxyLinkAssignmentNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedByNurseId",
                table: "ProxyLinks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "AssignedByAdminId",
                table: "ProxyLinks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProxyLinks_AssignedByAdminId",
                table: "ProxyLinks",
                column: "AssignedByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProxyLinks_Admins_AssignedByAdminId",
                table: "ProxyLinks",
                column: "AssignedByAdminId",
                principalTable: "Admins",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProxyLinks_Admins_AssignedByAdminId",
                table: "ProxyLinks");

            migrationBuilder.DropIndex(
                name: "IX_ProxyLinks_AssignedByAdminId",
                table: "ProxyLinks");

            migrationBuilder.DropColumn(
                name: "AssignedByAdminId",
                table: "ProxyLinks");

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignedByNurseId",
                table: "ProxyLinks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
