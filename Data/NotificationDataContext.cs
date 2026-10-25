
// App #1 - Atlas TaskFlow - Notification Data Context

using Atlas_TaskFlow_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas_TaskFlow_App.Data
{
    public class NotificationDataContext : DbContext
    {
        public NotificationDataContext(
            DbContextOptions<NotificationDataContext> options)
            : base(options)
        {
        }

        public DbSet<AppNotification> Notifications
            => Set<AppNotification>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppNotification>(entity =>
            {
                entity.ToTable("Notifications", "dbo");

                entity.HasKey(n => n.NotificationId);

                entity.Property(n => n.NotificationId)
                    .ValueGeneratedOnAdd();

                entity.Property(n => n.EventKey)
                    .HasMaxLength(160)
                    .IsRequired();

                entity.HasIndex(n => n.EventKey)
                    .IsUnique();

                entity.Property(n => n.Type)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(n => n.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(n => n.Message)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(n => n.IsRead)
                    .HasDefaultValue(false);

                entity.Property(n => n.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");
            });
        }
    }
}
