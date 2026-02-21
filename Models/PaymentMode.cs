using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense_Flow.Models;

public class PaymentMode
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public PaymentModeType Type { get; set; }

    [Required]
    public FundSource FundSource { get; set; } = FundSource.Company;

    [Required]
    public PaymentModeScope Scope { get; set; } = PaymentModeScope.Organization;

    public int? OwnerId { get; set; }
    public Contact? Owner { get; set; }

    public int? ContactId { get; set; }
    public Contact? Contact { get; set; }

    [MaxLength(50)]
    public string? CardType { get; set; }

    [MaxLength(4)]
    public string? LastFourDigits { get; set; }

    public decimal? Balance { get; set; }

    [MaxLength(200)]
    public string? UpiId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    public ICollection<Project> ProjectsWithDefaultPaymentMode { get; set; } = new List<Project>();

    // Computed properties for display
    [NotMapped]
    public string DisplayBrand => Type switch
    {
        PaymentModeType.Card => CardType ?? "CARD",
        PaymentModeType.UPI => "UPI",
        PaymentModeType.Cash => "CASH",
        _ => Type.ToString().ToUpper()
    };

    [NotMapped]
    public string DisplayNumber => Type switch
    {
        PaymentModeType.Card => $"XXXX  XXXX  XXXX {LastFourDigits ?? "****"}",
        PaymentModeType.UPI => UpiId ?? "Not Set",
        PaymentModeType.Cash => Balance.HasValue ? $"{Balance.Value:N2}" : "0.00",
        _ => "N/A"
    };

    public bool RequiresSettlement { get; set; } = false;

    [NotMapped]
    public string HolderLabel => $"{Type.ToString().ToUpper()} HOLDER";
}
