using ClashArt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
                // 1. ADĂUGAT: Avem nevoie de RoleManager
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await context.Database.MigrateAsync();

                // Daca avem deja useri, ne oprim (Presupunem că baza e populată)
                if (await userManager.Users.AnyAsync()) return;

                // ====================================================
                // 0. ROLURI (ROLES) - PRIMUL PAS
                // ====================================================
                string[] roleNames = { "Admin", "User" };

                foreach (var roleName in roleNames)
                {
                    // Verificăm dacă rolul există, dacă nu, îl creăm
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // ====================================================
                // 1. TEME (THEMES)
                // ====================================================
                var themes = new List<CompetitionTheme>
                {
                    new CompetitionTheme
                    {
                        Title = "Freestyle Gallery",
                        Description = "Postează orice, oricând. Fără reguli, doar artă.",
                        StartDate = DateTime.Now.AddYears(-2),
                        EndDate = DateTime.Now.AddYears(10),
                        ReferenceImageUrl = "https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=800"
                    },
                    new CompetitionTheme
                    {
                        Title = "Cyberpunk Noir",
                        Description = "Neon, ploaie acidă și tehnologie distopică.",
                        StartDate = DateTime.Now.AddDays(-5),
                        EndDate = DateTime.Now.AddDays(5),
                        ReferenceImageUrl = "https://images.unsplash.com/photo-1555680202-c86f0e12f086?w=800"
                    },
                    // Aceasta tema este EXPIRATA (pt testare istoric)
                    new CompetitionTheme
                    {
                        Title = "Nature Reclaimed",
                        Description = "Lumea după oameni. Plantele cuceresc betonul.",
                        StartDate = DateTime.Now.AddDays(-20),
                        EndDate = DateTime.Now.AddDays(-2),
                        ReferenceImageUrl = "https://images.unsplash.com/photo-1518531933037-9a3b14781ad9?w=800"
                    }
                };
                context.CompetitionThemes.AddRange(themes);
                await context.SaveChangesAsync();

                // ====================================================
                // 2. USERI (ARTISTS)
                // ====================================================
                var usersData = new List<(string Email, string Name, string Bio, string Avatar)>
                {
                    ("admin@clashart.com", "Admin Boss", "Eu fac regulile aici.", "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200"),
                    ("neon_samurai@demo.com", "Neon Samurai", "Digital artist exploring the synthwave realm.", "https://images.unsplash.com/photo-1599566150163-29194dcaad36?w=200"),
                    ("pixel_queen@demo.com", "Pixel Queen", "Retro gaming vibes & 8-bit love.", "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200"),
                    ("glitch_god@demo.com", "Glitch God", "Breaking the system one pixel at a time.", "https://images.unsplash.com/photo-1527980965255-d3b416303d12?w=200"),
                    ("traditional_art@demo.com", "DaVinci Reborn", "Oil on canvas only. Digital is just 0s and 1s.", "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200")
                };

                var createdUsers = new List<ApplicationUser>();

                foreach (var u in usersData)
                {
                    var user = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        DisplayName = u.Name,
                        Bio = u.Bio,
                        AvatarUrl = u.Avatar,
                        EmailConfirmed = true,
                        Level = new Random().Next(1, 50),
                        Victories = new Random().Next(0, 20)
                    };

                    var result = await userManager.CreateAsync(user, "Parola123!");

                    if (result.Succeeded)
                    {
                        createdUsers.Add(user);

                        // --- MODIFICARE ROLURI AICI ---

                        // 1. Toată lumea primește rolul de "User" (Artist)
                        await userManager.AddToRoleAsync(user, "User");

                        // 2. Dacă este email-ul de admin, primește ȘI rolul de "Admin"
                        if (u.Email == "admin@clashart.com")
                        {
                            await userManager.AddToRoleAsync(user, "Admin");
                        }
                    }
                }

                // ====================================================
                // 3. RESOURCE POOLS (Imagini & Texte)
                // ====================================================

                // Lista uriasă de imagini Art & Design
                var artImages = new[]
                {
                    // --- CYBERPUNK & NEON (12 Imagini) ---
                    "https://images.unsplash.com/photo-1555680202-c86f0e12f086?w=800&q=80",
                    "https://images.unsplash.com/photo-1620641788421-7f1c338e420c?w=800&q=80",
                    "https://images.unsplash.com/photo-1563089145-599997674d42?w=800&q=80",
                    "https://images.unsplash.com/photo-1542831371-29b0f74f9713?w=800&q=80",
                    "https://images.unsplash.com/photo-1605810230434-7631ac76ec81?w=800&q=80",
                    "https://images.unsplash.com/photo-1580927752452-89d86da3fa0a?w=800&q=80",
                    "https://images.unsplash.com/photo-1515630278258-407f66498911?w=800&q=80",
                    "https://images.unsplash.com/photo-1496449903678-68ddcb189a24?w=800&q=80",
                    "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=800&q=80",
                    "https://images.unsplash.com/photo-1573455494060-c5595004fb6c?w=800&q=80",
                    "https://images.unsplash.com/photo-1504384308090-c54be3855092?w=800&q=80",
                    "https://images.unsplash.com/photo-1565622080838-51f7823f5385?w=800&q=80",

                    // --- ABSTRACT & 3D ART (12 Imagini) ---
                    "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=800&q=80",
                    "https://images.unsplash.com/photo-1633596683562-4a469767350c?w=800&q=80",
                    "https://images.unsplash.com/photo-1549490349-8643362247b5?w=800&q=80",
                    "https://images.unsplash.com/photo-1550684848-fac1c5b4e853?w=800&q=80",
                    "https://images.unsplash.com/photo-1569172122301-bc5008bc09c5?w=800&q=80",
                    "https://images.unsplash.com/photo-1541701494587-cb58502866ab?w=800&q=80",
                    "https://images.unsplash.com/photo-1614730341194-75c6074065db?w=800&q=80",
                    "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=800&q=80",
                    "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=800&q=80",
                    "https://images.unsplash.com/photo-1558591710-4b4a1ae0f04d?w=800&q=80",
                    "https://images.unsplash.com/photo-1550684847-75bdda21cc95?w=800&q=80",
                    "https://images.unsplash.com/photo-1509114397022-ed747cca3f65?w=800&q=80",

                    // --- DARK FANTASY & SURREAL (10 Imagini) ---
                    "https://images.unsplash.com/photo-1518531933037-9a3b14781ad9?w=800&q=80",
                    "https://images.unsplash.com/photo-1519074069444-1ba4fff66d16?w=800&q=80",
                    "https://images.unsplash.com/photo-1533158307587-828f0a76ef93?w=800&q=80",
                    "https://images.unsplash.com/photo-1596796333939-50c822e11d01?w=800&q=80",
                    "https://images.unsplash.com/photo-1472214103451-9374bd1c798e?w=800&q=80",
                    "https://images.unsplash.com/photo-1516541196182-6bdb0516ed27?w=800&q=80",
                    "https://images.unsplash.com/photo-1500462918059-b1a0cb512f1d?w=800&q=80",
                    "https://images.unsplash.com/photo-1590422998634-93a8e5783307?w=800&q=80",
                    "https://images.unsplash.com/photo-1518558997970-4ddc236affcd?w=800&q=80",
                    "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&q=80",

                    // --- CHARACTERS & PORTRAITS (10 Imagini) ---
                    "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=800&q=80",
                    "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=800&q=80",
                    "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=800&q=80",
                    "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=800&q=80",
                    "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800&q=80",
                    "https://images.unsplash.com/photo-1531123897727-8f129e1688ce?w=800&q=80",
                    "https://images.unsplash.com/photo-1488161628813-99c97485fe11?w=800&q=80",
                    "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=800&q=80",
                    "https://images.unsplash.com/photo-1581338834647-b0fb40704e21?w=800&q=80",
                    "https://images.unsplash.com/photo-1500917293891-ef795e70e1f6?w=800&q=80",

                    // --- TRADITIONAL & SKETCH (11 Imagini) ---
                    "https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=800&q=80",
                    "https://images.unsplash.com/photo-1579783902614-a3fb39279c0f?w=800&q=80",
                    "https://images.unsplash.com/photo-1547891654-e66ed7ebb968?w=800&q=80",
                    "https://images.unsplash.com/photo-1578321272176-b7bbc0679853?w=800&q=80",
                    "https://images.unsplash.com/photo-1582201942988-13e60e4556ee?w=800&q=80",
                    "https://images.unsplash.com/photo-1579762715118-a6f1d4b934f1?w=800&q=80",
                    "https://images.unsplash.com/photo-1515405295579-ba7b45403062?w=800&q=80",
                    "https://images.unsplash.com/photo-1596548438137-d51ea5c83ca5?w=800&q=80",
                    "https://images.unsplash.com/photo-1569154941061-e231b4725ef1?w=800&q=80",
                    "https://images.unsplash.com/photo-1605721911519-3dfeb3be25e7?w=800&q=80",
                    "https://images.unsplash.com/photo-1544275149-a2e619d0824f?w=800&q=80"
                };

                // Titluri / Descrieri "Naturale"
                var captions = new[]
                {
                    "Just finished this piece! What do you guys think? 🎨",
                    "Late night inspiration hitting hard. #art #creative",
                    "Work in progress... taking forever but worth it.",
                    "Trying out a new style today. Feels weird but good.",
                    "Cyberpunk vibes all the way 🤖🌆",
                    "Nature always calms my mind. 🌿",
                    "This took me 40 hours. I need sleep.",
                    "Testing some new brushes in Procreate.",
                    "A quick sketch before breakfast.",
                    "Neon lights and city nights.",
                    "Chaos is the only truth.",
                    "Dreaming in digital.",
                    "My entry for the battle! Hope I win 🏆",
                    "Practicing lighting and shadows.",
                    "Abstract flow state.",
                    "Portrait study, referenced from Pinterest.",
                    "The colors turned out exactly how I wanted.",
                    "Glitch in the matrix.",
                    "Old school vibes.",
                    "Can't wait to see everyone else's submissions!"
                };

                // Luăm ID-urile temelor
                var freestyleTheme = await context.CompetitionThemes.FirstAsync(t => t.Title.Contains("Freestyle"));
                var cyberTheme = await context.CompetitionThemes.FirstAsync(t => t.Title.Contains("Cyberpunk"));

                // ====================================================
                // 4. GENERARE POSTĂRI
                // ====================================================
                var random = new Random();
                var posts = new List<Post>();

                foreach (var user in createdUsers)
                {
                    // Între 5 și 10 postări per user
                    int postsCount = random.Next(5, 11);

                    for (int i = 0; i < postsCount; i++)
                    {
                        var isCyberpunk = random.NextDouble() > 0.6; // 40% sansa sa fie cyberpunk
                        var theme = isCyberpunk ? cyberTheme : freestyleTheme;

                        // Alege o imagine care nu a fost folosita recent
                        string randomImage = artImages[random.Next(artImages.Length)];
                        string randomCaption = captions[random.Next(captions.Length)];

                        posts.Add(new Post
                        {
                            UserId = user.Id,
                            CompetitionThemeId = theme.Id,
                            Description = randomCaption,
                            ImageUrl = randomImage,
                            ProofOfWorkVideoUrl = null,
                            CreatedAt = DateTime.Now.AddDays(-random.Next(0, 60)).AddHours(-random.Next(0, 24))
                        });
                    }
                }

                context.Posts.AddRange(posts);
                await context.SaveChangesAsync();
            }
        }
    }
}