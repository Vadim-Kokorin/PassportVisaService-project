using System;

namespace PassportVisaService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string PassportSeries { get; set; }
        public string PassportNumber { get; set; }
        public string PassportIssuedBy { get; set; }
        public DateTime? PassportIssueDate { get; set; }
        public string Citizenship { get; set; }
        public DateTime? BirthDate { get; set; }
        public string BirthPlace { get; set; }
        public string RegistrationAddress { get; set; }
        public string Role { get; set; } // Гражданин, Проверяющий, Администратор
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}