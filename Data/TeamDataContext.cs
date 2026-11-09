
// App #1 - Atlas TaskFlow
// Team Data Context - Database First

using Atlas_TaskFlow_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Data
{
    public class TeamDataContext : DbContext
    {
        public TeamDataContext(
            DbContextOptions<TeamDataContext> options)
            : base(options)
        {
        }

        public DbSet<TeamMember> TeamMembers
            => Set<TeamMember>();

        public DbSet<TaskAssignment> TaskAssignments
            => Set<TaskAssignment>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TeamMembers SQL table
            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.ToTable("TeamMembers", "dbo");

                entity.HasKey(m => m.MemberId);

                entity.Property(m => m.MemberId)
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.FullName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(m => m.Email)
                    .HasMaxLength(256);

                // Preserve the original database index
                entity.HasIndex(m => m.Email)
                    .IsUnique()
                    .HasFilter("[Email] IS NOT NULL");

                entity.Property(m => m.Role)
                    .HasMaxLength(30)
                    .IsRequired()
                    .HasDefaultValue("Member");

                entity.Property(m => m.IsActive)
                    .HasDefaultValue(true);

                entity.Property(m => m.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                // New: Identity ownership
                entity.Property(m => m.OwnerUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasIndex(m => m.OwnerUserId);
            });

            // TaskAssignments SQL table
            modelBuilder.Entity<TaskAssignment>(entity =>
            {
                entity.ToTable("TaskAssignments", "dbo");

                // Only one assignment per task
                entity.HasKey(a => a.TaskId);

                entity.Property(a => a.AssignedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(a => a.Member)
                    .WithMany(m => m.Assignments)
                    .HasForeignKey(a => a.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                // TaskId is linked to dbo.Tasks in SQL.
            });
        }
    }
}
