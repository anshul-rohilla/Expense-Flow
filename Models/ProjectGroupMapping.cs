using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class ProjectGroupMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectGroupId { get; set; }
    public ProjectGroup ProjectGroup { get; set; } = null!;

    [Required]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}
