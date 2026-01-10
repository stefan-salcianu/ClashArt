using ClashArt.Models;
using Microsoft.AspNetCore.Identity;

namespace ClashArt.Data
{
    public static class DbSeeder
    {
        public static async Task SeedData(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Verificăm dacă există deja date, ca să nu le dublăm
                if (context.Posts.Any()) return;

                // 1. Creăm un User Fictiv (Bot) pentru teste
                var dummyUser = new ApplicationUser
                {
                    UserName = "demo_artist@clashart.com",
                    Email = "demo_artist@clashart.com",
                    DisplayName = "Demo Artist", // Asigură-te că ai câmpul ăsta în ApplicationUser
                    AvatarUrl = "https://i.pravatar.cc/150?img=11", // Poză random de pe net
                    EmailConfirmed = true
                };

                // Îl salvăm doar dacă nu există
                if (await userManager.FindByEmailAsync(dummyUser.Email) == null)
                {
                    await userManager.CreateAsync(dummyUser, "ParolaSigura123!");
                }

                // Luăm User-ul din DB ca să-i avem ID-ul real (Guid)
                var userFromDb = await userManager.FindByEmailAsync(dummyUser.Email);

                // 2. Creăm Temele
                var themeFreestyle = new CompetitionTheme
                {
                    Title = "Freestyle Gallery",
                    Description = "Postări generale, fără reguli de concurs.",
                    StartDate = DateTime.Now.AddYears(-1),
                    EndDate = DateTime.Now.AddYears(5) // Mereu activă
                };

                var themeCyber = new CompetitionTheme
                {
                    Title = "Cyberpunk Noir",
                    Description = "Neon, ploaie și tehnologie.",
                    StartDate = DateTime.Now.AddDays(-2),
                    EndDate = DateTime.Now.AddDays(5) // Activă acum
                };

                context.CompetitionThemes.AddRange(themeFreestyle, themeCyber);
                await context.SaveChangesAsync();

                // 3. Creăm Postările Fake
                // Folosim placeholder images de pe net (picsum/unsplash)
                var posts = new List<Post>
                {
                    new Post
                    {
                        Description = "Prima mea lucrare digitală!",
                        ImageUrl = "https://picsum.photos/id/237/600/400", // Caine
                        CreatedAt = DateTime.Now.AddHours(-5),
                        CompetitionThemeId = themeFreestyle.Id,
                        UserId = userFromDb.Id
                    },
                    new Post
                    {
                        Description = "Intrare pentru concursul Cyberpunk. Sper să vă placă!",
                        ImageUrl = "https://picsum.photos/id/238/600/800", // Oraș
                        CreatedAt = DateTime.Now.AddHours(-2),
                        CompetitionThemeId = themeCyber.Id,
                        UserId = userFromDb.Id
                    },
                    new Post
                    {
                        Description = "Un sketch rapid de seară.",
                        ImageUrl = "https://picsum.photos/id/239/600/600", // Abstract
                        CreatedAt = DateTime.Now.AddMinutes(-30),
                        CompetitionThemeId = themeFreestyle.Id,
                        UserId = userFromDb.Id
                    }
                };

                context.Posts.AddRange(posts);
                await context.SaveChangesAsync();
            }
        }
    }
}