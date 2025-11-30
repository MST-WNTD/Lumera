
namespace Lumera.Models
{
    public class ClientBrowseViewModel
    {
        public Client Client { get; set; } = new Client();
        public List<ServiceViewModel> Services { get; set; } = new List<ServiceViewModel>();
        public int UnreadMessages { get; set; }

        public static implicit operator ClientBrowseViewModel((Client Client, IEnumerable<ServiceViewModel> Services, int UnreadMessages) v)
        {
            return new ClientBrowseViewModel
            {
                Client = v.Client,
                Services = new List<ServiceViewModel>(v.Services),
                UnreadMessages = v.UnreadMessages
            };
        }
    }

    public class ServiceViewModel
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Rating { get; set; }
        public int ReviewCount { get; set; }
        public string Category { get; set; } = string.Empty;
    }
    public class BookServiceRequest
    {
        public int ServiceId { get; set; }
        public int EventId { get; set; }
    }
}