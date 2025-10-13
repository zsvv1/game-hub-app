using System.ComponentModel.DataAnnotations;

namespace GameHub.Core.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, EmailAddress, StringLength(120)]
        public string Email { get; set; } = string.Empty;

        // Store a hash, never the raw password
        [Required, StringLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
