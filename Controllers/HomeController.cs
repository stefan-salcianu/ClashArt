using ClashArt.Data;
using ClashArt.Models;
using ClashArt.Models.ViewModels; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClashArt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var existingLike = await _context.PostLikes.FindAsync(postId, currentUser.Id);

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
            }
            else
            {
                
                var newLike = new PostLike
                {
                    PostId = postId,
                    UserId = currentUser.Id
                };
                _context.PostLikes.Add(newLike);
            }

            await _context.SaveChangesAsync();

          
            var referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }

        public async Task<IActionResult> Index(string sortOrder = "newest")
        {
            var currentUserId = _userManager.GetUserId(User);

            bool isAdmin = User.IsInRole("Admin");

            var postsQuery = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Theme)
                .Include(p => p.Likes)
                .AsQueryable();

            ViewData["CurrentSort"] = sortOrder;

            switch (sortOrder)
            {
                case "following":
                    if (currentUserId != null)
                    {
                        var followingIds = _context.Follows
                            .Where(f => f.FollowerId == currentUserId && f.IsAccepted)
                            .Select(f => f.FollowedId);

                        postsQuery = postsQuery.Where(p => followingIds.Contains(p.UserId));

                        postsQuery = postsQuery.OrderByDescending(p => p.CreatedAt);
                    }
                    else
                    {
                        postsQuery = postsQuery.Where(p => false);
                    }
                    break;

                case "trending":
                case "newest":
                default:
                 
                    if (!isAdmin)
                    {
                        postsQuery = postsQuery.Where(p => !p.User.IsPrivate);
                    }

                    if (sortOrder == "trending")
                    {
                        postsQuery = postsQuery.OrderByDescending(p => p.Likes.Count);
                    }
                    else
                    {
                        postsQuery = postsQuery.OrderByDescending(p => p.CreatedAt);
                    }
                    break;
            }

            var feedPosts = await postsQuery.ToListAsync();

            var now = DateTime.Now;
            var activeTheme = await _context.CompetitionThemes
                .Where(t => t.StartDate <= now && t.EndDate >= now && t.Title != "Freestyle Gallery")
                .OrderByDescending(t => t.EndDate)
                .FirstOrDefaultAsync();

            var viewModel = new HomeViewModel
            {
                Posts = feedPosts,
                ActiveTheme = activeTheme
            };

            return View(viewModel);
        }
    }
}