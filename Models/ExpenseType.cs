using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class ExpenseType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Emoji { get; set; } = "??";

    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public string DisplayText => $"{Emoji} {Name}";
}
