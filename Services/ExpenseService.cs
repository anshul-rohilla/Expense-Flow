using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;

namespace Expense_Flow.Services;

public interface IExpenseService
{
    Task<ServiceResult<IEnumerable<Expense>>> GetAllExpensesAsync();
    Task<ServiceResult<IEnumerable<Expense>>> GetExpensesByProjectAsync(int projectId);
    Task<ServiceResult<IEnumerable<Expense>>> GetExpensesByDateRangeAsync(DateTime? startDate, DateTime? endDate);
    Task<ServiceResult<Expense>> GetExpenseByIdAsync(int id);
    Task<ServiceResult<Expense>> CreateExpenseAsync(Expense expense);
    Task<ServiceResult<Expense>> UpdateExpenseAsync(Expense expense);
    Task<ServiceResult<bool>> DeleteExpenseAsync(int id);
    Task<ServiceResult<decimal>> GetTotalExpensesAsync(int? projectId = null, DateTime? startDate = null, DateTime? endDate = null);
}

public class ExpenseService : IExpenseService
{
    private readonly IRepository<Expense> _expenseRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<Subscription> _subscriptionRepository;
    private readonly IRepository<ExpenseType> _expenseTypeRepository;
    private readonly IUserService _userService;
    private readonly IFileStorageService _fileStorageService;

    public ExpenseService(
        IRepository<Expense> expenseRepository,
        IRepository<Project> projectRepository,
        IRepository<Subscription> subscriptionRepository,
        IRepository<ExpenseType> expenseTypeRepository,
        IUserService userService,
        IFileStorageService fileStorageService)
    {
        _expenseRepository = expenseRepository;
        _projectRepository = projectRepository;
        _subscriptionRepository = subscriptionRepository;
        _expenseTypeRepository = expenseTypeRepository;
        _userService = userService;
        _fileStorageService = fileStorageService;
    }

    public async Task<ServiceResult<IEnumerable<Expense>>> GetAllExpensesAsync()
    {
        try
        {
            var expenses = await _expenseRepository.GetAllAsync();
            return ServiceResult<IEnumerable<Expense>>.SuccessResult(
                expenses.OrderByDescending(e => e.InvoiceDate ?? e.CreatedAt));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Expense>>.FailureResult($"Error retrieving expenses: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Expense>>> GetExpensesByProjectAsync(int projectId)
    {
        try
        {
            var expenses = await _expenseRepository.FindAsync(e => e.ProjectId == projectId);
            return ServiceResult<IEnumerable<Expense>>.SuccessResult(
                expenses.OrderByDescending(e => e.InvoiceDate ?? e.CreatedAt));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Expense>>.FailureResult($"Error retrieving expenses: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Expense>>> GetExpensesByDateRangeAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var expenses = await _expenseRepository.GetAllAsync();
            
            if (startDate.HasValue)
            {
                expenses = expenses.Where(e =>
                    (e.InvoiceDate.HasValue && e.InvoiceDate.Value >= startDate.Value) ||
                    (!e.InvoiceDate.HasValue && e.CreatedAt >= startDate.Value));
            }

            if (endDate.HasValue)
            {
                expenses = expenses.Where(e =>
                    (e.InvoiceDate.HasValue && e.InvoiceDate.Value <= endDate.Value) ||
                    (!e.InvoiceDate.HasValue && e.CreatedAt <= endDate.Value));
            }

            return ServiceResult<IEnumerable<Expense>>.SuccessResult(
                expenses.OrderByDescending(e => e.InvoiceDate ?? e.CreatedAt));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Expense>>.FailureResult($"Error retrieving expenses: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Expense>> GetExpenseByIdAsync(int id)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null)
            {
                return ServiceResult<Expense>.FailureResult("Expense not found.");
            }
            return ServiceResult<Expense>.SuccessResult(expense);
        }
        catch (Exception ex)
        {
            return ServiceResult<Expense>.FailureResult($"Error retrieving expense: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Expense>> CreateExpenseAsync(Expense expense)
    {
        try
        {
            var validationErrors = await ValidateExpenseAsync(expense);
            if (validationErrors.Any())
            {
                return ServiceResult<Expense>.FailureResult(validationErrors);
            }

            expense.CreatedAt = DateTime.Now;
            expense.CreatedBy = _userService.GetCurrentUsername();
            expense.ModifiedAt = null;
            expense.ModifiedBy = null;

            var createdExpense = await _expenseRepository.AddAsync(expense);
            return ServiceResult<Expense>.SuccessResult(createdExpense, "Expense created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Expense>.FailureResult($"Error creating expense: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Expense>> UpdateExpenseAsync(Expense expense)
    {
        try
        {
            var existingExpense = await _expenseRepository.GetByIdAsync(expense.Id);
            if (existingExpense == null)
            {
                return ServiceResult<Expense>.FailureResult("Expense not found.");
            }

            var validationErrors = await ValidateExpenseAsync(expense);
            if (validationErrors.Any())
            {
                return ServiceResult<Expense>.FailureResult(validationErrors);
            }

            // Update all properties
            existingExpense.ProjectId = expense.ProjectId;
            existingExpense.Name = expense.Name;
            existingExpense.Description = expense.Description;
            existingExpense.ExpenseTypeId = expense.ExpenseTypeId;
            existingExpense.Amount = expense.Amount;
            existingExpense.Currency = expense.Currency;
            existingExpense.HasInvoice = expense.HasInvoice;
            existingExpense.InvoiceNumber = expense.InvoiceNumber;
            existingExpense.InvoiceDate = expense.InvoiceDate;
            existingExpense.BillingPeriodStart = expense.BillingPeriodStart;
            existingExpense.BillingPeriodEnd = expense.BillingPeriodEnd;
            existingExpense.InvoiceFileGuid = expense.InvoiceFileGuid;
            existingExpense.InvoiceFileName = expense.InvoiceFileName;
            existingExpense.PaymentAmount = expense.PaymentAmount;
            existingExpense.PaymentModeId = expense.PaymentModeId;
            existingExpense.PaymentCurrency = expense.PaymentCurrency;
            existingExpense.PaymentDate = expense.PaymentDate;
            existingExpense.SubscriptionId = expense.SubscriptionId;
            existingExpense.ModifiedAt = DateTime.Now;
            existingExpense.ModifiedBy = _userService.GetCurrentUsername();

            var updatedExpense = await _expenseRepository.UpdateAsync(existingExpense);
            return ServiceResult<Expense>.SuccessResult(updatedExpense, "Expense updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Expense>.FailureResult($"Error updating expense: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteExpenseAsync(int id)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null)
            {
                return ServiceResult<bool>.FailureResult("Expense not found.");
            }

            // Delete invoice file if exists
            if (expense.InvoiceFileGuid.HasValue)
            {
                await _fileStorageService.DeleteInvoiceFileAsync(expense.InvoiceFileGuid.Value);
            }

            await _expenseRepository.DeleteAsync(expense);
            return ServiceResult<bool>.SuccessResult(true, "Expense deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting expense: {ex.Message}");
        }
    }

    public async Task<ServiceResult<decimal>> GetTotalExpensesAsync(
        int? projectId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var expenses = await _expenseRepository.GetAllAsync();

            if (projectId.HasValue)
            {
                expenses = expenses.Where(e => e.ProjectId == projectId.Value);
            }

            if (startDate.HasValue)
            {
                expenses = expenses.Where(e =>
                    (e.InvoiceDate.HasValue && e.InvoiceDate.Value >= startDate.Value) ||
                    (!e.InvoiceDate.HasValue && e.CreatedAt >= startDate.Value));
            }

            if (endDate.HasValue)
            {
                expenses = expenses.Where(e =>
                    (e.InvoiceDate.HasValue && e.InvoiceDate.Value <= endDate.Value) ||
                    (!e.InvoiceDate.HasValue && e.CreatedAt <= endDate.Value));
            }

            var total = expenses.Sum(e => e.Amount);
            return ServiceResult<decimal>.SuccessResult(total);
        }
        catch (Exception ex)
        {
            return ServiceResult<decimal>.FailureResult($"Error calculating total expenses: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidateExpenseAsync(Expense expense)
    {
        var errors = new List<string>();

        // Basic validations
        errors.AddRange(ValidationHelper.ValidateRequired(expense.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.Name, 300, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.Description, 2000, "Description"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.Currency, 10, "Currency"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.PaymentCurrency, 10, "Payment Currency"));

        // Amount validation
        if (expense.Amount < 0)
        {
            errors.Add("Amount must be zero or positive.");
        }

        // Payment validations (mandatory)
        if (expense.PaymentAmount <= 0)
        {
            errors.Add("Payment Amount is required and must be greater than zero.");
        }

        if (expense.PaymentModeId <= 0)
        {
            errors.Add("Payment Mode is required.");
        }

        // ExpenseType validation (mandatory)
        if (expense.ExpenseTypeId <= 0)
        {
            errors.Add("Expense Type is required.");
        }
        else
        {
            var expenseTypeExists = await _expenseTypeRepository.ExistsAsync(et => et.Id == expense.ExpenseTypeId);
            if (!expenseTypeExists)
            {
                errors.Add("Invalid Expense Type.");
            }
        }

        // Invoice validations (if HasInvoice is true)
        if (expense.HasInvoice)
        {
            if (string.IsNullOrWhiteSpace(expense.InvoiceNumber))
            {
                errors.Add("Invoice Number is required when invoice details are provided.");
            }

            if (!expense.InvoiceDate.HasValue)
            {
                errors.Add("Invoice Date is required when invoice details are provided.");
            }
        }

        // Billing period validation
        if (expense.BillingPeriodStart.HasValue && expense.BillingPeriodEnd.HasValue)
        {
            if (expense.BillingPeriodStart.Value > expense.BillingPeriodEnd.Value)
            {
                errors.Add("Billing Period Start Date cannot be after End Date.");
            }
        }

        // Date validations
        if (expense.InvoiceDate.HasValue && expense.InvoiceDate.Value > DateTime.Now)
        {
            errors.Add("Invoice Date cannot be in the future.");
        }

        if (expense.PaymentDate > DateTime.Now)
        {
            errors.Add("Payment Date cannot be in the future.");
        }

        // Project validation
        var projectExists = await _projectRepository.ExistsAsync(p => p.Id == expense.ProjectId);
        if (!projectExists)
        {
            errors.Add("Invalid Project. Project does not exist.");
        }

        // Subscription/Vendor type validation
        if (expense.SubscriptionId.HasValue && expense.SubscriptionId.Value > 0)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(expense.SubscriptionId.Value);
            if (subscription != null && expense.ExpenseTypeId > 0)
            {
                var expenseType = await _expenseTypeRepository.GetByIdAsync(expense.ExpenseTypeId);
                if (expenseType != null && !string.IsNullOrEmpty(subscription.Type))
                {
                    if (subscription.Type != expenseType.Name)
                    {
                        errors.Add($"Subscription/Vendor type '{subscription.Type}' does not match Expense Type '{expenseType.Name}'.");
                    }
                }
            }
        }

        return errors;
    }
}
