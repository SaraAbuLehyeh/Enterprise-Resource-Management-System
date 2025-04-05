using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ERMS.Models;
using System;

namespace ERMS.Data
{
    /// <summary>
    /// Represents the database context for the Enterprise Resource Management System.
    /// This class serves as the primary interface between the application and the database.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext class.
        /// </summary>
        /// <param name="options">The options to configure the database context.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the Departments table.
        /// </summary>
        public DbSet<Department> Departments { get; set; }

        /// <summary>
        /// Gets or sets the Projects table.
        /// </summary>
        public DbSet<Project> Projects { get; set; }

        /// <summary>
        /// Gets or sets the Tasks table.
        /// </summary>
        public DbSet<ProjectTask> Tasks { get; set; } // Note: Renamed from Task to ProjectTask to avoid conflict with System.Threading.Tasks.Task

        /// <summary>
        /// Configures the database model.
        /// </summary>
        /// <param name="modelBuilder">The model builder to configure the database model.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity mappings to database tables
            modelBuilder.Entity<Department>().ToTable("Departments");
            modelBuilder.Entity<Project>().ToTable("Projects");
            modelBuilder.Entity<ProjectTask>().ToTable("Tasks");

            // Configure relationships and constraints
            modelBuilder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany(u => u.ManagedProjects)
                .HasForeignKey(p => p.ManagerID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectID);

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure required fields and other constraints
            modelBuilder.Entity<Department>()
                .Property(d => d.DepartmentName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Project>()
                .Property(p => p.ProjectName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<ProjectTask>()
                .Property(t => t.TaskName)
                .IsRequired()
                .HasMaxLength(100);

            // Configure indexes for better query performance
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Project>()
                .HasIndex(p => p.StartDate);

            modelBuilder.Entity<ProjectTask>()
                .HasIndex(t => t.DueDate);
        }
    }
}
