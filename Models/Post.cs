using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// Asigura-te ca ai namespace-ul corect
namespace ClashArt.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Relatii ---

        public int CompetitionThemeId { get; set; }
        public virtual CompetitionTheme Theme { get; set; }

        // Aici eroarea ar trebui să dispară acum
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}