using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Flow.Models;

public class Expense
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Expense Type (Mandatory - links to ExpenseType table)
    [Required]
    public int ExpenseTypeId { get; set; }
    public ExpenseType? ExpenseType { get; set; }

    // Amount (Mandatory, renamed from InvoiceAmount)
    [Required]
    public decimal Amount { get; set; } = 0;

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    // Invoice Details (Optional based on HasInvoice flag)
    public bool HasInvoice { get; set; } = false;

    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    // Billing Period (Optional)
    public DateTime? BillingPeriodStart { get; set; }
    
    public DateTime? BillingPeriodEnd { get; set; }

    // Invoice File Storage
    public Guid? InvoiceFileGuid { get; set; }

    [MaxLength(500)]
    public string? InvoiceFileName { get; set; }

    // Payment Details (All Mandatory)
    [Required]
    public decimal PaymentAmount { get; set; }

    [Required]
    public int PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    [MaxLength(10)]
    public string? PaymentCurrency { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    // Subscription/Vendor (Optional)
    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Computed properties for display
    [NotMapped]
    public bool HasBillingPeriod => BillingPeriodStart.HasValue && BillingPeriodEnd.HasValue;

    [NotMapped]
    public bool HasInvoiceFile => InvoiceFileGuid.HasValue;
}
