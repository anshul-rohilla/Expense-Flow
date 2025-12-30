using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.Storage.Streams;

namespace Expense_Flow.Services;

public interface IUserProfileService
{
    Task<string> GetUserNameAsync();
    Task<string> GetUserEmailAsync();
    Task<string> GetUserDisplayNameAsync();
    string GetWindowsUserName();
    Task<IRandomAccessStreamReference?> GetUserPictureAsync();
}

public class UserProfileService : IUserProfileService
{
    public async Task<string> GetUserNameAsync()
    {
        try
        {
            var users = await User.FindAllAsync();
            if (users.Count > 0)
            {
                var currentUser = users[0];
                var data = await currentUser.GetPropertyAsync(KnownUserProperties.AccountName);
                return data?.ToString() ?? Environment.UserName;
            }
        }
        catch
        {
            // Fall back to Environment username
        }
        
        return Environment.UserName;
    }

    public async Task<string> GetUserEmailAsync()
    {
        try
        {
            var users = await User.FindAllAsync();
            if (users.Count > 0)
            {
                var currentUser = users[0];
                var data = await currentUser.GetPropertyAsync(KnownUserProperties.PrincipalName);
                return data?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // Return empty if not available
        }
        
        return string.Empty;
    }

    public async Task<string> GetUserDisplayNameAsync()
    {
        try
        {
            var users = await User.FindAllAsync();
            if (users.Count > 0)
            {
                var currentUser = users[0];
                var data = await currentUser.GetPropertyAsync(KnownUserProperties.DisplayName);
                if (data != null && !string.IsNullOrEmpty(data.ToString()))
                {
                    return data.ToString()!;
                }
            }
        }
        catch
        {
            // Fall back to username
        }
        
        return await GetUserNameAsync();
    }

    public string GetWindowsUserName()
    {
        return Environment.UserName;
    }

    public async Task<IRandomAccessStreamReference?> GetUserPictureAsync()
    {
        try
        {
            var users = await User.FindAllAsync();
            if (users.Count > 0)
            {
                var currentUser = users[0];
                var picture = await currentUser.GetPictureAsync(UserPictureSize.Size64x64);
                return picture;
            }
        }
        catch
        {
            // Return null if picture not available
        }
        
        return null;
    }
}
