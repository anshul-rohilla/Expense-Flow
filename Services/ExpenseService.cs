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
    private readonly IUserService _userService;

    public ExpenseService(
        IRepository<Expense> expenseRepository,
        IRepository<Project> projectRepository,
        IUserService userService)
    {
        _expenseRepository = expenseRepository;
        _projectRepository = projectRepository;
        _userService = userService;
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

            existingExpense.ProjectId = expense.ProjectId;
            existingExpense.Name = expense.Name;
            existingExpense.Description = expense.Description;
            existingExpense.Type = expense.Type;
            existingExpense.InvoiceNumber = expense.InvoiceNumber;
            existingExpense.InvoiceDate = expense.InvoiceDate;
            existingExpense.InvoiceAmount = expense.InvoiceAmount;
            existingExpense.BillingPeriod = expense.BillingPeriod;
            existingExpense.InvoiceCurrency = expense.InvoiceCurrency;
            existingExpense.PaymentModeId = expense.PaymentModeId;
            existingExpense.PaymentCurrency = expense.PaymentCurrency;
            existingExpense.PaymentDate = expense.PaymentDate;
            existingExpense.PaymentAmount = expense.PaymentAmount;
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

            var total = expenses.Sum(e => e.InvoiceAmount);
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

        errors.AddRange(ValidationHelper.ValidateRequired(expense.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.Name, 300, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.Description, 2000, "Description"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.Type, 100, "Type"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.InvoiceNumber, 100, "Invoice Number"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.BillingPeriod, 100, "Billing Period"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.InvoiceCurrency, 10, "Invoice Currency"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(expense.PaymentCurrency, 10, "Payment Currency"));

        if (expense.InvoiceAmount < 0)
        {
            errors.Add("Invoice Amount must be a positive value.");
        }

        errors.AddRange(ValidationHelper.ValidatePositive(expense.PaymentAmount, "Payment Amount"));

        if (expense.InvoiceDate.HasValue && expense.InvoiceDate.Value > DateTime.Now)
        {
            errors.Add("Invoice Date cannot be in the future.");
        }

        if (expense.PaymentDate.HasValue && expense.PaymentDate.Value > DateTime.Now)
        {
            errors.Add("Payment Date cannot be in the future.");
        }

        var projectExists = await _projectRepository.ExistsAsync(p => p.Id == expense.ProjectId);
        if (!projectExists)
        {
            errors.Add("Invalid Project ID. Project does not exist.");
        }

        return errors;
    }
}
