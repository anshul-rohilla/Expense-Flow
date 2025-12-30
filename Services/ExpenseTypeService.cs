using Expense_Flow.Data;
using Expense_Flow.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Expense_Flow.Services;

public class ExpenseTypeService : IExpenseTypeService
{
    private readonly IRepository<ExpenseType> _repository;

    public ExpenseTypeService(IRepository<ExpenseType> repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IEnumerable<ExpenseType>>> GetAllExpenseTypesAsync()
    {
        var expenseTypes = await _repository.GetAllAsync();
        return ServiceResult<IEnumerable<ExpenseType>>.SuccessResult(
            expenseTypes.OrderBy(et => et.Name).ToList());
    }

    public async Task<ServiceResult<ExpenseType>> GetExpenseTypeByIdAsync(int id)
    {
        var expenseType = await _repository.GetByIdAsync(id);
        if (expenseType == null)
        {
            return ServiceResult<ExpenseType>.FailureResult("Expense type not found.");
        }

        return ServiceResult<ExpenseType>.SuccessResult(expenseType);
    }

    public async Task<ServiceResult<ExpenseType>> CreateExpenseTypeAsync(ExpenseType expenseType)
    {
        var validationErrors = await ValidateExpenseTypeAsync(expenseType);
        if (validationErrors.Any())
        {
            return ServiceResult<ExpenseType>.FailureResult(validationErrors);
        }

        await _repository.AddAsync(expenseType);
        return ServiceResult<ExpenseType>.SuccessResult(expenseType);
    }

    public async Task<ServiceResult<ExpenseType>> UpdateExpenseTypeAsync(ExpenseType expenseType)
    {
        var existing = await _repository.GetByIdAsync(expenseType.Id);
        if (existing == null)
        {
            return ServiceResult<ExpenseType>.FailureResult("Expense type not found.");
        }

        var validationErrors = await ValidateExpenseTypeAsync(expenseType, expenseType.Id);
        if (validationErrors.Any())
        {
            return ServiceResult<ExpenseType>.FailureResult(validationErrors);
        }

        await _repository.UpdateAsync(expenseType);
        return ServiceResult<ExpenseType>.SuccessResult(expenseType);
    }

    public async Task<ServiceResult<bool>> DeleteExpenseTypeAsync(int id)
    {
        var expenseType = await _repository.GetByIdAsync(id);
        if (expenseType == null)
        {
            return ServiceResult<bool>.FailureResult("Expense type not found.");
        }

        if (expenseType.IsDefault)
        {
            return ServiceResult<bool>.FailureResult("Cannot delete default expense types.");
        }

        await _repository.DeleteAsync(expenseType);
        return ServiceResult<bool>.SuccessResult(true);
    }

    private async Task<List<string>> ValidateExpenseTypeAsync(ExpenseType expenseType, int? excludeId = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(expenseType.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(expenseType.Emoji))
        {
            errors.Add("Emoji is required.");
        }

        // Check for duplicate name
        var allTypes = await _repository.GetAllAsync();
        var duplicate = allTypes.FirstOrDefault(et =>
            et.Name.Equals(expenseType.Name, System.StringComparison.OrdinalIgnoreCase) &&
            et.Id != excludeId);

        if (duplicate != null)
        {
            errors.Add($"An expense type with the name '{expenseType.Name}' already exists.");
        }

        return errors;
    }
}
