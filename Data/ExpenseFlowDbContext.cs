using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Expense_Flow.Models;

namespace Expense_Flow.Data;

public class ExpenseFlowDbContext : DbContext
{
    // Core entities
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<PaymentMode> PaymentModes { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectGroup> ProjectGroups { get; set; }
    public DbSet<ProjectGroupMapping> ProjectGroupMappings { get; set; }
    public DbSet<ProjectVendor> ProjectVendors { get; set; }
    public DbSet<ProjectSubscription> ProjectSubscriptions { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ExpenseType> ExpenseTypes { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<SettlementItem> SettlementItems { get; set; }

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

        // ── Organization ──
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        // ── OrganizationMember ──
        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.OrganizationMemberships)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.OrganizationId, e.ContactId }).IsUnique();
        });

        // ── Contact ──
        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Email);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Contacts)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Vendor ──
        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Vendors)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Vendors)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Subscription ──
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Subscriptions)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.Subscriptions)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PaymentMode ──
        modelBuilder.Entity<PaymentMode>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.PaymentModes)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.PaymentModes)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Project ──
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsDefault);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Projects)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DefaultPaymentMode)
                .WithMany(p => p.ProjectsWithDefaultPaymentMode)
                .HasForeignKey(e => e.DefaultPaymentModeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── ProjectGroup ──
        modelBuilder.Entity<ProjectGroup>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });

        // ── ProjectGroupMapping ──
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

        // ── ProjectVendor ──
        modelBuilder.Entity<ProjectVendor>(entity =>
        {
            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProjectVendors)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.ProjectVendors)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectId, e.VendorId }).IsUnique();
        });

        // ── ProjectSubscription ──
        modelBuilder.Entity<ProjectSubscription>(entity =>
        {
            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProjectSubscriptions)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.ProjectSubscriptions)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectId, e.SubscriptionId }).IsUnique();
        });

        // ── ProjectMember ──
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.ProjectMemberships)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectId, e.ContactId }).IsUnique();
        });

        // ── Expense ──
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.InvoiceDate);
            entity.HasIndex(e => e.PaymentDate);
            entity.HasIndex(e => e.VendorId);
            entity.HasIndex(e => e.ReimbursementStatus);

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Expenses)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PaymentMode)
                .WithMany(pm => pm.Expenses)
                .HasForeignKey(e => e.PaymentModeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.Expenses)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.Expenses)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ExpenseType)
                .WithMany()
                .HasForeignKey(e => e.ExpenseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PaidBy)
                .WithMany(c => c.PaidExpenses)
                .HasForeignKey(e => e.PaidById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── ExpenseType ──
        modelBuilder.Entity<ExpenseType>(entity =>
        {
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.ExpenseTypes)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Settlement ──
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.HasIndex(e => e.ContactId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Settlements)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany(c => c.Settlements)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PaymentMode)
                .WithMany(pm => pm.Settlements)
                .HasForeignKey(e => e.PaymentModeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── SettlementItem ──
        modelBuilder.Entity<SettlementItem>(entity =>
        {
            entity.HasOne(e => e.Settlement)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.SettlementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Expense)
                .WithMany(ex => ex.SettlementItems)
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SettlementId, e.ExpenseId }).IsUnique();
        });

        SeedDefaultData(modelBuilder);
    }

    private void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Default Organization
        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = 1,
                Name = "Personal",
                Description = "Default personal organization",
                DefaultCurrency = "INR",
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );

        // Default Contact (current user placeholder)
        modelBuilder.Entity<Contact>().HasData(
            new Contact
            {
                Id = 1,
                OrganizationId = 1,
                Name = "Owner",
                Role = ContactRole.TeamMember,
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );

        // Default OrgMember
        modelBuilder.Entity<OrganizationMember>().HasData(
            new OrganizationMember
            {
                Id = 1,
                OrganizationId = 1,
                ContactId = 1,
                Role = OrgRole.Owner,
                JoinedAt = new DateTime(2026, 1, 1),
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );

        // Default Project
        modelBuilder.Entity<Project>().HasData(
            new Project
            {
                Id = 1,
                OrganizationId = 1,
                Name = "Personal",
                Description = "Default personal expenses project",
                IsDefault = true,
                Currency = "INR",
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );

        // Default ProjectMember (owner on default project)
        modelBuilder.Entity<ProjectMember>().HasData(
            new ProjectMember
            {
                Id = 1,
                ProjectId = 1,
                ContactId = 1,
                Role = ProjectRole.Owner,
                Source = AccessSource.Organization,
                JoinedAt = new DateTime(2026, 1, 1),
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );

        // Seed default expense types
        modelBuilder.Entity<ExpenseType>().HasData(
            new ExpenseType { Id = 1, OrganizationId = 1, Name = "Food & Dining", Emoji = "\U0001F354", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 2, OrganizationId = 1, Name = "Transportation", Emoji = "\U0001F697", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 3, OrganizationId = 1, Name = "Shopping", Emoji = "\U0001F6D2", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 4, OrganizationId = 1, Name = "Entertainment", Emoji = "\U0001F3AC", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 5, OrganizationId = 1, Name = "Healthcare", Emoji = "\U0001F3E5", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 6, OrganizationId = 1, Name = "Education", Emoji = "\U0001F4DA", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 7, OrganizationId = 1, Name = "Utilities", Emoji = "\U0001F4A1", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 8, OrganizationId = 1, Name = "IT & Software", Emoji = "\U0001F4BB", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 9, OrganizationId = 1, Name = "Mobile & Internet", Emoji = "\U0001F4F1", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 10, OrganizationId = 1, Name = "OTT & Streaming", Emoji = "\U0001F4FA", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 11, OrganizationId = 1, Name = "Fitness & Sports", Emoji = "\U0001F3CB", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 12, OrganizationId = 1, Name = "Travel", Emoji = "\u2708\uFE0F", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 13, OrganizationId = 1, Name = "Insurance", Emoji = "\U0001F6E1\uFE0F", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) },
            new ExpenseType { Id = 14, OrganizationId = 1, Name = "Other", Emoji = "\U0001F4E6", IsDefault = true, CreatedAt = new DateTime(2026, 1, 1) }
        );
    }
}
