using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ClashArt.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Cerinta 2: Nume complet / Display Name
        [StringLength(50)]
        public string DisplayName { get; set; }

        // Cerinta 2: Descriere / Manifesto
        [StringLength(240)]
        public string Bio { get; set; }

        // Cerinta 2: Poza profil (URL)
        public string AvatarUrl { get; set; }

        // Cerinta 2 & 5: Vizibilitate profil
        public bool IsPrivate { get; set; } = false;

        // Gamification (Level, Victorii)
        public int Level { get; set; } = 1;
        public int ExperiencePoints { get; set; } = 0;
        public int Victories { get; set; } = 0;

        // Relația cu postările (Un user are mai multe postări)
        public virtual ICollection<Post> Posts { get; set; }
    }
}