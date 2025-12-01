namespace Lumera.Models
{
    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public string MessageText { get; set; } = string.Empty;
    }

    public class StartConversationRequest
    {
        public int BookingId { get; set; }
        public string? InitialMessage { get; set; }
    }

    public class UpdateBookingStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class ToggleStatusRequest
    {
        public bool IsActive { get; set; }
    }
    public class UpdateUserStatusRequest
    {
        public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class ModerateContentRequest
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    public class ModerateServiceRequest
    {
        public int ServiceId { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class ModerateReviewRequest
    {
        public int ReviewId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}