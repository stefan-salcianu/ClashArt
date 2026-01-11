using System.ComponentModel.DataAnnotations.Schema;

namespace ClashArt.Models
{
    public class PostLike
    {
       
        public int PostId { get; set; }
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }

        // Cheia Utilizatorului care dă Like
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}