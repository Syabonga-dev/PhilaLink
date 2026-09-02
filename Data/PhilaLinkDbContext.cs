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

        public DbSet<User> Users { get; set; }

        public DbSet<Clinic> Clinics { get; set; }

        public DbSet<Medication> Medications { get; set; }

        public DbSet<MedicationSchedule> MedicationSchedules { get; set; }

        public DbSet<MedicationLog> MedicationLogs { get; set; }

        public DbSet<SymptomAssessment> SymptomAssessments { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<ProxyLink> ProxyLinks { get; set; }

        public DbSet<OtpVerification> OtpVerifications { get; set; }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Nurse> Nurses { get; set; }

        public DbSet<Proxy> Proxies { get; set; }

        public DbSet<Admin> Admins { get; set; }

        public DbSet<Allergy> Allergies { get; set; }

        public DbSet<MedicalCondition> MedicalConditions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =====================================================
            // USER
            // =====================================================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role);


            // =====================================================
            // USER → PATIENT
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.UserId)
                .IsUnique();


            // =====================================================
            // USER → NURSE
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Nurse)
                .WithOne(n => n.User)
                .HasForeignKey<Nurse>(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Nurse>()
                .HasIndex(n => n.UserId)
                .IsUnique();


            // =====================================================
            // USER → PROXY
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Proxy)
                .WithOne(p => p.User)
                .HasForeignKey<Proxy>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Proxy>()
                .HasIndex(p => p.UserId)
                .IsUnique();


            // =====================================================
            // USER → ADMIN
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Admin)
                .WithOne(a => a.User)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.UserId)
                .IsUnique();


            // =====================================================
            // PATIENT → ALLERGIES
            // =====================================================

            modelBuilder.Entity<Allergy>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Allergies)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // PATIENT → MEDICAL CONDITIONS
            // =====================================================

            modelBuilder.Entity<MedicalCondition>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.MedicalConditions)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // NURSE → CLINIC
            // =====================================================

            modelBuilder.Entity<Nurse>()
                .HasOne(n => n.Clinic)
                .WithMany()
                .HasForeignKey(n => n.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // PATIENT → MEDICATIONS
            // =====================================================

            modelBuilder.Entity<Medication>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // MEDICATION → SCHEDULES
            // =====================================================

            modelBuilder.Entity<MedicationSchedule>()
                .HasOne(s => s.Medication)
                .WithMany(m => m.Schedules)
                .HasForeignKey(s => s.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // MEDICATION → LOGS
            // =====================================================

            modelBuilder.Entity<MedicationLog>()
                .HasOne(l => l.Medication)
                .WithMany(m => m.Logs)
                .HasForeignKey(l => l.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // PATIENT → SYMPTOM ASSESSMENTS
            // =====================================================

            modelBuilder.Entity<SymptomAssessment>()
                .HasOne(sa => sa.Patient)
                .WithMany()
                .HasForeignKey(sa => sa.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SymptomAssessment>()
                .HasIndex(sa => sa.PatientId);


            // =====================================================
            // PATIENT → PROXY LINKS
            // =====================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.Patient)
                .WithMany(p => p.ProxyLinksAsPatient)
                .HasForeignKey(pl => pl.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // PROXY → PROXY LINKS
            // =====================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.Proxy)
                .WithMany(p => p.ProxyLinksAsProxy)
                .HasForeignKey(pl => pl.ProxyId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // NURSE → PROXY LINKS
            // =====================================================

            modelBuilder.Entity<ProxyLink>()
                .HasOne(pl => pl.AssignedByNurse)
                .WithMany()
                .HasForeignKey(pl => pl.AssignedByNurseId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // USER → NOTIFICATIONS
            // =====================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);


            // =====================================================
            // USER → OTP VERIFICATIONS
            // =====================================================

            modelBuilder.Entity<OtpVerification>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => o.UserId);

            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => o.ExpiryTime);


            // =====================================================
            // USER → AUDIT LOGS
            // =====================================================

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}