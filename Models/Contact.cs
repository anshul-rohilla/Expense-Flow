using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class Contact
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [Required]
    public ContactRole Role { get; set; } = ContactRole.TeamMember;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Navigation
    public ICollection<PaymentMode> PaymentModes { get; set; } = new List<PaymentMode>();
    public ICollection<Expense> PaidExpenses { get; set; } = new List<Expense>();
    public ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
