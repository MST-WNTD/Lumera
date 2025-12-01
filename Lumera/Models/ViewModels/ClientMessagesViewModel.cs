using Lumera.Models.ViewModels;

namespace Lumera.Models
{
    public class ClientMessagesViewModel
    {
        public Client Client { get; set; } = new Client();
        public List<ConversationViewModel> Conversations { get; set; } = new List<ConversationViewModel>();
        public List<MessageViewModel> RecentMessages { get; set; } = new List<MessageViewModel>();
        public int UnreadMessages { get; set; }
    }

    public class ConversationViewModel
    {
        public int ConversationID { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}