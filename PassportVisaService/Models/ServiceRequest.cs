using System;
using System.Collections.Generic;

namespace PassportVisaService.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; } // Черновик, На проверке, Одобрено, Отказано, Требует доработки
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewComment { get; set; }
        public string FormData { get; set; } // JSON с данными формы

        // Навигационные свойства
        public string UserName { get; set; }
        public string ReviewerName { get; set; }

        // Список документов к заявке
        public List<RequestDocument> Documents { get; set; } = new List<RequestDocument>();
    }
}