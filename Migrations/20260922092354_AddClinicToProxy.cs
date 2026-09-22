using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalProject.Migrations
{
    public partial class AddClinicToProxy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =================================================
            // 1. ADD CLINIC ID AS TEMPORARILY NULLABLE
            // =================================================

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicId",
                table: "Proxies",
                type: "uuid",
                nullable: true
            );

            // =================================================
            // 2. BACKFILL FROM LEGITIMATE PATIENT RELATIONSHIP
            // =================================================
            //
            // Existing seeded Proxies already have historical
            // ProxyLinks to their legitimate Patients.
            //
            // Some of those links may now be inactive.
            //
            // Therefore:
            //
            // - Active legitimate links are preferred.
            // - Inactive legitimate historical links may be used.
            // - Load-test patients are completely ignored.
            //
            // Load-test accounts use:
            //
            // proxytest...@philalink.local
            //
            // =================================================

            migrationBuilder.Sql(
                                """
                    UPDATE "Proxies" AS proxy

                    SET "ClinicId" =
                    (
                        SELECT
                            patient."ClinicId"

                        FROM "ProxyLinks" AS link

                        INNER JOIN "Patients" AS patient
                            ON patient."Id" =
                                link."PatientId"

                        INNER JOIN "Users" AS patient_user
                            ON patient_user."Id" =
                                patient."UserId"

                        WHERE
                            link."ProxyId" =
                                proxy."Id"

                            AND patient."ClinicId"
                                IS NOT NULL

                            AND patient_user."Email"
                                NOT LIKE
                                'proxytest%@philalink.local'

                        ORDER BY
                            link."IsActive" DESC,
                            link."AssignedAt" DESC,
                            link."Id"

                        LIMIT 1
                    )

                    WHERE
                        proxy."ClinicId"
                            IS NULL;
                    """
            );

            // =================================================
            // 3. EXPLICITLY PROTECT PROXY01
            // =================================================
            //
            // proxy01@philalink.test was linked to 500
            // artificial patients spread across 50 clinics.
            //
            // Her legitimate relationship is:
            //
            // Patient:
            // patient01@philalink.test
            //
            // Clinic:
            // Johannesburg Community Health Clinic
            //
            // =================================================

            migrationBuilder.Sql(
                """
                UPDATE "Proxies" AS proxy

                SET "ClinicId" =
                    'a52d9222-0021-1e20-be62-16ca5fae5b19'::uuid

                FROM "Users" AS users

                WHERE
                    proxy."UserId" =
                        users."Id"

                    AND users."Email" =
                        'proxy01@philalink.test';
                """
            );

            // =================================================
            // 4. SAFETY CHECK
            // =================================================
            //
            // Do NOT assign an arbitrary clinic.
            //
            // If any Proxy still has no clinic after examining
            // legitimate historical relationships, abort the
            // migration instead.
            //
            // =================================================

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN

                    IF EXISTS
                    (
                        SELECT 1

                        FROM "Proxies"

                        WHERE
                            "ClinicId"
                                IS NULL
                    )
                    THEN

                        RAISE EXCEPTION
                            'Cannot migrate Proxies: one or more Proxy accounts could not be assigned to a legitimate clinic.';

                    END IF;

                END
                $$;
                """
            );

            // =================================================
            // 5. MAKE CLINIC ID REQUIRED
            // =================================================

            migrationBuilder.AlterColumn<Guid>(
                name: "ClinicId",
                table: "Proxies",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true
            );

            // =================================================
            // 6. INDEX
            // =================================================

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_ClinicId",
                table: "Proxies",
                column: "ClinicId"
            );

            // =================================================
            // 7. FOREIGN KEY
            // =================================================

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_Clinics_ClinicId",
                table: "Proxies",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        protected override void Down(
            MigrationBuilder migrationBuilder
        )
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_Clinics_ClinicId",
                table: "Proxies"
            );

            migrationBuilder.DropIndex(
                name: "IX_Proxies_ClinicId",
                table: "Proxies"
            );

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Proxies"
            );
        }
    }
}