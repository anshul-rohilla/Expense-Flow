using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Expense_Flow.Models;

public class Vendor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Website { get; set; }

    [MaxLength(200)]
    public string? AccountReference { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int? ContactId { get; set; }
    public Contact? Contact { get; set; }

    public bool IsArchived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<ProjectVendor> ProjectVendors { get; set; } = new List<ProjectVendor>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    // Computed
    [NotMapped]
    public decimal TotalExpenses => Expenses?.Sum(e => e.Amount) ?? 0;

    [NotMapped]
    public int ProjectCount => ProjectVendors?.Count ?? 0;
}
