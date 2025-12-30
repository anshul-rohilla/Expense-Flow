using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class Contact
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<PaymentMode> PaymentModes { get; set; } = new List<PaymentMode>();
}
