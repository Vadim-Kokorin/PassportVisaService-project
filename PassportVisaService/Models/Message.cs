using System;

namespace PassportVisaService.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public int? TicketId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
    }
}