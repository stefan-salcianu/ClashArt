using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ClashArt.Models;
using ClashArt.Models.ViewModels;

namespace ClashArt.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        // 1. AVEAM NEVOIE DE ASTA PENTRU POZE (O lipsea din codul tau)
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UsersController(UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
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
                FollowersCount = 0,
                IsFollowing = false
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
            
            ModelState.Remove("Id");
            ModelState.Remove("ProfileImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Actualizăm textele
            user.DisplayName = model.DisplayName;
            user.Bio = model.Bio;
            user.IsPrivate = model.IsPrivate;

            // 4. LOGICA DE SALVARE POZĂ (Era ștearsă în codul tău anterior)
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
    }
}