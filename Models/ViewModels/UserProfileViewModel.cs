using System.ComponentModel.DataAnnotations;

namespace ClashArt.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string Id { get; set; } // Necesar pentru link-uri


        [Required(ErrorMessage = "Te rugăm să alegi un nume de scenă.")]
        [Display(Name = "Nume de Scenă")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Numele trebuie să aibă între 3 și 50 de caractere.")]
        public string DisplayName { get; set; }

        [Display(Name = "Manifest Artistic (Bio)")]
        [StringLength(240, ErrorMessage = "Bio-ul nu poate depăși 240 de caractere.")]
        public string? Bio { get; set; }

        [Display(Name = "Profil Privat")]
        public bool IsPrivate { get; set; }

        public string? AvatarUrl { get; set; }
        [Display(Name = "Încarcă o poză nouă")]
        public IFormFile? ProfileImage { get; set; }

        public int Level { get; set; }
        public int Victories { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; } 

        public bool IsCurrentUser { get; set; }
        public bool IsFollowing { get; set; }
        public bool IsPending { get; set; }
        public bool HasAccess { get; set; }
    }
}