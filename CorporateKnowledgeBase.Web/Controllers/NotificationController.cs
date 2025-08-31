using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// Manages user notifications, including listing and marking them as read.
    /// </summary>
    [Authorize]
    public class NotificationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        /// <summary>
        /// Displays the notification center with a list of all of the user's notifications.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Retrieve all notifications for the current user, ordered by most recent.
            var notifications = await _context.Notifications
                                              .Where(n => n.UserId == user.Id)
                                              .OrderByDescending(n => n.CreatedDate)
                                              .ToListAsync();

            return View(notifications);
        }

        /// <summary>
        /// Marks all unread notifications for the current user as read.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Find all unread notifications for the user.
            var unreadNotifications = await _context.Notifications
                                                    .Where(n => n.UserId == user.Id && !n.IsRead)
                                                    .ToListAsync();

            if (unreadNotifications.Count != 0)
            {
                // Mark all of them as read.
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }
                // Save the changes to the database.
                await _context.SaveChangesAsync();
            }

            // Redirect back to the notification list.
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Marks a single notification as read and redirects the user to its associated URL.
        /// </summary>
        /// <param name="id">The ID of the notification to process.</param>
        public async Task<IActionResult> Go(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification == null)
            {
                return NotFound();
            }

            // Mark the notification as read.
            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Redirect to the notification's URL, or to the home page if the URL is empty.
            if (string.IsNullOrEmpty(notification.Url))
            {
                return RedirectToAction("Index", "Home");
            }
            return Redirect(notification.Url);
        }
    }
}