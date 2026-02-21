using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;

namespace Expense_Flow.Services;

public interface ISettlementService
{
    Task<ServiceResult<IEnumerable<Settlement>>> GetAllSettlementsAsync(int organizationId);
    Task<ServiceResult<IEnumerable<Settlement>>> GetSettlementsByContactAsync(int contactId);
    Task<ServiceResult<Settlement>> GetSettlementByIdAsync(int id);
    Task<ServiceResult<Settlement>> CreateSettlementAsync(Settlement settlement, List<int> expenseIds);
    Task<ServiceResult<Settlement>> CompleteSettlementAsync(int settlementId, int? paymentModeId, string? transactionRef);
    Task<ServiceResult<bool>> CancelSettlementAsync(int settlementId);
    Task<ServiceResult<bool>> DeleteSettlementAsync(int id);
    Task<ServiceResult<decimal>> GetPendingReimbursementAsync(int organizationId, int? contactId = null);
    Task<ServiceResult<IEnumerable<Expense>>> GetUnsettledExpensesAsync(int organizationId, int? contactId = null);
}

public class SettlementService : ISettlementService
{
    private readonly IRepository<Settlement> _settlementRepository;
    private readonly IRepository<SettlementItem> _itemRepository;
    private readonly IRepository<Expense> _expenseRepository;
    private readonly IRepository<PaymentMode> _paymentModeRepository;
    private readonly IUserService _userService;

    public SettlementService(
        IRepository<Settlement> settlementRepository,
        IRepository<SettlementItem> itemRepository,
        IRepository<Expense> expenseRepository,
        IRepository<PaymentMode> paymentModeRepository,
        IUserService userService)
    {
        _settlementRepository = settlementRepository;
        _itemRepository = itemRepository;
        _expenseRepository = expenseRepository;
        _paymentModeRepository = paymentModeRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<Settlement>>> GetAllSettlementsAsync(int organizationId)
    {
        try
        {
            var settlements = await _settlementRepository.FindAsync(s => s.OrganizationId == organizationId);
            return ServiceResult<IEnumerable<Settlement>>.SuccessResult(
                settlements.OrderByDescending(s => s.SettlementDate));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Settlement>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Settlement>>> GetSettlementsByContactAsync(int contactId)
    {
        try
        {
            var settlements = await _settlementRepository.FindAsync(s => s.ContactId == contactId);
            return ServiceResult<IEnumerable<Settlement>>.SuccessResult(
                settlements.OrderByDescending(s => s.SettlementDate));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Settlement>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Settlement>> GetSettlementByIdAsync(int id)
    {
        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(id);
            if (settlement == null)
                return ServiceResult<Settlement>.FailureResult("Settlement not found.");
            return ServiceResult<Settlement>.SuccessResult(settlement);
        }
        catch (Exception ex)
        {
            return ServiceResult<Settlement>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Settlement>> CreateSettlementAsync(Settlement settlement, List<int> expenseIds)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settlement.Reference))
                return ServiceResult<Settlement>.FailureResult("Reference is required.");

            if (expenseIds == null || !expenseIds.Any())
                return ServiceResult<Settlement>.FailureResult("At least one expense must be selected.");

            // Validate all expenses exist and are pending reimbursement
            decimal totalAmount = 0;
            var expenses = new List<Expense>();
            foreach (var expenseId in expenseIds)
            {
                var expense = await _expenseRepository.GetByIdAsync(expenseId);
                if (expense == null)
                    return ServiceResult<Settlement>.FailureResult($"Expense #{expenseId} not found.");

                if (expense.ReimbursementStatus == ReimbursementStatus.Settled)
                    return ServiceResult<Settlement>.FailureResult($"Expense '{expense.Name}' is already settled.");

                var pendingAmount = expense.PaymentAmount - expense.ReimbursedAmount;
                totalAmount += pendingAmount;
                expenses.Add(expense);
            }

            settlement.TotalAmount = totalAmount;
            settlement.Status = SettlementStatus.Draft;
            settlement.CreatedAt = DateTime.Now;
            settlement.CreatedBy = _userService.GetCurrentUsername();

            var created = await _settlementRepository.AddAsync(settlement);

            // Create settlement items
            foreach (var expense in expenses)
            {
                var pendingAmount = expense.PaymentAmount - expense.ReimbursedAmount;
                var item = new SettlementItem
                {
                    SettlementId = created.Id,
                    ExpenseId = expense.Id,
                    Amount = pendingAmount,
                    CreatedAt = DateTime.Now
                };
                await _itemRepository.AddAsync(item);
            }

            return ServiceResult<Settlement>.SuccessResult(created, "Settlement created.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Settlement>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Settlement>> CompleteSettlementAsync(int settlementId, int? paymentModeId, string? transactionRef)
    {
        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
                return ServiceResult<Settlement>.FailureResult("Settlement not found.");

            if (settlement.Status != SettlementStatus.Draft)
                return ServiceResult<Settlement>.FailureResult("Only draft settlements can be completed.");

            // Get all items and update expenses
            var items = await _itemRepository.FindAsync(i => i.SettlementId == settlementId);
            foreach (var item in items)
            {
                var expense = await _expenseRepository.GetByIdAsync(item.ExpenseId);
                if (expense != null)
                {
                    expense.ReimbursedAmount += item.Amount;
                    if (expense.ReimbursedAmount >= expense.PaymentAmount)
                        expense.ReimbursementStatus = ReimbursementStatus.Settled;
                    else
                        expense.ReimbursementStatus = ReimbursementStatus.Partial;

                    expense.ModifiedAt = DateTime.Now;
                    expense.ModifiedBy = _userService.GetCurrentUsername();
                    await _expenseRepository.UpdateAsync(expense);
                }
            }

            settlement.Status = SettlementStatus.Completed;
            settlement.PaymentModeId = paymentModeId;
            settlement.TransactionReference = transactionRef;
            settlement.SettlementDate = DateTime.Now;
            settlement.ModifiedAt = DateTime.Now;
            settlement.ModifiedBy = _userService.GetCurrentUsername();

            var updated = await _settlementRepository.UpdateAsync(settlement);
            return ServiceResult<Settlement>.SuccessResult(updated, "Settlement completed. Expenses marked as reimbursed.");
        }
        catch (Exception ex)
        {
            return ServiceResult<Settlement>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> CancelSettlementAsync(int settlementId)
    {
        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(settlementId);
            if (settlement == null)
                return ServiceResult<bool>.FailureResult("Settlement not found.");

            if (settlement.Status == SettlementStatus.Completed)
                return ServiceResult<bool>.FailureResult("Cannot cancel a completed settlement.");

            settlement.Status = SettlementStatus.Cancelled;
            settlement.ModifiedAt = DateTime.Now;
            settlement.ModifiedBy = _userService.GetCurrentUsername();

            await _settlementRepository.UpdateAsync(settlement);
            return ServiceResult<bool>.SuccessResult(true, "Settlement cancelled.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteSettlementAsync(int id)
    {
        try
        {
            var settlement = await _settlementRepository.GetByIdAsync(id);
            if (settlement == null)
                return ServiceResult<bool>.FailureResult("Settlement not found.");

            if (settlement.Status == SettlementStatus.Completed)
                return ServiceResult<bool>.FailureResult("Cannot delete a completed settlement.");

            await _settlementRepository.DeleteAsync(settlement);
            return ServiceResult<bool>.SuccessResult(true, "Settlement deleted.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<decimal>> GetPendingReimbursementAsync(int organizationId, int? contactId = null)
    {
        try
        {
            // Get payment modes that require settlement
            var paymentModes = await _paymentModeRepository.FindAsync(pm =>
                pm.OrganizationId == organizationId && pm.RequiresSettlement);
            var settlementPmIds = paymentModes.Select(pm => pm.Id).ToHashSet();

            var expenses = await _expenseRepository.FindAsync(e =>
                e.ReimbursementStatus != ReimbursementStatus.Settled);

            // Filter to expenses paid via settlement-requiring payment modes
            var filtered = expenses.Where(e => settlementPmIds.Contains(e.PaymentModeId));

            if (contactId.HasValue)
                filtered = filtered.Where(e => e.PaidById == contactId.Value);

            var total = filtered.Sum(e => e.PaymentAmount - e.ReimbursedAmount);
            return ServiceResult<decimal>.SuccessResult(total);
        }
        catch (Exception ex)
        {
            return ServiceResult<decimal>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<Expense>>> GetUnsettledExpensesAsync(int organizationId, int? contactId = null)
    {
        try
        {
            // Get payment modes that require settlement
            var paymentModes = await _paymentModeRepository.FindAsync(pm =>
                pm.OrganizationId == organizationId && pm.RequiresSettlement);
            var settlementPmIds = paymentModes.Select(pm => pm.Id).ToHashSet();

            var expenses = await _expenseRepository.FindAsync(e =>
                e.ReimbursementStatus != ReimbursementStatus.Settled);

            // Filter to expenses paid via settlement-requiring payment modes
            var filtered = expenses.Where(e => settlementPmIds.Contains(e.PaymentModeId));

            if (contactId.HasValue)
                filtered = filtered.Where(e => e.PaidById == contactId.Value);

            return ServiceResult<IEnumerable<Expense>>.SuccessResult(
                filtered.OrderByDescending(e => e.PaymentDate));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<Expense>>.FailureResult($"Error: {ex.Message}");
        }
    }
}
