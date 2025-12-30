using System;
using System.ComponentModel.DataAnnotations;

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

    [MaxLength(100)]
    public string? Type { get; set; }

    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    [Required]
    public decimal InvoiceAmount { get; set; }

    [MaxLength(100)]
    public string? BillingPeriod { get; set; }

    [Required]
    [MaxLength(10)]
    public string InvoiceCurrency { get; set; } = "USD";

    public int? PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }

    [MaxLength(10)]
    public string? PaymentCurrency { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? PaymentAmount { get; set; }

    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
