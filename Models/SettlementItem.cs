using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class SettlementItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SettlementId { get; set; }
    public Settlement Settlement { get; set; } = null!;

    [Required]
    public int ExpenseId { get; set; }
    public Expense Expense { get; set; } = null!;

    [Required]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
