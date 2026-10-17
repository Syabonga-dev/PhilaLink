using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalProject.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationLogs_MedicationId",
                table: "MedicationLogs");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyLinks_PatientId_IsActive_AssignedAt",
                table: "ProxyLinks",
                columns: new[] { "PatientId", "IsActive", "AssignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProxyLinks_ProxyId_IsActive",
                table: "ProxyLinks",
                columns: new[] { "ProxyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpVerifications_UserId_Purpose_CreatedAt",
                table: "OtpVerifications",
                columns: new[] { "UserId", "Purpose", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_MedicationId_TakenAt",
                table: "MedicationLogs",
                columns: new[] { "MedicationId", "TakenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_TakenAt",
                table: "MedicationLogs",
                column: "TakenAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCollections_ClinicId_ScheduledCollectionDate",
                table: "MedicationCollections",
                columns: new[] { "ClinicId", "ScheduledCollectionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCollections_ClinicId_Status_CollectedAt",
                table: "MedicationCollections",
                columns: new[] { "ClinicId", "Status", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCollections_PatientId_ScheduledCollectionDate",
                table: "MedicationCollections",
                columns: new[] { "PatientId", "ScheduledCollectionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationCollections_ScheduledCollectionDate",
                table: "MedicationCollections",
                column: "ScheduledCollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ClinicId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "ClinicId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_ScheduledAt",
                table: "Appointments",
                columns: new[] { "ClinicId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_Status",
                table: "Appointments",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_ScheduledAt",
                table: "Appointments",
                columns: new[] { "PatientId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledAt",
                table: "Appointments",
                column: "ScheduledAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProxyLinks_PatientId_IsActive_AssignedAt",
                table: "ProxyLinks");

            migrationBuilder.DropIndex(
                name: "IX_ProxyLinks_ProxyId_IsActive",
                table: "ProxyLinks");

            migrationBuilder.DropIndex(
                name: "IX_OtpVerifications_UserId_Purpose_CreatedAt",
                table: "OtpVerifications");

            migrationBuilder.DropIndex(
                name: "IX_MedicationLogs_MedicationId_TakenAt",
                table: "MedicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicationLogs_TakenAt",
                table: "MedicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicationCollections_ClinicId_ScheduledCollectionDate",
                table: "MedicationCollections");

            migrationBuilder.DropIndex(
                name: "IX_MedicationCollections_ClinicId_Status_CollectedAt",
                table: "MedicationCollections");

            migrationBuilder.DropIndex(
                name: "IX_MedicationCollections_PatientId_ScheduledCollectionDate",
                table: "MedicationCollections");

            migrationBuilder.DropIndex(
                name: "IX_MedicationCollections_ScheduledCollectionDate",
                table: "MedicationCollections");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ClinicId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId_ScheduledAt",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId_Status",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId_ScheduledAt",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ScheduledAt",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_MedicationId",
                table: "MedicationLogs",
                column: "MedicationId");
        }
    }
}