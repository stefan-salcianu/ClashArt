using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClashArt.Data;
using ClashArt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ClashArt.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public PostsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
        }

        // GET: Posts/Create
        public IActionResult Create()
        {
            var activeThemes = _context.CompetitionThemes
                .Where(t => t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now)
                .ToList();

            if (!activeThemes.Any())
            {
                var freestyle = _context.CompetitionThemes.FirstOrDefault(t => t.Title.Contains("Freestyle"));
                if (freestyle != null) activeThemes.Add(freestyle);
                else return Content("Nu există teme active. Contactează adminul.");
            }

            ViewData["CompetitionThemeId"] = new SelectList(activeThemes, "Id", "Title");
            return View();
        }

        // POST: Posts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile imageFile, IFormFile? videoFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 1. Validare Manuală Imagine
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("ImageUrl", "Trebuie să încarci o imagine principală.");
            }

            // 2. Eliminăm câmpurile automate din validare
            // Este CRITIC să facem asta ÎNAINTE de a verifica IsValid
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Theme");
            ModelState.Remove("ImageUrl");

            // 3. Verificăm validitatea restului (Descriere, ThemeId valid)
            if (ModelState.IsValid)
            {
                // A. Procesare Imagine
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                post.ImageUrl = "/images/posts/" + uniqueFileName;

                // B. Procesare Video (Opțional)
                if (videoFile != null && videoFile.Length > 0)
                {
                    string videoFolder = Path.Combine(_hostEnvironment.WebRootPath, "videos", "posts");
                    if (!Directory.Exists(videoFolder)) Directory.CreateDirectory(videoFolder);

                    string videoName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                    string videoPath = Path.Combine(videoFolder, videoName);

                    using (var stream = new FileStream(videoPath, FileMode.Create))
                    {
                        await videoFile.CopyToAsync(stream);
                    }
                    post.ProofOfWorkVideoUrl = "/videos/posts/" + videoName;
                }

                // C. Setări Finale și Salvare
                post.CreatedAt = DateTime.Now;
                post.UserId = currentUser.Id;

                _context.Add(post);
                await _context.SaveChangesAsync();

                return RedirectToAction("Profile", "Users", new { id = currentUser.Id });
            }

            // Fallback în caz de eroare (repopulare dropdown)
            var activeThemes = _context.CompetitionThemes
                 .Where(t => t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now)
                 .ToList();
            ViewData["CompetitionThemeId"] = new SelectList(activeThemes, "Id", "Title", post.CompetitionThemeId);

            return View(post);
        }
    }
}