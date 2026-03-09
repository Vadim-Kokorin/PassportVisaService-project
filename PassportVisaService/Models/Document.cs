using System;

namespace PassportVisaService.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string DocumentType { get; set; } // Загранпаспорт, Внутренний паспорт, Визовая анкета и т.д.
        public string DocumentSubType { get; set; } // Новый, Замена, Продление и т.д.
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileExtension { get; set; }
        public string Status { get; set; } // Черновик, На рассмотрении, Принят, Отклонен
        public DateTime UploadDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string Comment { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string ReviewComment { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; } = true;
    }
}