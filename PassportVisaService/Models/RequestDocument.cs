using System;

namespace PassportVisaService.Models
{
    public class RequestDocument
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string DocumentType { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
    }
}