using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClashArt.Data;
using ClashArt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClashArt.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public PostsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
            _configuration = configuration;
        }

        // --- METODA PRIVATĂ PENTRU GEMINI AI ---
        private async Task<bool> CheckContentWithGemini(string text)
        {
            try
            {
                var apiKey = _configuration["GoogleAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey)) return false;

                using var client = new HttpClient();
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                var prompt = $@"
                    Act as a strict Content Moderation AI. 
                    Analyze the following text for: hate speech, profanity (in English or Romanian), sexual content, harassment, or extreme insults.
                    Text: ""{text}""
                    
                    Rules:
                    1. If it contains words like 'sugi', 'pula', 'mortii', 'fuck', 'shit', 'idiot' -> Return TRUE immediately.
                    2. If it is a friendly greeting or normal sentence -> Return FALSE.
                    
                    ANSWER ONLY WITH THE WORD 'TRUE' (if toxic) OR 'FALSE' (if safe). NO EXPLANATION.";

                var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, jsonContent);
                if (!response.IsSuccessStatusCode) return false;

                var responseString = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(responseString);
                var answer = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim()?.ToUpper();

                return answer != null && answer.Contains("TRUE");
            }
            catch { return false; }
        }

        public IActionResult Create()
        {
            PopulateThemesDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile imageFile, IFormFile? videoFile)
        {
            // --- VERIFICARE AI ---
            if (await CheckContentWithGemini(post.Description))
            {
                ViewData["ToxicError"] = "Manifesto";
                PopulateThemesDropdown(post.CompetitionThemeId);
                return View(post);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            if (imageFile == null || imageFile.Length == 0)
                ModelState.AddModelError("ImageUrl", "Trebuie să încarci o imagine principală.");

            ModelState.Remove("User"); ModelState.Remove("UserId"); ModelState.Remove("Theme"); ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
                post.ImageUrl = "/images/posts/" + uniqueFileName;

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
                return RedirectToAction("Index", "Home");
            }
            PopulateThemesDropdown(post.CompetitionThemeId);
            return View(post);
        }

        public async Task<IActionResult> Edit(int? id, string? returnUrl)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id) return Forbid();
            PopulateThemesDropdown(post.CompetitionThemeId);
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

            // --- VERIFICARE AI LA EDIT ---
            if (await CheckContentWithGemini(post.Description))
            {
                ViewData["ToxicError"] = "Manifesto";
                ViewData["ReturnUrl"] = returnUrl;
                PopulateThemesDropdown(post.CompetitionThemeId);
                post.ImageUrl = existingPost.ImageUrl;
                post.ProofOfWorkVideoUrl = existingPost.ProofOfWorkVideoUrl;
                return View(post);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (existingPost.UserId != currentUser.Id) return Forbid();

            ModelState.Remove("User"); ModelState.Remove("UserId"); ModelState.Remove("Theme"); ModelState.Remove("ImageUrl"); ModelState.Remove("ProofOfWorkVideoUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        DeleteFile(existingPost.ImageUrl);
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "posts");
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
                        post.ImageUrl = "/images/posts/" + uniqueFileName;
                    }
                    else { post.ImageUrl = existingPost.ImageUrl; }

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
                    else { post.ProofOfWorkVideoUrl = existingPost.ProofOfWorkVideoUrl; }

                    post.UserId = existingPost.UserId;
                    post.CreatedAt = existingPost.CreatedAt;
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                    if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Posts.Any(e => e.Id == post.Id)) return NotFound(); else throw;
                }
            }
            PopulateThemesDropdown(post.CompetitionThemeId);
            ViewData["ReturnUrl"] = returnUrl;
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id && !User.IsInRole("Admin")) return Forbid();
            DeleteFile(post.ImageUrl);
            DeleteFile(post.ProofOfWorkVideoUrl);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id, string? returnUrl)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (post.UserId != currentUser.Id) return Forbid();
            if (!string.IsNullOrEmpty(post.ProofOfWorkVideoUrl)) DeleteFile(post.ProofOfWorkVideoUrl);
            post.ProofOfWorkVideoUrl = null;
            _context.Entry(post).Property(x => x.ProofOfWorkVideoUrl).IsModified = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Edit", new { id = post.Id, returnUrl = returnUrl });
        }

        private void PopulateThemesDropdown(int? selectedThemeId = null)
        {
            var activeThemes = _context.CompetitionThemes.Where(t => t.StartDate <= DateTime.Now && t.EndDate >= DateTime.Now).ToList();
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
            try { var absolutePath = Path.Combine(_hostEnvironment.WebRootPath, relativeUrl.TrimStart('/')); if (System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath); } catch { }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Theme)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null) return NotFound();
            post.Comments = post.Comments.OrderByDescending(c => c.CreatedAt).ToList();
            return View(post);
        }
    }
}