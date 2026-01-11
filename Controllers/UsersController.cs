using ClashArt.Data;
using ClashArt.Models;
using ClashArt.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClashArt.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public UsersController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, ApplicationDbContext context)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Profile(string? id)
        {
            ApplicationUser? targetUser;

            if (string.IsNullOrEmpty(id))
            {
                targetUser = await _userManager.GetUserAsync(User);
            }
            else
            {
                targetUser = await _userManager.FindByIdAsync(id);
            }

            if (targetUser == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            bool isMe = currentUser != null && currentUser.Id == targetUser.Id;

            bool isFollowing = false;
            bool isPending = false;

            if (currentUser != null && !isMe)
            {
                var followRelation = await _context.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowedId == targetUser.Id);

                if (followRelation != null)
                {
                    isFollowing = followRelation.IsAccepted; // True = Follow activ
                    isPending = !followRelation.IsAccepted;  // True = Cerere în așteptare
                }
            }

            var followersCount = await _context.Follows.CountAsync(f => f.FollowedId == targetUser.Id && f.IsAccepted);
            var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == targetUser.Id && f.IsAccepted);


            bool isAdmin = User.IsInRole("Admin");

            bool hasAccess = isMe || !targetUser.IsPrivate || isFollowing || isAdmin;

            var userPosts = await _context.Posts
                .Include(p => p.Likes)
                .Where(p => p.UserId == targetUser.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var model = new UserProfileViewModel
            {
                Id = targetUser.Id,
                DisplayName = targetUser.DisplayName ?? targetUser.UserName,
                Bio = targetUser.Bio,
                AvatarUrl = targetUser.AvatarUrl,
                IsPrivate = targetUser.IsPrivate,
                Level = targetUser.Level,
                Victories = targetUser.Victories,

                IsCurrentUser = isMe,
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                IsFollowing = isFollowing,
                IsPending = isPending,

                HasAccess = hasAccess,
                UserPosts = userPosts
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new UserProfileViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName ?? user.UserName,
                Bio = user.Bio,
                IsPrivate = user.IsPrivate,
                AvatarUrl = user.AvatarUrl ?? "https://placehold.co/400"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfileViewModel model)
        {
            // 1. Aducem userul PRIMA DATĂ (Corecție critică!)
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            ModelState.Remove("Id");
            ModelState.Remove("ProfileImage");
            ModelState.Remove("UserPosts");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 2. Verificare Unicitate Nume
            if (!string.IsNullOrWhiteSpace(model.DisplayName))
            {
                // Acum variabila 'user' există, deci nu mai dă eroare
                bool nameExists = await _context.Users
                    .AnyAsync(u => u.DisplayName == model.DisplayName && u.Id != user.Id);

                if (nameExists)
                {
                    ModelState.AddModelError("DisplayName", "This name is already taken by another artist.");
                    return View(model);
                }
            }

            // 3. Actualizăm textele
            user.DisplayName = model.DisplayName;
            user.Bio = model.Bio;
            user.IsPrivate = model.IsPrivate;

            // 4. Logica de salvare poză
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }

                user.AvatarUrl = "/images/profiles/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

   

        [HttpPost]
        public async Task<IActionResult> Follow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (currentUser.Id == userId) return BadRequest("You cannot follow yourself.");

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowedId == userId);

            if (existingFollow != null)
            {
                return RedirectToAction("Profile", new { id = userId });
            }

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null) return NotFound();

            var follow = new Follow
            {
                FollowerId = currentUser.Id,
                FollowedId = userId,
                CreatedAt = DateTime.UtcNow,
                IsAccepted = !targetUser.IsPrivate // Dacă e Private -> Pending (False)
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile", new { id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> Unfollow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowedId == userId);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Profile", new { id = userId });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SearchApi(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var users = _userManager.Users
                .Where(u => u.DisplayName.Contains(query) || u.UserName.Contains(query))
                .Take(5)
                .Select(u => new
                {
                    id = u.Id,
                    displayName = u.DisplayName ?? u.UserName,
                    avatarUrl = u.AvatarUrl ?? "https://placehold.co/100",
                    level = u.Level,
                    isPrivate = u.IsPrivate
                })
                .ToList();

            return Json(users);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Requests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var requests = await _context.Follows
                .Include(f => f.Follower) 
                .Where(f => f.FollowedId == currentUser.Id && !f.IsAccepted)
                .OrderByDescending(f => f.CreatedAt) 
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AcceptRequest(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Căutăm cererea specifică
            var request = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == currentUser.Id);

            if (request == null) return NotFound();

            request.IsAccepted = true;

            request.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Requests");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeclineRequest(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Căutăm cererea
            var request = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == currentUser.Id);

            if (request != null)
            {
                // O ștergem din bază (ca și cum n-ar fi existat)
                _context.Follows.Remove(request);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Requests");
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowersList(string userId)
        {
            var followers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowedId == userId && f.IsAccepted) 
                .Select(f => f.Follower)
                .ToListAsync();

            return PartialView("_UserListModal", followers);
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowingList(string userId)
        {
            var following = await _context.Follows
                .Include(f => f.Followed)
                .Where(f => f.FollowerId == userId && f.IsAccepted)
                .Select(f => f.Followed)
                .ToListAsync();

            return PartialView("_UserListModal", following);
        }
    }
}