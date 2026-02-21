using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class Organization
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(10)]
    public string DefaultCurrency { get; set; } = "INR";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<PaymentMode> PaymentModes { get; set; } = new List<PaymentMode>();
    public ICollection<ExpenseType> ExpenseTypes { get; set; } = new List<ExpenseType>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}
