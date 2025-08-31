using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.ViewComponents
{
    /// <summary>
    /// A View Component responsible for rendering the user's notification menu.
    /// It fetches the latest notifications and the unread count for the current user.
    /// </summary>
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Executes the View Component logic.
        /// </summary>
        /// <returns>A View with a list of notifications and the unread count.</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If the user is not logged in, render nothing.
            if (string.IsNullOrEmpty(userId))
            {
                return Content(string.Empty);
            }

            // Fetch the last 5 notifications for the user.
            var notifications = await _context.Notifications
                                              .Where(n => n.UserId == userId)
                                              .OrderByDescending(n => n.CreatedDate)
                                              .Take(5)
                                              .ToListAsync();

            // Get the count of all unread notifications.
            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            ViewBag.UnreadCount = unreadCount;

            return View(notifications);
        }
    }
}