using System.ComponentModel.DataAnnotations;

namespace ClashArt.Models
{
    public class CompetitionTheme
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // --- ADAUGĂ ACEASTĂ LINIE ---
        public string? ReferenceImageUrl { get; set; }

        public bool IsActive => DateTime.Now >= StartDate && DateTime.Now <= EndDate;

        public virtual ICollection<Post> Posts { get; set; }
    }
}