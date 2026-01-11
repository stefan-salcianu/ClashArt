using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashArt.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Comentariul nu poate fi mai lung de 500 de caractere.")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relația cu Postarea
        public int PostId { get; set; }
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }

        // Relația cu Userul care a scris
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}