using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Serves the main dashboard for the Admin area.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Displays the admin dashboard with key application statistics and activity.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                // Existing Stats
                UserCount = await _context.Users.CountAsync(),
                BlogPostCount = await _context.BlogPosts.CountAsync(),
                DocumentCount = await _context.TechnicalDocuments.CountAsync(),
                TagCount = await _context.Tags.CountAsync(),

                // New: Get latest 5 blog posts
                LatestBlogPosts = await _context.BlogPosts
                                                .Include(p => p.Author)
                                                .OrderByDescending(p => p.CreatedDate)
                                                .Take(5)
                                                .ToListAsync(),

                // New: Get latest 5 registered users
                LatestUsers = await _context.Users
                                            .OrderByDescending(u => u.Id) // Assuming higher Id means newer
                                            .Take(5)
                                            .ToListAsync(),

                // New: Get 5 most viewed documents
                MostViewedDocuments = await _context.TechnicalDocuments
                                                    .OrderByDescending(d => d.ViewCount)
                                                    .Take(5)
                                                    .ToListAsync()
            };

            // New: Prepare data for the chart (content created in the last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var dailyContentCounts = await _context.BlogPosts
                .Where(p => p.CreatedDate >= thirtyDaysAgo)
                .GroupBy(p => p.CreatedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                viewModel.ChartLabels.Insert(0, date.ToString("MMM dd"));
                viewModel.ChartData.Insert(0, dailyContentCounts.ContainsKey(date) ? dailyContentCounts[date] : 0);
            }

            return View(viewModel);
        }
    }
}