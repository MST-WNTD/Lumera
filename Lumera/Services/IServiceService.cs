using Lumera.Models;

namespace Lumera.Services
{
    public interface IServiceService
    {
        Task<List<Service>> GetAllServicesAsync();
        Task<List<Service>> GetApprovedServicesAsync();
        Task<List<Service>> GetProviderServicesAsync(int providerId, string providerType);
        Task<Service?> GetServiceByIdAsync(int serviceId);
        Task<Service> CreateServiceAsync(Service service);
        Task<bool> UpdateServiceAsync(Service service);
        Task<bool> DeleteServiceAsync(int serviceId);
        Task<List<Service>> SearchServicesAsync(string? category = null, string? location = null, decimal? minPrice = null, decimal? maxPrice = null);
        Task<bool> ApproveServiceAsync(int serviceId);
        Task<bool> AddServiceImageAsync(ServiceGallery image);
        Task<List<ServiceGallery>> GetServiceGalleryAsync(int serviceId);
    }
}
