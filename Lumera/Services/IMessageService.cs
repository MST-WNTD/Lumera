using Lumera.Models;

namespace Lumera.Services
{
    public interface IMessageService
    {
        Task<Conversation?> GetConversationByIdAsync(int conversationId);
        Task<List<Conversation>> GetUserConversationsAsync(int userId);
        Task<Conversation> CreateConversationAsync(Conversation conversation);
        Task<bool> AddParticipantAsync(ConversationParticipant participant);
        Task<Message> SendMessageAsync(Message message);
        Task<List<Message>> GetConversationMessagesAsync(int conversationId);
        Task<bool> MarkMessageAsReadAsync(int messageId);
        Task<int> GetUnreadMessageCountAsync(int userId);
        Task<List<Message>> GetUnreadMessagesAsync(int userId);
    }
}