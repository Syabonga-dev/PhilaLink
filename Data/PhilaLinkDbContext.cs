using Microsoft.EntityFrameworkCore;
using PersonalProject.Models;
using PersonalProject.Models.Entities;

namespace PersonalProject.Data
{
    public class PhilaLinkDbContext : DbContext
    {
        public PhilaLinkDbContext(DbContextOptions<PhilaLinkDbContext> options) : base(options)
        {
        }

        // =====================================================
        // IDENTITY / PROFILES
        // =====================================================

        public DbSet<User> Users { get; set; }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Nurse> Nurses { get; set; }

        public DbSet<Proxy> Proxies { get; set; }

        public DbSet<Admin> Admins { get; set; }

        // =====================================================
        // CLINICS
        // =====================================================

        public DbSet<Clinic> Clinics { get; set; }

        public DbSet<ClinicStock> ClinicStocks { get; set; }

        // =====================================================
        // PATIENT HEALTH
        // =====================================================

        public DbSet<Allergy> Allergies { get; set; }

        public DbSet<MedicalCondition> MedicalConditions { get; set; }

        public DbSet<SymptomAssessment> SymptomAssessments { get; set; }
        public DbSet<HealthMetric> HealthMetrics { get; set; }

        public DbSet<HealthRecord> HealthRecords { get; set; }

        public DbSet<PatientPreference> PatientPreferences { get; set; }

        // =====================================================
        // APPOINTMENTS
        // =====================================================

        public DbSet<Appointment> Appointments { get; set; }

        // =====================================================
        // MEDICATIONS
        // =====================================================

        public DbSet<Medication> Medications { get; set; }

        public DbSet<MedicationSchedule> MedicationSchedules { get; set; }

        public DbSet<MedicationLog> MedicationLogs { get; set; }

        // =====================================================
        // COLLECTIONS
        // =====================================================

        public DbSet<MedicationCollection> MedicationCollections { get; set; }

        public DbSet<MedicationCollectionItem> MedicationCollectionItems { get; set; }

        // =====================================================
        // PROXY
        // =====================================================

        public DbSet<ProxyLink> ProxyLinks { get; set; }

        // =====================================================
        // COMMUNICATION / SECURITY
        // =====================================================

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<OtpVerification> OtpVerifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =================================================
            // USER
            // =================================================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.IsActive);

            // =================================================
            // USER → PATIENT
            // =================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(
                    p => p.UserId
                )
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // =================================================
            // USER → NURSE
            // =================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Nurse)
                .WithOne(n => n.User)
                .HasForeignKey<Nurse>(
                    n => n.UserId
                )
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nurse>()
                .HasIndex(n => n.UserId)
                .IsUnique();

            // =================================================
            // USER → PROXY
            // =================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Proxy)
                .WithOne(p => p.User)
                .HasForeignKey<Proxy>(
                    p => p.UserId
                )
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Proxy>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // =================================================
            // USER → ADMIN
            // =================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Admin)
                .WithOne(a => a.User)
                .HasForeignKey<Admin>(
                    a => a.UserId
                )
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.UserId)
                .IsUnique();

            // =================================================
            // ADMIN → CLINIC
            // =================================================

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Clinic)
                .WithMany()
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.ClinicId);

            // =================================================
            // PATIENT → CLINIC
            // =================================================

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Clinic)
                .WithMany()
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.ClinicId);

            // =================================================
            // NURSE → CLINIC
            // =================================================

            modelBuilder.Entity<Nurse>()
                .HasOne(n => n.Clinic)
                .WithMany()
                .HasForeignKey(n => n.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nurse>()
                .HasIndex(n => n.ClinicId);

            // =================================================
            // PATIENT → ALLERGIES
            // =================================================

            modelBuilder.Entity<Allergy>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Allergies)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Allergy>()
                .HasIndex(a => a.PatientId);

            // =================================================
            // PATIENT → MEDICAL CONDITIONS
            // =================================================

            modelBuilder.Entity<MedicalCondition>()
                .HasOne(c => c.Patient)
                .WithMany(
                    p => p.MedicalConditions
                )
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicalCondition>()
                .HasIndex(c => c.PatientId);

            // =================================================
            // PATIENT → MEDICATIONS
            // =================================================

            modelBuilder.Entity<Medication>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Medication>()
                .HasIndex(m => m.PatientId);

            // =================================================
            // MEDICATION → SCHEDULES
            // =================================================

            modelBuilder.Entity<MedicationSchedule>()
                .HasOne(s => s.Medication)
                .WithMany(m => m.Schedules)
                .HasForeignKey(
                    s => s.MedicationId
                )
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // MEDICATION → LOGS
            // =================================================

            modelBuilder.Entity<MedicationLog>()
                .HasOne(l => l.Medication)
                .WithMany(m => m.Logs)
                .HasForeignKey(
                    l => l.MedicationId
                )
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // APPOINTMENT → PATIENT
            // =================================================

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.PatientId);

            // =================================================
            // APPOINTMENT → CLINIC
            // =================================================

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Clinic)
                .WithMany()
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.ClinicId);

            // =================================================
            // APPOINTMENT → NURSE
            // =================================================

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Nurse)
                .WithMany()
                .HasForeignKey(a => a.NurseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.NurseId);

            // =================================================
            // PATIENT → SYMPTOM ASSESSMENTS
            // =================================================

            modelBuilder.Entity<SymptomAssessment>()
                .HasOne(sa => sa.Patient)
                .WithMany()
                .HasForeignKey(sa => sa.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SymptomAssessment>()
                .HasIndex(sa => sa.PatientId);

            // =================================================
            // PATIENT → PROXY LINKS
            // =================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.Patient)
                .WithMany(
                    p => p.ProxyLinksAsPatient
                )
                .HasForeignKey(pl => pl.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProxyLink>()
                .HasIndex(pl => pl.PatientId);

            // =================================================
            // PROXY → PROXY LINKS
            // =================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.Proxy)
                .WithMany(
                    p => p.ProxyLinksAsProxy
                )
                .HasForeignKey(pl => pl.ProxyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProxyLink>()
                .HasIndex(pl => pl.ProxyId);

            // Track active/inactive proxy links.
            modelBuilder.Entity<ProxyLink>()
                .HasIndex(pl => pl.IsActive);

            modelBuilder.Entity<ProxyLink>()
                .HasIndex(
                    pl => new
                    {
                        pl.PatientId,
                        pl.ProxyId,
                        pl.IsActive
                    }
                );

            // =================================================
            // NURSE → PROXY LINKS
            // =================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.AssignedByNurse)
                .WithMany()
                .HasForeignKey(
                    pl => pl.AssignedByNurseId
                )
                .OnDelete(DeleteBehavior.Restrict);

            // =================================================
            // ADMIN → PROXY LINKS
            // =================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.AssignedByAdmin)
                .WithMany()
                .HasForeignKey(
                    pl => pl.AssignedByAdminId
                )
                .OnDelete(DeleteBehavior.Restrict);

            // =================================================
            // USER → ENDED PROXY LINKS
            // =================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.EndedByUser)
                .WithMany()
                .HasForeignKey(pl => pl.EndedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // =================================================
            // MEDICATION COLLECTION → PATIENT
            // =================================================

            modelBuilder.Entity<MedicationCollection>()
                .HasOne(c => c.Patient)
                .WithMany()
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicationCollection>()
                .HasIndex(c => c.PatientId);

            // =================================================
            // MEDICATION COLLECTION → CLINIC
            // =================================================

            modelBuilder.Entity<MedicationCollection>()
                .HasOne(c => c.Clinic)
                .WithMany()
                .HasForeignKey(c => c.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicationCollection>()
                .HasIndex(c => c.ClinicId);

            // =================================================
            // MEDICATION COLLECTION → PROXY
            // =================================================

            modelBuilder.Entity<MedicationCollection>()
                .HasOne(c => c.Proxy)
                .WithMany()
                .HasForeignKey(c => c.ProxyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicationCollection>()
                .HasIndex(c => c.ProxyId);

            // =================================================
            // MEDICATION COLLECTION → NURSE
            // =================================================

            modelBuilder.Entity<MedicationCollection>()
                .HasOne(c => c.ProcessedByNurse)
                .WithMany()
                .HasForeignKey(
                    c => c.ProcessedByNurseId
                )
                .OnDelete(DeleteBehavior.Restrict);

            // =================================================
            // COLLECTION ITEM → COLLECTION
            // =================================================

            modelBuilder.Entity<MedicationCollectionItem>()
                .HasOne(i => i.MedicationCollection)
                .WithMany(c => c.Items)
                .HasForeignKey(
                    i => i.MedicationCollectionId
                )
                .OnDelete(DeleteBehavior.Cascade);

            // =================================================
            // COLLECTION ITEM → MEDICATION
            // =================================================

            modelBuilder.Entity<MedicationCollectionItem>()
                .HasOne(i => i.Medication)
                .WithMany()
                .HasForeignKey(i => i.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicationCollectionItem>()
                .HasIndex(
                    i => new
                    {
                        i.MedicationCollectionId,
                        i.MedicationId
                    }
                )
                .IsUnique();

            modelBuilder.Entity<MedicationCollectionItem>()
                .HasOne(i => i.ClinicStock)
                .WithMany()
                .HasForeignKey(i => i.ClinicStockId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MedicationCollectionItem>()
                .HasIndex(i => i.ClinicStockId);

            // =================================================
            // CLINIC STOCK → CLINIC
            // =================================================

            modelBuilder.Entity<ClinicStock>()
                .HasOne(s => s.Clinic)
                .WithMany()
                .HasForeignKey(s => s.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClinicStock>()
                .HasIndex(s => s.ClinicId);

            /*
             * Prevent duplicate stock entries for the same
             * medication/strength/form in one clinic.
             */
            modelBuilder.Entity<ClinicStock>()
                .HasIndex(
                    s => new
                    {
                        s.ClinicId,
                        s.MedicationName,
                        s.Strength,
                        s.Form
                    }
                )
                .IsUnique();

            // =================================================
            // USER → NOTIFICATIONS
            // =================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            // =================================================
            // USER → OTP
            // =================================================

            modelBuilder.Entity<OtpVerification>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => o.UserId);

            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => o.ExpiryTime);

            // =================================================
            // USER → AUDIT LOGS
            // =================================================

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(
                    a => a.PerformedByUserId
                )
                .OnDelete(DeleteBehavior.Restrict);


            // =================================================
            // AUDIT LOG → CLINIC
            // =================================================

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Clinic)
                .WithMany()
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.ClinicId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            // =================================================
            // PATIENT -> HEALTH METRICS
            // =================================================

            modelBuilder.Entity<HealthMetric>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.HealthMetrics)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HealthMetric>()
                .HasIndex(m => new
                {
                    m.PatientId,
                    m.RecordedAt
                });

            // =================================================
            // PATIENT -> HEALTH RECORDS
            // =================================================

            modelBuilder.Entity<HealthRecord>()
                .HasOne(r => r.Patient)
                .WithMany(p => p.HealthRecords)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HealthRecord>()
                .HasOne(r => r.Clinic)
                .WithMany()
                .HasForeignKey(r => r.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HealthRecord>()
                .HasIndex(r => new
                {
                    r.PatientId,
                    r.RecordDate
                });

            // =================================================
            // PATIENT -> PREFERENCES
            // =================================================

            modelBuilder.Entity<PatientPreference>()
                .HasOne(p => p.Patient)
                .WithOne(p => p.Preference)
                .HasForeignKey<PatientPreference>(
                    p => p.PatientId
                )
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientPreference>()
                .HasIndex(p => p.PatientId)
                .IsUnique();

            // =================================================
            // PATIENT NUMBER
            // =================================================

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.PatientNumber)
                .IsUnique();
        }
    }
}