using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Helpers;
using CorporateKnowledgeBase.Web.Models;
using CorporateKnowledgeBase.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// Manages the main pages of the application, such as the home page and privacy policy.
    /// </summary>
    public class HomeController(
        ILogger<HomeController> logger, 
        ApplicationDbContext context
        ) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Displays the main dashboard for authenticated users or a guest page for anonymous users.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            if (User?.Identity?.IsAuthenticated == true)
            {
                // For authenticated users, populate the view model with various content lists.
                viewModel.RecentAnnouncements = await _context.Announcements
                                                    .OrderByDescending(a => a.CreatedDate)
                                                    .Take(2).ToListAsync();

                viewModel.RecentBlogPosts = await _context.BlogPosts
                                                .Include(b => b.Author)
                                                .OrderByDescending(b => b.CreatedDate)
                                                .Take(5).ToListAsync();

                viewModel.RecentDocuments = await _context.TechnicalDocuments
                                                .Include(d => d.Author)
                                                .OrderByDescending(d => d.CreatedDate)
                                                .Take(5).ToListAsync();

                // Retrieve and populate the "Recently Viewed" items from the user's session.
                var recentViews = HttpContext.Session.Get<List<RecentViewItem>>("RecentViews") ?? [];
                var recentItems = new List<object>();

                foreach (var view in recentViews)
                {
                    if (view.ContentType == "Blog")
                    {
                        var post = await _context.BlogPosts.FindAsync(view.ContentId);
                        if (post != null) recentItems.Add(post);
                    }
                    else if (view.ContentType == "Document")
                    {
                        var doc = await _context.TechnicalDocuments.FindAsync(view.ContentId);
                        if (doc != null) recentItems.Add(doc);
                    }
                }
                viewModel.RecentlyViewedItems = recentItems;
            }

            return View(viewModel);
        }


        /// <summary>
        /// Gets a list of content (blogs or documents) via an AJAX request, filtered by type and time.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetContentList(string contentType, string listType, string timeFilter = "all")
        {
            int takeCount = 5;
            DateTime sinceDate = DateTime.MinValue;

            // Zaman filtresi SADECE oluþturulma tarihine göre çalýþacak
            switch (timeFilter)
            {
                case "daily": sinceDate = DateTime.Now.Date; break;
                case "weekly": sinceDate = DateTime.Now.AddDays(-7); break;
                case "monthly": sinceDate = DateTime.Now.AddMonths(-1); break;
            }

            if (contentType == "blog")
            {
                var query = _context.BlogPosts.Include(b => b.Author).AsQueryable();
                if (sinceDate != DateTime.MinValue)
                {
                    query = query.Where(b => b.CreatedDate >= sinceDate);
                }

                var list = (listType == "popular")
                    ? await query.OrderByDescending(b => b.ViewCount).Take(takeCount).ToListAsync()
                    : await query.OrderByDescending(b => b.CreatedDate).Take(takeCount).ToListAsync();

                return PartialView("_ContentListPartial", list);
            }

            if (contentType == "document")
            {
                var query = _context.TechnicalDocuments.Include(d => d.Author).AsQueryable();
                if (sinceDate != DateTime.MinValue)
                {
                    query = query.Where(d => d.CreatedDate >= sinceDate);
                }

                var list = (listType == "popular")
                    ? await query.OrderByDescending(d => d.ViewCount).Take(takeCount).ToListAsync()
                    : await query.OrderByDescending(d => d.CreatedDate).Take(takeCount).ToListAsync();

                return PartialView("_ContentListPartial", list);
            }

            return BadRequest();
        }

        /// <summary>
        /// Displays the privacy policy page.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Displays the generic error page.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}