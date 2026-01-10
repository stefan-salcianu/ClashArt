using ClashArt.Data;
using ClashArt.Models; // Asigura-te ca ai acest using
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClashArt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Luam postarile (Feed)
            var feedPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Theme)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 2. Cautam Competitia Activa (care NU este Freestyle)
            var now = DateTime.Now; // Luam timpul curent intr-o variabila pentru SQL

            var activeTheme = await _context.CompetitionThemes
                // Inlocuim 't.IsActive' cu logica explicita de date
                .Where(t => t.StartDate <= now && t.EndDate >= now && t.Title != "Freestyle Gallery")
                .OrderByDescending(t => t.EndDate)
                .FirstOrDefaultAsync();

            // 3. Impachetam totul in ViewModel
            var viewModel = new HomeViewModel
            {
                Posts = feedPosts,
                ActiveTheme = activeTheme
            };

            return View(viewModel);
        }
    }
}