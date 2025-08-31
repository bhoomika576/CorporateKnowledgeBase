using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// Manages user-specific pages, such as the user profile and notification settings.
    /// </summary>
    [Authorize]
    public class ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        /// <summary>
        /// Displays the current user's profile, including their created content.
        /// </summary>
        public async Task<IActionResult> Index(int blogPage = 1, int docPage = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User information could not be retrieved.");
            }

            int pageSize = 5;

            var blogQuery = _context.BlogPosts
                                    .Where(b => b.AuthorId == user.Id)
                                    .OrderByDescending(b => b.CreatedDate);

            ViewBag.TotalBlogPostsCount = await blogQuery.CountAsync(); 

            ViewBag.TotalBlogPages = (int)Math.Ceiling((double)ViewBag.TotalBlogPostsCount / pageSize);
            ViewBag.CurrentBlogPage = blogPage;

            var blogPosts = await blogQuery.Skip((blogPage - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToListAsync();


            var docQuery = _context.TechnicalDocuments
                                   .Where(t => t.AuthorId == user.Id)
                                   .OrderByDescending(t => t.CreatedDate);


            ViewBag.TotalDocumentsCount = await docQuery.CountAsync(); 

            ViewBag.TotalDocPages = (int)Math.Ceiling((double)ViewBag.TotalDocumentsCount / pageSize);
            ViewBag.CurrentDocPage = docPage;

            var technicalDocuments = await docQuery.Skip((docPage - 1) * pageSize)
                                                   .Take(pageSize)
                                                   .ToListAsync();

            var userProfile = new UserProfileViewModel
            {
                User = user,
                BlogPosts = blogPosts,
                TechnicalDocuments = technicalDocuments
            };

            return View(userProfile);
        }


        /// <summary>
        /// Displays the notification settings page for the current user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> NotificationSettings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new NotificationSettingsViewModel
            {
                NotifyOnNewComment = user.NotifyOnNewComment,
                NotifyOnNewBlogPost = user.NotifyOnNewBlogPost,
                NotifyOnNewDocument = user.NotifyOnNewDocument,
                NotifyOnNewAnnouncement = user.NotifyOnNewAnnouncement
            };

            return View(model);
        }

        /// <summary>
        /// Handles the submission of updated notification settings.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotificationSettings(NotificationSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.NotifyOnNewComment = model.NotifyOnNewComment;
            user.NotifyOnNewBlogPost = model.NotifyOnNewBlogPost;
            user.NotifyOnNewDocument = model.NotifyOnNewDocument;
            user.NotifyOnNewAnnouncement = model.NotifyOnNewAnnouncement;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your notification settings have been updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "An error occurred while updating your settings.";
            }

            return RedirectToAction(nameof(NotificationSettings));
        }

        /// <summary>
        /// Handles the uploading and updating of the user's profile picture.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound("Kullanıcı bulunamadı.");
                }

                // Folder path where images will be saved
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Create a unique file name (to avoid conflicts)
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Delete old image
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePictureUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                // Kullanıcının profil resmi yolunu güncelleme
                user.ProfilePictureUrl = "/images/avatars/" + uniqueFileName;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("Index");
        }
    }
}
