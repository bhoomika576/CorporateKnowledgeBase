using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// Manages the site-wide search functionality.
    /// </summary>
    [Authorize]
    public class SearchController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Performs a search across multiple content types (Blogs, Documents, Announcements)
        /// and displays the paginated results.
        /// </summary>
        /// <param name="query">The search term entered by the user.</param>
        /// <param name="pageNumber">The current page number for pagination.</param>
        public async Task<IActionResult> Index(string query, int pageNumber = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return View(new SearchViewModel { Query = query });
            }

            int pageSize = 10;
            var allResults = new List<SearchResultItem>();

            // 1. Search in Blog Posts
            var blogs = await _context.BlogPosts
                .Include(b => b.Author)
                .Where(b => b.Title.Contains(query) || b.Content.Contains(query))
                .Select(b => new SearchResultItem
                {
                    Title = b.Title,
                    ContentSnippet = $"{b.Content.Substring(0, Math.Min(b.Content.Length, 200))}...",
                    ResultType = "Blog Post", 
                    CreatedDate = b.CreatedDate,
                    AuthorName = b.Author != null ? b.Author.FullName : "Unknown Author",
                    Url = $"/Blog/Details/{b.Id}"
                }).ToListAsync();
            allResults.AddRange(blogs);

            // 2. Search in Technical Documents
            var documents = await _context.TechnicalDocuments
                .Include(d => d.Author)
                .Where(d => d.Title.Contains(query) || d.Content.Contains(query))
                .Select(d => new SearchResultItem
                {
                    Title = d.Title,
                    ContentSnippet = $"{d.Content.Substring(0, Math.Min(d.Content.Length, 200))}...",
                    ResultType = "Technical Document", 
                    CreatedDate = d.CreatedDate,
                    AuthorName = d.Author != null ? d.Author.FullName : "Unknown Author",
                    Url = $"/Document/Details/{d.Id}"
                }).ToListAsync();
            allResults.AddRange(documents);

            // 3. Search in Announcements
            var announcements = await _context.Announcements
                .Include(a => a.Author)
                .Where(a => a.Title.Contains(query) || a.Content.Contains(query))
                .Select(a => new SearchResultItem
                {
                    Title = a.Title,
                    ContentSnippet = $"{a.Content.Substring(0, Math.Min(a.Content.Length, 200))}...",
                    ResultType = "Announcement", 
                    CreatedDate = a.CreatedDate,
                    AuthorName = a.Author != null ? a.Author.FullName : "Unknown Author",
                    Url = $"/Announcement/Details/{a.Id}"
                }).ToListAsync();
            allResults.AddRange(announcements);

            // 4. Sort all results by date and paginate
            var paginatedResults = allResults
                .OrderByDescending(r => r.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new SearchViewModel
            {
                Query = query,
                Results = paginatedResults,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(allResults.Count / (double)pageSize)
            };

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("_SearchResultsPartial", viewModel);
            }

            return View(viewModel);
        }
    }
}
