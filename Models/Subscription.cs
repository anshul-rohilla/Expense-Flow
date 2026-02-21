using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Flow.Models;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Plan { get; set; }

    public decimal? Amount { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    [Required]
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public DateTime? StartDate { get; set; }

    public DateTime? RenewalDate { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? Reference { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation
    public ICollection<ProjectSubscription> ProjectSubscriptions { get; set; } = new List<ProjectSubscription>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    // Computed
    [NotMapped]
    public string DisplayBillingCycle => BillingCycle switch
    {
        BillingCycle.Monthly => "Monthly",
        BillingCycle.Quarterly => "Quarterly",
        BillingCycle.HalfYearly => "Half-Yearly",
        BillingCycle.Yearly => "Yearly",
        BillingCycle.OneTime => "One-Time",
        BillingCycle.Custom => "Custom",
        _ => BillingCycle.ToString()
    };

    [NotMapped]
    public string DisplayAmount => Amount.HasValue ? $"{Currency ?? "INR"} {Amount.Value:N2}" : "N/A";

    [NotMapped]
    public string DisplayNameWithVendor => Vendor != null
        ? $"{Name} ({Vendor.Name})"
        : !string.IsNullOrEmpty(_vendorNameCache)
            ? $"{Name} ({_vendorNameCache})"
            : Name;

    // Cache for vendor name when navigation property isn't loaded
    [NotMapped]
    private string _vendorNameCache = string.Empty;

    public void SetVendorName(string vendorName)
    {
        _vendorNameCache = vendorName;
    }
}
