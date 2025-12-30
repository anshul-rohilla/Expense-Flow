using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Expense_Flow.Models;

namespace Expense_Flow.Data;

public class ExpenseFlowDbContext : DbContext
{
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<PaymentMode> PaymentModes { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectGroup> ProjectGroups { get; set; }
    public DbSet<ProjectGroupMapping> ProjectGroupMappings { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ExpenseType> ExpenseTypes { get; set; }

    public ExpenseFlowDbContext(DbContextOptions<ExpenseFlowDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ExpenseFlow",
                "expenseflow.db"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentMode>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasOne(e => e.Contact)
                .WithMany(c => c.PaymentModes)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsDefault);
            entity.HasOne(e => e.DefaultPaymentMode)
                .WithMany(p => p.ProjectsWithDefaultPaymentMode)
                .HasForeignKey(e => e.DefaultPaymentModeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProjectGroup>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<ProjectGroupMapping>(entity =>
        {
            entity.HasOne(e => e.ProjectGroup)
                .WithMany(pg => pg.ProjectGroupMappings)
                .HasForeignKey(e => e.ProjectGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProjectGroupMappings)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectGroupId, e.ProjectId }).IsUnique();
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.InvoiceDate);
            entity.HasIndex(e => e.PaymentDate);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Expenses)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PaymentMode)
                .WithMany(pm => pm.Expenses)
                .HasForeignKey(e => e.PaymentModeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.Expenses)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExpenseType>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        SeedDefaultData(modelBuilder);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>().HasData(
            new Project
            {
                Id = 1,
                Name = "Personal",
                Description = "Default personal expenses project",
                IsDefault = true,
                CreatedAt = DateTime.Now
            }
        );

        // Seed default expense types
        modelBuilder.Entity<ExpenseType>().HasData(
            new ExpenseType { Id = 1, Name = "Food & Dining", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 2, Name = "Transportation", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 3, Name = "Shopping", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 4, Name = "Entertainment", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 5, Name = "Healthcare", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 6, Name = "Education", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 7, Name = "Utilities", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 8, Name = "IT & Software", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 9, Name = "Mobile & Internet", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 10, Name = "OTT & Streaming", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 11, Name = "Fitness & Sports", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 12, Name = "Travel", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 13, Name = "Insurance", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now },
            new ExpenseType { Id = 14, Name = "Other", Emoji = "", IsDefault = true, CreatedAt = DateTime.Now }
        );
    }
}
