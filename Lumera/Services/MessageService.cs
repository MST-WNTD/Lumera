using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public MessageService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Conversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .Include(c => c.Messages)
                .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.ConversationID == conversationId);
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(int userId)
        {
            return await _context.Conversations
                .Where(c => c.Participants.Any(p => p.UserID == userId && p.LeftAt == null))
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .Include(c => c.Messages)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<Conversation> CreateConversationAsync(Conversation conversation)
        {
            conversation.CreatedAt = DateTime.Now;
            conversation.LastMessageAt = DateTime.Now;
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<bool> AddParticipantAsync(ConversationParticipant participant)
        {
            participant.JoinedAt = DateTime.Now;
            _context.ConversationParticipants.Add(participant);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<Message> SendMessageAsync(Message message)
        {
            message.SentAt = DateTime.Now;
            _context.Messages.Add(message);

            // Update conversation's last message time
            var conversation = await _context.Conversations
                .Include(c => c.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(c => c.ConversationID == message.ConversationID);

            if (conversation != null)
            {
                conversation.LastMessageAt = DateTime.Now;

                // Create notification for other participants
                var sender = await _context.Users.FindAsync(message.SenderID);
                var otherParticipants = conversation.Participants
                    .Where(p => p.UserID != message.SenderID && p.LeftAt == null)
                    .ToList();

                foreach (var participant in otherParticipants)
                {
                    if (sender != null)
                    {
                        await _notificationService.CreateMessageNotificationAsync(
                            participant.UserID,
                            conversation.ConversationID,
                            $"{sender.FirstName} {sender.LastName}"
                        );
                    }
                }
            }

            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<Message>> GetConversationMessagesAsync(int conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationID == conversationId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return false;

            message.IsRead = true;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId)
        {
            var userConversations = await _context.ConversationParticipants
                .Where(p => p.UserID == userId && p.LeftAt == null)
                .Select(p => p.ConversationID)
                .ToListAsync();

            return await _context.Messages
                .Where(m => userConversations.Contains(m.ConversationID) &&
                           m.SenderID != userId &&
                           !m.IsRead)
                .CountAsync();
        }

        public async Task<List<Message>> GetUnreadMessagesAsync(int userId)
        {
            var userConversations = await _context.ConversationParticipants
                .Where(p => p.UserID == userId && p.LeftAt == null)
                .Select(p => p.ConversationID)
                .ToListAsync();

            return await _context.Messages
                .Where(m => userConversations.Contains(m.ConversationID) &&
                           m.SenderID != userId &&
                           !m.IsRead)
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }
    }
}