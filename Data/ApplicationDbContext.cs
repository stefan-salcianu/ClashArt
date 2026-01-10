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
    }
}
