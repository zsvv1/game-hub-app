using System.ComponentModel.DataAnnotations;

namespace GameHub.Core.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Genre { get; set; }

        [Range(1970, 2100)]
        public int? ReleaseYear { get; set; }
    }
}
