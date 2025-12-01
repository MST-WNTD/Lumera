using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Services
{
    public class EventService(ApplicationDbContext context) : IEventService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<Event>> GetClientEventsAsync(int clientId)
        {
            return await _context.Events
                .Where(e => e.ClientID == clientId)
                .Include(e => e.Organizer)
                .ThenInclude(o => o.User)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
        }

        public async Task<Event?> GetEventByIdAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Client)
                .ThenInclude(c => c.User)
                .Include(e => e.Organizer)
                .ThenInclude(o => o.User)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventID == eventId);
        }

        public async Task<Event> CreateEventAsync(Event eventItem)
        {
            eventItem.CreatedAt = DateTime.Now;
            eventItem.UpdatedAt = DateTime.Now;
            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();
            return eventItem;
        }

        public async Task<bool> UpdateEventAsync(Event eventItem)
        {
            eventItem.UpdatedAt = DateTime.Now;
            _context.Events.Update(eventItem);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            var eventItem = await GetEventByIdAsync(eventId);
            if (eventItem == null) return false;

            _context.Events.Remove(eventItem);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<Event>> GetUpcomingEventsAsync(int clientId)
        {
            return await _context.Events
                .Where(e => e.ClientID == clientId && e.EventDate >= DateTime.Now)
                .OrderBy(e => e.EventDate)
                .ToListAsync();
        }
    }
}