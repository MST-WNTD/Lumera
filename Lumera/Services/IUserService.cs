using Lumera.Models;
using Lumera.Models.ViewModels;

namespace Lumera.Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User?> RegisterAsync(RegisterLoginViewModel model);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> UpdatePasswordAsync(int userId, string newPassword);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> ApproveUserAsync(int userId);
        Task<bool> SuspendUserAsync(int userId);
        Task<bool> ActivateUserAsync(int userId);
        Task<bool> UserExistsAsync(string email);
    }
}