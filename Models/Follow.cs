using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClashArt.Models
{
    public class Follow
    {
        public int Id { get; set; }

        // 1. CINE inițiază acțiunea (Urmăritorul / Fanul)
        [Required]
        public string FollowerId { get; set; }

        [ForeignKey("FollowerId")]
        public virtual ApplicationUser Follower { get; set; }

        // 2. PE CINE se apasă butonul (Ținta / Idolul)
        [Required]
        public string FollowedId { get; set; }

        [ForeignKey("FollowedId")]
        public virtual ApplicationUser Followed { get; set; }

        // 3. Statusul relației
        // True = Follow activ (îi vezi postările)
        // False = Cerere în așteptare (Pending) - apare doar la conturi Private
        public bool IsAccepted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}