using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashArt.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Descrierea este obligatorie.")]
        public string Description { get; set; }

        public string? ImageUrl { get; set; } // Am adăugat '?' (Nullable)

        public string? ProofOfWorkVideoUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Relații ---

        public int CompetitionThemeId { get; set; }
        public virtual CompetitionTheme Theme { get; set; }

        public string? UserId { get; set; } // Am adăugat '?' (Nullable) pentru a evita validarea automată

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}