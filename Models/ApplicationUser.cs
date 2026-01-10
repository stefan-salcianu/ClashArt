using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ClashArt.Models
{
    public class ApplicationUser: IdentityUser
    {
        [StringLength(100)]
        public string? DisplayName { get; set; }

        [StringLength(240)]
        public string? Bio { get; set; }

        public string? AvatarUrl { get; set; }

        public bool IsPrivate { get; set; } = false;

        // Gamification
        public int Level { get; set; } = 1;
        public int Victories { get; set; } = 0;

    }
}
