using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class ProjectGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<ProjectGroupMapping> ProjectGroupMappings { get; set; } = new List<ProjectGroupMapping>();
}
