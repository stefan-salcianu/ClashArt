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
            PopulateThemesDropdown();
            return View();
        }

        // POST: Posts/Create
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
                return RedirectToAction("Profile", "Users", new { id = currentUser.Id });
            }

            PopulateThemesDropdown(post.CompetitionThemeId);
            return View(post);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id && !User.IsInRole("Admin")) return Forbid();

            PopulateThemesDropdown(post.CompetitionThemeId);
            return View(post);
        }

        // POST: Posts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post, IFormFile? imageFile, IFormFile? videoFile)
        {
            if (id != post.Id) return NotFound();

            var existingPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingPost == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (existingPost.UserId != currentUser.Id && !User.IsInRole("Admin")) return Forbid();

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
                    return RedirectToAction("Index", "Home");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Posts.Any(e => e.Id == post.Id)) return NotFound();
                    else throw;
                }
            }

            PopulateThemesDropdown(post.CompetitionThemeId);
            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id && !User.IsInRole("Admin")) return Forbid();

            DeleteFile(post.ImageUrl);
            DeleteFile(post.ProofOfWorkVideoUrl);

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        //  NOU: ȘTERGERE VIDEO SPECIFIC
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id && !User.IsInRole("Admin")) return Forbid();

            if (!string.IsNullOrEmpty(post.ProofOfWorkVideoUrl))
            {
                DeleteFile(post.ProofOfWorkVideoUrl);
            }

            post.ProofOfWorkVideoUrl = null;

            // Salvăm doar modificarea asta
            _context.Entry(post).Property(x => x.ProofOfWorkVideoUrl).IsModified = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new { id = post.Id });
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
    }
}