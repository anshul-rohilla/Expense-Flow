using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class OrganizationMember
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    [Required]
    public OrgRole Role { get; set; } = OrgRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.Now;

    [MaxLength(200)]
    public string? InvitedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}
