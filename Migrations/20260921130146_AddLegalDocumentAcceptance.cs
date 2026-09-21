using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PersonalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalDocumentAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLegalAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLegalAcceptances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLegalAcceptances_LegalDocuments_LegalDocumentId",
                        column: x => x.LegalDocumentId,
                        principalTable: "LegalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLegalAcceptances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LegalDocuments",
                columns: new[] { "Id", "CreatedAt", "EffectiveDate", "IsCurrent", "Title", "Type", "Version" },
                values: new object[,]
                {
                    { new Guid("3d116ce4-8d55-4a45-93ce-1d1b5a77c101"), new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Utc), true, "PhilaLink Terms of Use", "TermsOfUse", "1.0" },
                    { new Guid("83a6a26b-281b-4fd8-92ce-a1887cafc102"), new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Utc), true, "PhilaLink Privacy Policy", "PrivacyPolicy", "1.0" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_Type_IsCurrent",
                table: "LegalDocuments",
                columns: new[] { "Type", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_Type_Version",
                table: "LegalDocuments",
                columns: new[] { "Type", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalAcceptances_LegalDocumentId",
                table: "UserLegalAcceptances",
                column: "LegalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalAcceptances_UserId",
                table: "UserLegalAcceptances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegalAcceptances_UserId_LegalDocumentId",
                table: "UserLegalAcceptances",
                columns: new[] { "UserId", "LegalDocumentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLegalAcceptances");

            migrationBuilder.DropTable(
                name: "LegalDocuments");
        }
    }
}
