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

        // ==========================================
        // CREATE
        // ==========================================
        public IActionResult Create()
        {
            PopulateThemesDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile imageFile, IFormFile? videoFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            if (imageFile == null || imageFile.Length == 0)
                ModelState.AddModelError("ImageUrl", "Trebuie să încarci o imagine principală.");

            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Theme");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                // Upload Imagine
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
                post.ImageUrl = "/images/posts/" + uniqueFileName;

                // Upload Video
                if (videoFile != null && videoFile.Length > 0)
                {
                    string videoFolder = Path.Combine(_hostEnvironment.WebRootPath, "videos", "posts");
                    if (!Directory.Exists(videoFolder)) Directory.CreateDirectory(videoFolder);
                    string videoName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                    string videoPath = Path.Combine(videoFolder, videoName);
                    using (var stream = new FileStream(videoPath, FileMode.Create)) { await videoFile.CopyToAsync(stream); }
                    post.ProofOfWorkVideoUrl = "/videos/posts/" + videoName;
                }

                post.CreatedAt = DateTime.Now;
                post.UserId = currentUser.Id;

                _context.Add(post);
                await _context.SaveChangesAsync();

                // Redirect către Arena (Home/Index)
                return RedirectToAction("Index", "Home");
            }

            PopulateThemesDropdown(post.CompetitionThemeId);
            return View(post);
        }

        // ==========================================
        // EDIT
        // ==========================================
        public async Task<IActionResult> Edit(int? id, string? returnUrl)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id) return Forbid();

            PopulateThemesDropdown(post.CompetitionThemeId);

            // Păstrăm URL-ul de unde a venit userul
            ViewData["ReturnUrl"] = returnUrl;

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post, IFormFile? imageFile, IFormFile? videoFile, string? returnUrl)
        {
            if (id != post.Id) return NotFound();

            var existingPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingPost == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (existingPost.UserId != currentUser.Id) return Forbid();

            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Theme");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ProofOfWorkVideoUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Imagine
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        DeleteFile(existingPost.ImageUrl);
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
                        post.ImageUrl = "/images/posts/" + uniqueFileName;
                    }
                    else
                    {
                        post.ImageUrl = existingPost.ImageUrl;
                    }

                    // 2. Video
                    if (videoFile != null && videoFile.Length > 0)
                    {
                        DeleteFile(existingPost.ProofOfWorkVideoUrl);
                        string videoFolder = Path.Combine(_hostEnvironment.WebRootPath, "videos", "posts");
                        if (!Directory.Exists(videoFolder)) Directory.CreateDirectory(videoFolder);
                        string videoName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                        string videoPath = Path.Combine(videoFolder, videoName);
                        using (var stream = new FileStream(videoPath, FileMode.Create)) { await videoFile.CopyToAsync(stream); }
                        post.ProofOfWorkVideoUrl = "/videos/posts/" + videoName;
                    }
                    else
                    {
                        post.ProofOfWorkVideoUrl = existingPost.ProofOfWorkVideoUrl;
                    }

                    post.UserId = existingPost.UserId;
                    post.CreatedAt = existingPost.CreatedAt;

                    _context.Update(post);
                    await _context.SaveChangesAsync();

                    // Redirect Inteligent
                    if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Posts.Any(e => e.Id == post.Id)) return NotFound();
                    else throw;
                }
            }

            PopulateThemesDropdown(post.CompetitionThemeId);
            ViewData["ReturnUrl"] = returnUrl;
            return View(post);
        }

        // ==========================================
        // DELETE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            // Adminul sau Ownerul
            if (post.UserId != currentUser.Id && !User.IsInRole("Admin")) return Forbid();

            DeleteFile(post.ImageUrl);
            DeleteFile(post.ProofOfWorkVideoUrl);

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            // Redirect Inteligent
            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // DELETE VIDEO
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id, string? returnUrl)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id) return Forbid();

            if (!string.IsNullOrEmpty(post.ProofOfWorkVideoUrl))
            {
                DeleteFile(post.ProofOfWorkVideoUrl);
            }

            post.ProofOfWorkVideoUrl = null;
            _context.Entry(post).Property(x => x.ProofOfWorkVideoUrl).IsModified = true;
            await _context.SaveChangesAsync();

            // Ne întoarcem la Edit, păstrând returnUrl-ul original
            return RedirectToAction("Edit", new { id = post.Id, returnUrl = returnUrl });
        }

        // Helpers
        private void PopulateThemesDropdown(int? selectedThemeId = null)
        {
            var activeThemes = _context.CompetitionThemes
                .Where(t => t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now)
                .ToList();

            if (!activeThemes.Any())
            {
                var freestyle = _context.CompetitionThemes.FirstOrDefault(t => t.Title.Contains("Freestyle"));
                if (freestyle != null) activeThemes.Add(freestyle);
            }
            ViewData["CompetitionThemeId"] = new SelectList(activeThemes, "Id", "Title", selectedThemeId);
        }

        private void DeleteFile(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return;
            try
            {
                var absolutePath = Path.Combine(_hostEnvironment.WebRootPath, relativeUrl.TrimStart('/'));
                if (System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);
            }
            catch { }
        }

        [HttpGet]
        [AllowAnonymous] // Permitem și vizitatorilor să vadă, dar nu să comenteze
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User) // Autorul postării
                .Include(p => p.Theme) // Tema
                .Include(p => p.Likes) // Like-uri
                .Include(p => p.Comments).ThenInclude(c => c.User) // Comentariile + Autorii lor
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null) return NotFound();

            // Ordonăm comentariile descrescător (cele mai noi sus)
            post.Comments = post.Comments.OrderByDescending(c => c.CreatedAt).ToList();

            return View(post);
        }
    }
}