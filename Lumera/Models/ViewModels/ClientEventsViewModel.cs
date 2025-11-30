namespace Lumera.Models
{
    public class ClientEventsViewModel
    {
        public required Client Client { get; set; }
        public List<Event> Events { get; set; } = new List<Event>();
        public int UnreadMessages { get; set; }
    }
}