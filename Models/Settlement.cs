using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Expense_Flow.Models;

public class Settlement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string Reference { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    public int? PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "INR";

    [Required]
    public DateTime SettlementDate { get; set; } = DateTime.Now;

    [Required]
    public SettlementStatus Status { get; set; } = SettlementStatus.Draft;

    [MaxLength(200)]
    public string? TransactionReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation
    public ICollection<SettlementItem> Items { get; set; } = new List<SettlementItem>();

    // Computed
    [NotMapped]
    public decimal ItemsTotal => Items?.Sum(i => i.Amount) ?? 0;

    [NotMapped]
    public int ExpenseCount => Items?.Count ?? 0;

    [NotMapped]
    public string StatusDisplay => Status switch
    {
        SettlementStatus.Draft => "Draft",
        SettlementStatus.Completed => "Completed",
        SettlementStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };

    [NotMapped]
    public string DisplayAmount => $"{Currency} {TotalAmount:N2}";
}
