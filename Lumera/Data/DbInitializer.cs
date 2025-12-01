using Lumera.Models;
using BCrypt.Net;

namespace Lumera.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Check if data already exists
            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // Create Admin User
            var adminUser = new User
            {
                Email = "admin@lumera.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                FirstName = "Admin",
                LastName = "User",
                Phone = "+1234567890",
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(adminUser);

            // Create Sample Client
            var clientUser = new User
            {
                Email = "client@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client@123"),
                Role = "Client",
                FirstName = "John",
                LastName = "Doe",
                Phone = "+1234567891",
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(clientUser);
            context.SaveChanges();

            var client = new Client
            {
                UserID = clientUser.UserID,
                DateOfBirth = new DateTime(1990, 1, 1),
                NewsletterSubscription = true
            };

            context.Clients.Add(client);

            // Create Sample Organizer
            var organizerUser = new User
            {
                Email = "organizer@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Organizer@123"),
                Role = "Organizer",
                FirstName = "Jane",
                LastName = "Smith",
                Phone = "+1234567892",
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(organizerUser);
            context.SaveChanges();

            var organizer = new Organizer
            {
                UserID = organizerUser.UserID,
                BusinessName = "Elite Events Planning",
                BusinessDescription = "Premium event planning services for all occasions",
                YearsOfExperience = 5,
                AverageRating = 4.8m,
                TotalReviews = 120,
                IsActive = true
            };

            context.Organizers.Add(organizer);

            // Create Sample Supplier
            var supplierUser = new User
            {
                Email = "supplier@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Supplier@123"),
                Role = "Supplier",
                FirstName = "Mike",
                LastName = "Johnson",
                Phone = "+1234567893",
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(supplierUser);
            context.SaveChanges();

            var supplier = new Supplier
            {
                UserID = supplierUser.UserID,
                BusinessName = "Gourmet Catering Co.",
                BusinessDescription = "Professional catering services for any event",
                ServiceCategory = "Catering",
                YearsOfExperience = 10,
                AverageRating = 4.9m,
                TotalReviews = 250,
                IsActive = true
            };

            context.Suppliers.Add(supplier);
            context.SaveChanges();

            // Create Sample Services
            var service1 = new Service
            {
                ProviderID = organizer.OrganizerID,
                ProviderType = "Organizer",
                ServiceName = "Full Event Planning Package",
                ServiceDescription = "Complete event planning from start to finish",
                Category = "Event Planning",
                BasePrice = 5000.00m,
                PriceType = "Package",
                Location = "Quezon City, Metro Manila",
                IsActive = true,
                IsApproved = true,
                AverageRating = 4.8m,
                TotalReviews = 45,
                CreatedAt = DateTime.Now
            };

            var service2 = new Service
            {
                ProviderID = supplier.SupplierID,
                ProviderType = "Supplier",
                ServiceName = "Premium Catering Service",
                ServiceDescription = "High-quality catering for 50-200 guests",
                Category = "Catering",
                BasePrice = 150.00m,
                PriceType = "Per Person",
                Location = "Metro Manila",
                IsActive = true,
                IsApproved = true,
                AverageRating = 4.9m,
                TotalReviews = 180,
                CreatedAt = DateTime.Now
            };

            context.Services.AddRange(service1, service2);
            context.SaveChanges();

            Console.WriteLine("Database initialized with sample data.");
        }
    }
}