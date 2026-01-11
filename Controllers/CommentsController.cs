using ClashArt.Data;
using ClashArt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClashArt.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Details", "Posts", new { id = postId });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                PostId = postId,
                UserId = currentUser.Id,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Redirect înapoi pe pagina de detalii a postării (Refresh)
            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Logica: Poți șterge dacă ești autorul comentariului SAU Admin
            if (comment.UserId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            int postId = comment.PostId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Posts", new { id = postId });
        }
    }
}