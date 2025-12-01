namespace Lumera.Models.ViewModels;
public class ClientBookingsViewModel
{
    public Client Client { get; set; } = new Client();
    public List<BookingViewModel> Bookings { get; set; } = new List<BookingViewModel>();
    public int UnreadMessages { get; set; }
}
