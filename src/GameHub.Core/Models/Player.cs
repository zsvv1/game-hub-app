using System.ComponentModel.DataAnnotations;

namespace GameHub.Core.Models
{
    public class Player
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress, StringLength(120)]
        public string? Email { get; set; }
    }
}
