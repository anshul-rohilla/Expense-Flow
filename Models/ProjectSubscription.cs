using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class ProjectSubscription
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}
