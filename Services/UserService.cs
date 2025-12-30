using System;

namespace Expense_Flow.Services;

public interface IUserService
{
    string GetCurrentUsername();
}

public class UserService : IUserService
{
    public string GetCurrentUsername()
    {
        try
        {
            return Environment.UserName;
        }
        catch
        {
            return "System";
        }
    }
}
