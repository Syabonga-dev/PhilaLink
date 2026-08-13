using Microsoft.EntityFrameworkCore;
using PersonalProject.Models;
using PersonalProject.Models.Entities;

namespace PersonalProject.Data
{
    public class PhilaLinkDbContext : DbContext
    {
        public PhilaLinkDbContext(DbContextOptions<PhilaLinkDbContext> options)
            : base(options)
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



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);



            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();


            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role);



            modelBuilder.Entity<User>()
                .HasOne(u => u.Patient)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<User>()
                .HasOne(u => u.Nurse)
                .WithOne(n => n.User)
                .HasForeignKey<Nurse>(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<User>()
                .HasOne(u => u.Proxy)
                .WithOne(p => p.User)
                .HasForeignKey<Proxy>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<User>()
                .HasOne(u => u.Admin)
                .WithOne(a => a.User)
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<ProxyLink>()
                .HasOne(p => p.Patient)
                .WithMany(u => u.ProxyLinksAsPatient)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<ProxyLink>()
                .HasOne(p => p.Proxy)
                .WithMany(u => u.ProxyLinksAsProxy)
                .HasForeignKey(p => p.ProxyId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<ProxyLink>()
                .HasOne(p => p.AssignedByNurse)
                .WithMany()
                .HasForeignKey(p => p.AssignedByNurseId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<Medication>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<MedicationSchedule>()
                .HasOne(s => s.Medication)
                .WithMany(m => m.Schedules)
                .HasForeignKey(s => s.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<MedicationLog>()
                .HasOne(l => l.Medication)
                .WithMany(m => m.Logs)
                .HasForeignKey(l => l.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<SymptomAssessment>()
                .HasOne(sa => sa.Patient)
                .WithMany()
                .HasForeignKey(sa => sa.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<SymptomAssessment>()
                .HasIndex(sa => sa.PatientId);



            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);



            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);



            modelBuilder.Entity<OtpVerification>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => o.UserId);


            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => o.ExpiryTime);



            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

