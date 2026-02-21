using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class ProjectMember
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    [Required]
    public ProjectRole Role { get; set; } = ProjectRole.Member;

    [Required]
    public AccessSource Source { get; set; } = AccessSource.Direct;

    public DateTime JoinedAt { get; set; } = DateTime.Now;

    [MaxLength(200)]
    public string? InvitedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}
