using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using CorporateKnowledgeBase.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// Manages announcements. Only accessible by Admins, except for the Details view.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AnnouncementController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        INotificationService notificationService
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly INotificationService _notificationService = notificationService;


        /// <summary>
        /// Displays a list of all announcements.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Eagerly load the Author to avoid N+1 query issues.
            var announcements = await _context.Announcements
                                              .Include(a => a.Author)
                                              .OrderByDescending(a => a.CreatedDate)
                                              .ToListAsync();
            return View(announcements);
        }

        /// <summary>
        /// Displays the details of a single announcement. Can be viewed by anonymous users.
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var announcement = await _context.Announcements
                                             .Include(a => a.Author)
                                             .FirstOrDefaultAsync(m => m.Id == id);
            if (announcement == null) return NotFound();
            return View(announcement);
        }

        /// <summary>
        /// Displays the form to create a new announcement.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Handles the submission of a new announcement.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    // This can happen if the user's cookie is valid but the user was deleted from the DB.
                    return Unauthorized();
                }

                // Set the author and creation date.
                announcement.AuthorId = user.Id;
                announcement.CreatedDate = DateTime.Now;
                _context.Add(announcement);
                await _context.SaveChangesAsync();

                var savedAnnouncement = await _context.Announcements
                                                      .Include(a => a.Author)
                                                      .FirstOrDefaultAsync(a => a.Id == announcement.Id);
                if (savedAnnouncement != null)
                {
                    await _notificationService.CreateNotificationsForNewAnnouncementAsync(savedAnnouncement);
                }

                // Use TempData to show a success message after redirect.
                TempData["SuccessMessage"] = "Announcement was created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(announcement);
        }

        /// <summary>
        /// Handles the deletion of an announcement.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Announcement was deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}