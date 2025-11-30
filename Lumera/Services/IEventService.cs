using Lumera.Models;

namespace Lumera.Services
{
    public interface IEventService
    {
        Task<List<Event>> GetClientEventsAsync(int clientId);
        Task<Event?> GetEventByIdAsync(int eventId);
        Task<Event> CreateEventAsync(Event eventItem);
        Task<bool> UpdateEventAsync(Event eventItem);
        Task<bool> DeleteEventAsync(int eventId);
        Task<List<Event>> GetUpcomingEventsAsync(int clientId);
    }
}