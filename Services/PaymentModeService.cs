using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Expense_Flow.Data;
using Expense_Flow.Models;
using Expense_Flow.Helpers;

namespace Expense_Flow.Services;

public interface IPaymentModeService
{
    Task<ServiceResult<IEnumerable<PaymentMode>>> GetAllPaymentModesAsync(int organizationId);
    Task<ServiceResult<IEnumerable<PaymentMode>>> GetPaymentModesByTypeAsync(PaymentModeType type);
    Task<ServiceResult<PaymentMode>> GetPaymentModeByIdAsync(int id);
    Task<ServiceResult<PaymentMode>> CreatePaymentModeAsync(PaymentMode paymentMode);
    Task<ServiceResult<PaymentMode>> UpdatePaymentModeAsync(PaymentMode paymentMode);
    Task<ServiceResult<bool>> DeletePaymentModeAsync(int id);
    Task<ServiceResult<bool>> PaymentModeExistsAsync(string name, int organizationId, int? excludeId = null);
}

public class PaymentModeService : IPaymentModeService
{
    private readonly IRepository<PaymentMode> _paymentModeRepository;
    private readonly IUserService _userService;

    public PaymentModeService(IRepository<PaymentMode> paymentModeRepository, IUserService userService)
    {
        _paymentModeRepository = paymentModeRepository;
        _userService = userService;
    }

    public async Task<ServiceResult<IEnumerable<PaymentMode>>> GetAllPaymentModesAsync(int organizationId)
    {
        try
        {
            var paymentModes = await _paymentModeRepository.FindAsync(pm => pm.OrganizationId == organizationId);
            return ServiceResult<IEnumerable<PaymentMode>>.SuccessResult(paymentModes.OrderBy(pm => pm.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<PaymentMode>>.FailureResult($"Error retrieving payment modes: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<PaymentMode>>> GetPaymentModesByTypeAsync(PaymentModeType type)
    {
        try
        {
            var paymentModes = await _paymentModeRepository.FindAsync(pm => pm.Type == type);
            return ServiceResult<IEnumerable<PaymentMode>>.SuccessResult(paymentModes.OrderBy(pm => pm.Name));
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<PaymentMode>>.FailureResult($"Error retrieving payment modes: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentMode>> GetPaymentModeByIdAsync(int id)
    {
        try
        {
            var paymentMode = await _paymentModeRepository.GetByIdAsync(id);
            if (paymentMode == null)
            {
                return ServiceResult<PaymentMode>.FailureResult("Payment mode not found.");
            }
            return ServiceResult<PaymentMode>.SuccessResult(paymentMode);
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentMode>.FailureResult($"Error retrieving payment mode: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentMode>> CreatePaymentModeAsync(PaymentMode paymentMode)
    {
        try
        {
            var validationErrors = await ValidatePaymentModeAsync(paymentMode);
            if (validationErrors.Any())
            {
                return ServiceResult<PaymentMode>.FailureResult(validationErrors);
            }

            paymentMode.CreatedAt = DateTime.Now;
            paymentMode.CreatedBy = _userService.GetCurrentUsername();
            paymentMode.ModifiedAt = null;
            paymentMode.ModifiedBy = null;

            var createdPaymentMode = await _paymentModeRepository.AddAsync(paymentMode);
            return ServiceResult<PaymentMode>.SuccessResult(createdPaymentMode, "Payment mode created successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentMode>.FailureResult($"Error creating payment mode: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PaymentMode>> UpdatePaymentModeAsync(PaymentMode paymentMode)
    {
        try
        {
            var existingPaymentMode = await _paymentModeRepository.GetByIdAsync(paymentMode.Id);
            if (existingPaymentMode == null)
            {
                return ServiceResult<PaymentMode>.FailureResult("Payment mode not found.");
            }

            var validationErrors = await ValidatePaymentModeAsync(paymentMode, paymentMode.Id);
            if (validationErrors.Any())
            {
                return ServiceResult<PaymentMode>.FailureResult(validationErrors);
            }

            existingPaymentMode.Name = paymentMode.Name;
            existingPaymentMode.Type = paymentMode.Type;
            existingPaymentMode.FundSource = paymentMode.FundSource;
            existingPaymentMode.Scope = paymentMode.Scope;
            existingPaymentMode.OwnerId = paymentMode.OwnerId;
            existingPaymentMode.ContactId = paymentMode.ContactId;
            existingPaymentMode.CardType = paymentMode.CardType;
            existingPaymentMode.LastFourDigits = paymentMode.LastFourDigits;
            existingPaymentMode.Balance = paymentMode.Balance;
            existingPaymentMode.UpiId = paymentMode.UpiId;
            existingPaymentMode.ModifiedAt = DateTime.Now;
            existingPaymentMode.ModifiedBy = _userService.GetCurrentUsername();

            var updatedPaymentMode = await _paymentModeRepository.UpdateAsync(existingPaymentMode);
            return ServiceResult<PaymentMode>.SuccessResult(updatedPaymentMode, "Payment mode updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentMode>.FailureResult($"Error updating payment mode: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeletePaymentModeAsync(int id)
    {
        try
        {
            var paymentMode = await _paymentModeRepository.GetByIdAsync(id);
            if (paymentMode == null)
            {
                return ServiceResult<bool>.FailureResult("Payment mode not found.");
            }

            await _paymentModeRepository.DeleteAsync(paymentMode);
            return ServiceResult<bool>.SuccessResult(true, "Payment mode deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error deleting payment mode: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> PaymentModeExistsAsync(string name, int organizationId, int? excludeId = null)
    {
        try
        {
            var exists = await _paymentModeRepository.ExistsAsync(pm =>
                pm.Name.ToLower() == name.ToLower() &&
                pm.OrganizationId == organizationId &&
                (!excludeId.HasValue || pm.Id != excludeId.Value));

            return ServiceResult<bool>.SuccessResult(exists);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.FailureResult($"Error checking payment mode existence: {ex.Message}");
        }
    }

    private async Task<List<string>> ValidatePaymentModeAsync(PaymentMode paymentMode, int? excludeId = null)
    {
        var errors = new List<string>();

        errors.AddRange(ValidationHelper.ValidateRequired(paymentMode.Name, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(paymentMode.Name, 200, "Name"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(paymentMode.CardType, 50, "Card Type"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(paymentMode.LastFourDigits, 4, "Last Four Digits"));
        errors.AddRange(ValidationHelper.ValidateMaxLength(paymentMode.UpiId, 200, "UPI ID"));

        if (paymentMode.Type == PaymentModeType.Card)
        {
            if (string.IsNullOrWhiteSpace(paymentMode.CardType))
            {
                errors.Add("Card Type is required for Card payment mode.");
            }
            if (string.IsNullOrWhiteSpace(paymentMode.LastFourDigits))
            {
                errors.Add("Last Four Digits is required for Card payment mode.");
            }
            else if (paymentMode.LastFourDigits.Length != 4 || !paymentMode.LastFourDigits.All(char.IsDigit))
            {
                errors.Add("Last Four Digits must be exactly 4 digits.");
            }
        }

        if (paymentMode.Type == PaymentModeType.Cash)
        {
            errors.AddRange(ValidationHelper.ValidatePositive(paymentMode.Balance, "Balance"));
        }

        if (paymentMode.Type == PaymentModeType.UPI)
        {
            if (string.IsNullOrWhiteSpace(paymentMode.UpiId))
            {
                errors.Add("UPI ID is required for UPI payment mode.");
            }
            else if (!ValidationHelper.IsValidUpiId(paymentMode.UpiId))
            {
                errors.Add("UPI ID format is invalid. Expected format: username@provider");
            }
        }

        var existsResult = await PaymentModeExistsAsync(paymentMode.Name, paymentMode.OrganizationId, excludeId);
        if (existsResult.Success && existsResult.Data)
        {
            errors.Add($"A payment mode with the name '{paymentMode.Name}' already exists.");
        }

        return errors;
    }
}
