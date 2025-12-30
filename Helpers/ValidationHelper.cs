using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Expense_Flow.Helpers;

public static class ValidationHelper
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var phoneRegex = new Regex(@"^[\d\s\-\+\(\)]+$");
        return phoneRegex.IsMatch(phone) && phone.Length >= 10;
    }

    public static bool IsValidUpiId(string? upiId)
    {
        if (string.IsNullOrWhiteSpace(upiId))
            return false;

        var upiRegex = new Regex(@"^[\w\.\-_]+@[\w]+$", RegexOptions.IgnoreCase);
        return upiRegex.IsMatch(upiId);
    }

    public static List<string> ValidateRequired(string? value, string fieldName)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required.");
        }
        return errors;
    }

    public static List<string> ValidateMaxLength(string? value, int maxLength, string fieldName)
    {
        var errors = new List<string>();
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
        {
            errors.Add($"{fieldName} cannot exceed {maxLength} characters.");
        }
        return errors;
    }

    public static List<string> ValidatePositive(decimal? value, string fieldName)
    {
        var errors = new List<string>();
        if (value.HasValue && value.Value < 0)
        {
            errors.Add($"{fieldName} must be a positive value.");
        }
        return errors;
    }

    public static List<string> ValidateDateRange(DateTime? startDate, DateTime? endDate, string startFieldName, string endFieldName)
    {
        var errors = new List<string>();
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            errors.Add($"{startFieldName} cannot be after {endFieldName}.");
        }
        return errors;
    }
}
