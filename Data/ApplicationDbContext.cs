using ClashArt.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClashArt.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Artist> Students { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<CompetitionTheme> CompetitionThemes { get; set; }

        public DbSet<Follow> Follows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurarea Relației Follow
            builder.Entity<Follow>(entity =>
            {
                // Regula de Unicitate: 
                // Combinația (Cine dă follow + Cui dă follow) trebuie să fie unică.
                // Asta împiedică baza de date să aibă duplicate (să nu poți da follow de 2 ori).
                entity.HasIndex(f => new { f.FollowerId, f.FollowedId }).IsUnique();

                // Relația: Un Follower (Eu) are multe intrări în lista Following
                entity.HasOne(f => f.Follower)
                      .WithMany(u => u.Following)
                      .HasForeignKey(f => f.FollowerId)
                      .OnDelete(DeleteBehavior.Restrict); // Restrict = Siguranță (să nu șteargă useri în lanț din greșeală)

                // Relația: Un Followed (Tu) are multe intrări în lista Followers
                entity.HasOne(f => f.Followed)
                      .WithMany(u => u.Followers)
                      .HasForeignKey(f => f.FollowedId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}