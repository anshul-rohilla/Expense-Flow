using System;
using System.ComponentModel.DataAnnotations;

namespace Expense_Flow.Models;

public class ProjectVendor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    public int VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    [MaxLength(200)]
    public string? ProjectSpecificAccountId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
}
