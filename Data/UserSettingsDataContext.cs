
// App #1 - Atlas TaskFlow - User Settings DbContext

using Atlas_TaskFlow_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Data
{
    public class UserSettingsDataContext : DbContext
    {
        public UserSettingsDataContext(
            DbContextOptions<UserSettingsDataContext> options)
            : base(options)
        {
        }

        public DbSet<UserSettings> UserSettings
            => Set<UserSettings>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSettings>(entity =>
            {
                entity.ToTable("UserSettings", "dbo");

                entity.HasKey(x => x.UserId);

                entity.Property(x => x.UserId)
                    .HasMaxLength(450);

                entity.Property(x => x.FullName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.StudentId)
                    .HasMaxLength(50);

                entity.Property(x => x.University)
                    .HasMaxLength(150);

                entity.Property(x => x.Major)
                    .HasMaxLength(100);

                entity.Property(x => x.AcademicYear)
                    .HasMaxLength(30);

                entity.Property(x => x.ExpectedGraduation)
                    .HasColumnType("date");

                entity.Property(x => x.TimeZone)
                    .HasMaxLength(100);

                entity.Property(x => x.Bio)
                    .HasMaxLength(500);


                entity.Property(x => x.EmailAlerts)
                    .HasDefaultValue(true)
                    .ValueGeneratedNever();

                entity.Property(x => x.TaskReminders)
                    .HasDefaultValue(true)
                    .ValueGeneratedNever();

                entity.Property(x => x.TeamUpdates)
                    .HasDefaultValue(true)
                    .ValueGeneratedNever();

                entity.Property(x => x.WeeklySummary)
                    .HasDefaultValue(false)
                    .ValueGeneratedNever();


                entity.Property(x => x.UpdatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");
            });
        }
    }
}
