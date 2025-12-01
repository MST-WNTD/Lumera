using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Services
{
    public class ServiceService : IServiceService
    {
        private readonly ApplicationDbContext _context;

        public ServiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Service>> GetAllServicesAsync()
        {
            return await _context.Services
                .Include(s => s.Gallery)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Service>> GetApprovedServicesAsync()
        {
            return await _context.Services
                .Where(s => s.IsActive && s.IsApproved)
                .Include(s => s.Gallery)
                .OrderByDescending(s => s.AverageRating)
                .ToListAsync();
        }

        public async Task<List<Service>> GetProviderServicesAsync(int providerId, string providerType)
        {
            return await _context.Services
                .Where(s => s.ProviderID == providerId && s.ProviderType == providerType)
                .Include(s => s.Gallery)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Service?> GetServiceByIdAsync(int serviceId)
        {
            return await _context.Services
                .Include(s => s.Gallery)
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.ServiceID == serviceId);
        }

        public async Task<Service> CreateServiceAsync(Service service)
        {
            service.CreatedAt = DateTime.Now;
            service.IsApproved = false; // Requires admin approval
            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return service;
        }

        public async Task<bool> UpdateServiceAsync(Service service)
        {
            _context.Services.Update(service);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteServiceAsync(int serviceId)
        {
            var service = await GetServiceByIdAsync(serviceId);
            if (service == null) return false;

            _context.Services.Remove(service);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<Service>> SearchServicesAsync(string? category = null, string? location = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var query = _context.Services
                .Where(s => s.IsActive && s.IsApproved)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.Category == category);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(s => s.Location != null && s.Location.Contains(location));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(s => s.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(s => s.BasePrice <= maxPrice.Value);
            }

            return await query
                .Include(s => s.Gallery)
                .OrderByDescending(s => s.AverageRating)
                .ToListAsync();
        }

        public async Task<bool> ApproveServiceAsync(int serviceId)
        {
            var service = await GetServiceByIdAsync(serviceId);
            if (service == null) return false;

            service.IsApproved = true;
            service.IsActive = true;
            return await UpdateServiceAsync(service);
        }

        public async Task<bool> AddServiceImageAsync(ServiceGallery image)
        {
            _context.ServiceGallery.Add(image);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<ServiceGallery>> GetServiceGalleryAsync(int serviceId)
        {
            return await _context.ServiceGallery
                .Where(g => g.ServiceID == serviceId)
                .OrderBy(g => g.DisplayOrder)
                .ToListAsync();
        }
    }
}