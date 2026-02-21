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
    ByVendor,
    ByExpenseType,
    Overall
}

public class ReportItem
{
    public string Label { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}

public partial class ReportsViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly IProjectService _projectService;
    private readonly IProjectGroupService _projectGroupService;
    private readonly IPaymentModeService _paymentModeService;
    private readonly IContactService _contactService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ReportType _selectedReportType = ReportType.Overall;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private ObservableCollection<ReportItem> _reportData = new();

    [ObservableProperty]
    private string _totalAmountFormatted = string.Empty;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private int _transactionCount;

    [ObservableProperty]
    private string _averageAmountFormatted = string.Empty;

    [ObservableProperty]
    private decimal _averageAmount;

    [ObservableProperty]
    private string _highestExpenseLabel = string.Empty;

    [ObservableProperty]
    private string _highestExpenseAmountFormatted = string.Empty;

    [ObservableProperty]
    private string _lowestExpenseLabel = string.Empty;

    [ObservableProperty]
    private string _lowestExpenseAmountFormatted = string.Empty;

    [ObservableProperty]
    private string _mostFrequentCategory = string.Empty;

    [ObservableProperty]
    private int _mostFrequentCategoryCount;

    [ObservableProperty]
    private string _dateRangeLabel = string.Empty;

    public ReportsViewModel(
        IExpenseService expenseService,
        IProjectService projectService,
        IProjectGroupService projectGroupService,
        IPaymentModeService paymentModeService,
        IContactService contactService,
        ISettingsService settingsService)
    {
        _expenseService = expenseService;
        _projectService = projectService;
        _projectGroupService = projectGroupService;
        _paymentModeService = paymentModeService;
        _contactService = contactService;
        _settingsService = settingsService;

        // Set default date range to this month
        StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        EndDate = DateTime.Now;
        
        // Initialize formatted values using settings currency
        FormatZero = _settingsService.FormatCurrency(0);
        TotalAmountFormatted = FormatZero;
        AverageAmountFormatted = FormatZero;
    }

    public string FormatZero { get; }

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
                case ReportType.ByVendor:
                    await GenerateVendorReportAsync();
                    break;
                case ReportType.ByExpenseType:
                    await GenerateExpenseTypeReportAsync();
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
            var total = expensesResult.Data.Sum(e => e.Amount);
            var groupedData = expensesResult.Data
                .GroupBy(e => e.ProjectId)
                .Select(g => new ReportItem
                {
                    Label = g.First().Project?.Name ?? "Unknown",
                    Amount = g.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(g.Sum(e => e.Amount)),
                    Count = g.Count(),
                    Percentage = total > 0 ? (double)(g.Sum(e => e.Amount) / total * 100) : 0
                })
                .OrderByDescending(x => x.Amount);

            ReportData = new ObservableCollection<ReportItem>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateProjectGroupReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            // Get all groups and their project mappings
            var groupsResult = await _projectGroupService.GetAllProjectGroupsAsync();
            var groups = groupsResult.Success && groupsResult.Data != null ? groupsResult.Data.ToList() : new List<ProjectGroup>();
            
            // Build project-to-group lookup
            var projectGroupMap = new Dictionary<int, string>();
            foreach (var group in groups)
            {
                var projectsInGroup = await _projectGroupService.GetProjectsInGroupAsync(group.Id);
                if (projectsInGroup.Success && projectsInGroup.Data != null)
                {
                    foreach (var project in projectsInGroup.Data)
                    {
                        projectGroupMap[project.Id] = group.Name;
                    }
                }
            }

            var groupedData = expensesResult.Data
                .GroupBy(e => projectGroupMap.TryGetValue(e.ProjectId, out var groupName) ? groupName : "Ungrouped")
                .Select(g => new ReportItem
                {
                    Label = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(g.Sum(e => e.Amount)),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Amount);

            ReportData = new ObservableCollection<ReportItem>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GeneratePaymentModeReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var total = expensesResult.Data.Sum(e => e.Amount);
            var groupedData = expensesResult.Data
                .GroupBy(e => e.PaymentModeId)
                .Select(g => new ReportItem
                {
                    Label = g.First().PaymentMode?.Name ?? "Unknown",
                    Amount = g.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(g.Sum(e => e.Amount)),
                    Count = g.Count(),
                    Percentage = total > 0 ? (double)(g.Sum(e => e.Amount) / total * 100) : 0
                })
                .OrderByDescending(x => x.Amount);

            ReportData = new ObservableCollection<ReportItem>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateVendorReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var total = expensesResult.Data.Sum(e => e.Amount);
            var groupedData = expensesResult.Data
                .GroupBy(e => e.VendorId ?? 0)
                .Select(g => new ReportItem
                {
                    Label = g.First().Vendor?.Name ?? (g.Key == 0 ? "No Vendor" : "Unknown"),
                    Amount = g.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(g.Sum(e => e.Amount)),
                    Count = g.Count(),
                    Percentage = total > 0 ? (double)(g.Sum(e => e.Amount) / total * 100) : 0
                })
                .OrderByDescending(x => x.Amount);

            ReportData = new ObservableCollection<ReportItem>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateExpenseTypeReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var total = expensesResult.Data.Sum(e => e.Amount);
            var groupedData = expensesResult.Data
                .GroupBy(e => e.ExpenseTypeId)
                .Select(g => new ReportItem
                {
                    Label = g.First().ExpenseType?.Name ?? "Unknown",
                    Amount = g.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(g.Sum(e => e.Amount)),
                    Count = g.Count(),
                    Percentage = total > 0 ? (double)(g.Sum(e => e.Amount) / total * 100) : 0
                })
                .OrderByDescending(x => x.Amount);

            ReportData = new ObservableCollection<ReportItem>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateContactReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            // Group by vendor (who was paid)
            var groupedData = expensesResult.Data
                .GroupBy(e => e.VendorId ?? 0)
                .Select(g => new ReportItem
                {
                    Label = g.First().Vendor?.Name ?? (g.Key == 0 ? "No Vendor" : "Unknown"),
                    Amount = g.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(g.Sum(e => e.Amount)),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Amount);

            ReportData = new ObservableCollection<ReportItem>(groupedData);
            CalculateSummary(expensesResult.Data);
        }
    }

    private async Task GenerateOverallReportAsync()
    {
        var expensesResult = await _expenseService.GetExpensesByDateRangeAsync(StartDate, EndDate);
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var items = new List<ReportItem>
            {
                new ReportItem
                {
                    Label = "Total Expenses",
                    Amount = expensesResult.Data.Sum(e => e.Amount),
                    AmountFormatted = _settingsService.FormatCurrency(expensesResult.Data.Sum(e => e.Amount)),
                    Count = expensesResult.Data.Count()
                },
                new ReportItem
                {
                    Label = "Transaction Count",
                    Amount = expensesResult.Data.Count(),
                    AmountFormatted = expensesResult.Data.Count().ToString(),
                    Count = expensesResult.Data.Count()
                },
                new ReportItem
                {
                    Label = "Average Amount",
                    Amount = expensesResult.Data.Any() ? expensesResult.Data.Average(e => e.Amount) : 0,
                    AmountFormatted = _settingsService.FormatCurrency(
                        expensesResult.Data.Any() ? expensesResult.Data.Average(e => e.Amount) : 0),
                    Count = expensesResult.Data.Count()
                }
            };

            ReportData = new ObservableCollection<ReportItem>(items);
            CalculateSummary(expensesResult.Data);
        }
    }

    private void CalculateSummary(IEnumerable<Expense> expenses)
    {
        var expenseList = expenses.ToList();
        TotalAmount = expenseList.Sum(e => e.Amount);
        TransactionCount = expenseList.Count;
        AverageAmount = TransactionCount > 0 ? TotalAmount / TransactionCount : 0;
        
        TotalAmountFormatted = _settingsService.FormatCurrency(TotalAmount);
        AverageAmountFormatted = _settingsService.FormatCurrency(AverageAmount);

        // Additional insights
        if (expenseList.Any())
        {
            var highest = expenseList.OrderByDescending(e => e.Amount).First();
            HighestExpenseLabel = highest.Name;
            HighestExpenseAmountFormatted = _settingsService.FormatCurrency(highest.Amount);

            var lowest = expenseList.OrderBy(e => e.Amount).First();
            LowestExpenseLabel = lowest.Name;
            LowestExpenseAmountFormatted = _settingsService.FormatCurrency(lowest.Amount);

            var topCategory = expenseList
                .GroupBy(e => e.ExpenseType?.Name ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .First();
            MostFrequentCategory = topCategory.Key;
            MostFrequentCategoryCount = topCategory.Count();
        }
        else
        {
            HighestExpenseLabel = "N/A";
            HighestExpenseAmountFormatted = FormatZero;
            LowestExpenseLabel = "N/A";
            LowestExpenseAmountFormatted = FormatZero;
            MostFrequentCategory = "N/A";
            MostFrequentCategoryCount = 0;
        }

        // Date range label
        if (StartDate.HasValue && EndDate.HasValue)
        {
            DateRangeLabel = $"{StartDate.Value:MMM dd, yyyy} — {EndDate.Value:MMM dd, yyyy}";
        }
        else
        {
            DateRangeLabel = "All Time";
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        // Will be implemented for CSV/Excel/PDF export
        await Task.CompletedTask;
    }
}
