using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Expense_Flow.Models;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public int? DefaultPaymentModeId { get; set; }
    public PaymentMode? DefaultPaymentMode { get; set; }

    // Monthly budget (renamed from Budget for clarity)
    public decimal? MonthlyBudget { get; set; }

    public bool IsArchived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<ProjectGroupMapping> ProjectGroupMappings { get; set; } = new List<ProjectGroupMapping>();

    // Computed properties for analytics
    [NotMapped]
    public decimal AnnualBudget => (MonthlyBudget ?? 0) * 12;

    [NotMapped]
    public decimal TotalExpenses => Expenses?.Sum(e => e.Amount) ?? 0;

    [NotMapped]
    public decimal CurrentMonthExpenses
    {
        get
        {
            var now = DateTime.Now;
            return Expenses?
                .Where(e => e.InvoiceDate.HasValue && 
                           e.InvoiceDate.Value.Year == now.Year && 
                           e.InvoiceDate.Value.Month == now.Month)
                .Sum(e => e.Amount) ?? 0;
        }
    }

    [NotMapped]
    public decimal LastMonthExpenses
    {
        get
        {
            var lastMonth = DateTime.Now.AddMonths(-1);
            return Expenses?
                .Where(e => e.InvoiceDate.HasValue && 
                           e.InvoiceDate.Value.Year == lastMonth.Year && 
                           e.InvoiceDate.Value.Month == lastMonth.Month)
                .Sum(e => e.Amount) ?? 0;
        }
    }

    [NotMapped]
    public decimal CurrentYearExpenses
    {
        get
        {
            var now = DateTime.Now;
            return Expenses?
                .Where(e => e.InvoiceDate.HasValue && e.InvoiceDate.Value.Year == now.Year)
                .Sum(e => e.Amount) ?? 0;
        }
    }

    [NotMapped]
    public decimal MonthlyBudgetRemaining => (MonthlyBudget ?? 0) - CurrentMonthExpenses;

    [NotMapped]
    public decimal AnnualBudgetRemaining => AnnualBudget - CurrentYearExpenses;

    [NotMapped]
    public double MonthlyBudgetUsagePercentage
    {
        get
        {
            if (MonthlyBudget == null || MonthlyBudget == 0) return 0;
            return (double)(CurrentMonthExpenses / MonthlyBudget.Value) * 100;
        }
    }

    [NotMapped]
    public double AnnualBudgetUsagePercentage
    {
        get
        {
            if (AnnualBudget == 0) return 0;
            return (double)(CurrentYearExpenses / AnnualBudget) * 100;
        }
    }

    // Date range filtering methods
    public decimal GetExpensesForDateRange(DateTime startDate, DateTime endDate)
    {
        return Expenses?
            .Where(e => e.InvoiceDate.HasValue &&
                       e.InvoiceDate.Value >= startDate &&
                       e.InvoiceDate.Value <= endDate)
            .Sum(e => e.Amount) ?? 0;
    }

    public IEnumerable<Expense> GetExpensesByDateRange(DateTime startDate, DateTime endDate)
    {
        return Expenses?
            .Where(e => e.InvoiceDate.HasValue &&
                       e.InvoiceDate.Value >= startDate &&
                       e.InvoiceDate.Value <= endDate)
            .OrderByDescending(e => e.InvoiceDate) ?? Enumerable.Empty<Expense>();
    }
}
