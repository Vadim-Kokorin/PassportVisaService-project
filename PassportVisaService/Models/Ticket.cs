using System;

namespace PassportVisaService.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? AssignedToId { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; } // Открыт, В работе, Закрыт
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Priority { get; set; } // Низкий, Средний, Высокий
        public string UserName { get; set; }
        public string AssignedToName { get; set; }
    }
}