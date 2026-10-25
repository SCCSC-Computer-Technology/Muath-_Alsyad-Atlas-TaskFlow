
// App #1 - Atlas TaskFlow - Database Context

using Microsoft.EntityFrameworkCore;
using Atlas_TaskFlow_App.Models;

namespace Atlas_TaskFlow_App.Data
{
    public class TaskFlowDbContext : DbContext
    {
        public TaskFlowDbContext(
            DbContextOptions<TaskFlowDbContext> options
        ) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }

        public DbSet<TaskItem> Tasks { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Projects table
            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projects", "dbo");

                entity.HasKey(p => p.ProjectId);

                entity.Property(p => p.ProjectId)
                      .ValueGeneratedOnAdd();

                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(p => p.Description)
                      .HasMaxLength(500);

                entity.Property(p => p.StartDate)
                      .HasColumnType("date");

                entity.Property(p => p.DueDate)
                      .HasColumnType("date");

                entity.Property(p => p.Status)
                      .IsRequired()
                      .HasMaxLength(30);
            });

            // Tasks table
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.ToTable("Tasks", "dbo");

                entity.HasKey(t => t.TaskId);

                entity.Property(t => t.TaskId)
                      .ValueGeneratedOnAdd();

                entity.Property(t => t.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(t => t.Description)
                      .HasMaxLength(1000);

                entity.Property(t => t.DueDate)
                      .HasColumnType("date");

                entity.Property(t => t.Priority)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(t => t.Status)
                      .IsRequired()
                      .HasMaxLength(30);

                entity.HasOne(t => t.Project)
                      .WithMany(p => p.Tasks)
                      .HasForeignKey(t => t.ProjectId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
