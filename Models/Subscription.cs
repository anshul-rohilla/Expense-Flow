using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Type { get; set; }

    [MaxLength(200)]
    public string? Reference { get; set; }

    public int? ContactId { get; set; }
    public Contact? Contact { get; set; }

    // Flag to distinguish vendors from subscriptions
    public bool IsVendor { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
