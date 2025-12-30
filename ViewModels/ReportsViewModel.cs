using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public enum ReportType
{
    ByProject,
    ByProjectGroup,
    ByPaymentMode,
    ByContact,
    Overall
}

public partial class ReportsViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IProjectService _projectService;
    private readonly IProjectGroupService _projectGroupService;
    private readonly IPaymentModeService _paymentModeService;
    private readonly IContactService _contactService;

    [ObservableProperty]
    private ReportType _selectedReportType = ReportType.Overall;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private ObservableCollection<object> _reportData = new();

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private decimal _averageAmount;

    public ReportsViewModel(
        IExpenseService expenseService,
        IProjectService projectService,
        IProjectGroupService projectGroupService,
        IPaymentModeService paymentModeService,
        IContactService contactService)
    {
        _expenseService = expenseService;
        _projectService = projectService;
        _projectGroupService = projectGroupService;
        _paymentModeService = paymentModeService;
        _contactService = contactService;

        // Set default date range to this month
        StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        EndDate = DateTime.Now;
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            switch (SelectedReportType)
            {
                case ReportType.ByProject:
                    await GenerateProjectReportAsync();
                    break;
                case ReportType.ByProjectGroup:
                    await GenerateProjectGroupReportAsync();
                    break;
                case ReportType.ByPaymentMode:
                    await GeneratePaymentModeReportAsync();
                    break;
                case ReportType.ByContact:
                    await GenerateContactReportAsync();
                    break;
                case ReportType.Overall:
                    await GenerateOverallReportAsync();
                    break;
            }
        }, "Generating report...");
    }

    private async Task GenerateProjectReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var groupedData = expensesResult.Data
                .GroupBy(e => e.ProjectId)
                .Select(g => new
                {
                    ProjectId = g.Key,
                    ProjectName = g.First().Project?.Name ?? "Unknown",
                    TotalAmount = g.Sum(e => e.InvoiceAmount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount);

            ReportData = new ObservableCollection<object>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateProjectGroupReportAsync()
    {
        // Implementation will group by project groups
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GeneratePaymentModeReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var groupedData = expensesResult.Data
                .Where(e => e.PaymentModeId.HasValue)
                .GroupBy(e => e.PaymentModeId)
                .Select(g => new
                {
                    PaymentModeId = g.Key,
                    PaymentModeName = g.First().PaymentMode?.Name ?? "Unknown",
                    TotalAmount = g.Sum(e => e.InvoiceAmount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount);

            ReportData = new ObservableCollection<object>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateContactReportAsync()
    {
        // Implementation will group by contacts through payment modes
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateOverallReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var summaryData = new[]
            {
                new { Category = "Total Expenses", Value = expensesResult.Data.Sum(e => e.InvoiceAmount) },
                new { Category = "Transaction Count", Value = (decimal)expensesResult.Data.Count() },
                new { Category = "Average Amount", Value = expensesResult.Data.Any() ? expensesResult.Data.Average(e => e.InvoiceAmount) : 0 }
            };

            ReportData = new ObservableCollection<object>(summaryData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private void CalculateSummary(IEnumerable<Expense> expenses)
    {
        TotalAmount = expenses.Sum(e => e.InvoiceAmount);
        TransactionCount = expenses.Count();
        AverageAmount = TransactionCount > 0 ? TotalAmount / TransactionCount : 0;
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        // Will be implemented for CSV/Excel/PDF export
        await Task.CompletedTask;
    }
}
