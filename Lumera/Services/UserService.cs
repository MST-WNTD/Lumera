using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Lumera.Models.ViewModels;

namespace Lumera.Services
{
    public class UserService(ApplicationDbContext context) : IUserService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Client)
                .Include(u => u.Organizer)
                .Include(u => u.Supplier)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            if (!user.IsActive)
                return null;

            // Update last login
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            return user;
        }
        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        public async Task<User?> RegisterAsync(RegisterLoginViewModel model)
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                return null;

            var user = new User
            {
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Phone = model.Phone,
                IsActive = true,
                IsApproved = model.Role == "Client", // Auto-approve clients
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create role-specific record
            switch (model.Role)
            {
                case "Client":
                    var client = new Client
                    {
                        UserID = user.UserID,
                        NewsletterSubscription = true
                    };
                    _context.Clients.Add(client);
                    break;

                case "Organizer":
                    var organizer = new Organizer
                    {
                        UserID = user.UserID,
                        BusinessName = model.BusinessName ?? "",
                        BusinessDescription = model.BusinessDescription,
                        YearsOfExperience = model.YearsOfExperience,
                        IsActive = true
                    };
                    _context.Organizers.Add(organizer);
                    break;

                case "Supplier":
                    var supplier = new Supplier
                    {
                        UserID = user.UserID,
                        BusinessName = model.BusinessName ?? "",
                        BusinessDescription = model.BusinessDescription,
                        ServiceCategory = model.ServiceCategory ?? "",
                        YearsOfExperience = model.YearsOfExperience,
                        IsActive = true
                    };
                    _context.Suppliers.Add(supplier);
                    break;
            }

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Client)
                .Include(u => u.Organizer)
                .Include(u => u.Supplier)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Client)
                .Include(u => u.Organizer)
                .Include(u => u.Supplier)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            return await UpdateUserAsync(user);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Client)
                .Include(u => u.Organizer)
                .Include(u => u.Supplier)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ApproveUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsApproved = true;
            return await UpdateUserAsync(user);
        }

        public async Task<bool> SuspendUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            return await UpdateUserAsync(user);
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            return await UpdateUserAsync(user);
        }
    }
}