using Expense_Flow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Expense_Flow.Services;

public interface IExpenseTypeService
{
    Task<ServiceResult<IEnumerable<ExpenseType>>> GetAllExpenseTypesAsync();
    Task<ServiceResult<IEnumerable<ExpenseType>>> GetExpenseTypesByOrganizationAsync(int organizationId);
    Task<ServiceResult<ExpenseType>> GetExpenseTypeByIdAsync(int id);
    Task<ServiceResult<ExpenseType>> CreateExpenseTypeAsync(ExpenseType expenseType);
    Task<ServiceResult<ExpenseType>> UpdateExpenseTypeAsync(ExpenseType expenseType);
    Task<ServiceResult<bool>> DeleteExpenseTypeAsync(int id);
}
