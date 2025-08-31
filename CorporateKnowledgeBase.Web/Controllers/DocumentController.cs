using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Enums;
using CorporateKnowledgeBase.Web.Helpers;
using CorporateKnowledgeBase.Web.Models;
using CorporateKnowledgeBase.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// Manages technical documents. Accessible by Admins and Developers.
    /// </summary>
    [Authorize(Roles = "Admin,Developer")]
    public class DocumentController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        IMemoryCache cache, 
        INotificationService notificationService
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IMemoryCache _cache = cache;
        private readonly INotificationService _notificationService = notificationService;

        /// <summary>
        /// Displays a paginated and filterable list of technical documents.
        /// </summary>
        public async Task<IActionResult> Index(string searchString, int? categoryId, string tag, int pageNumber = 1)
        {
            // Fetch categories for the filter dropdown
            var categories = await GetCategoriesAsync();

            int pageSize = 10; // Number of documents per page
            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentTag = tag;
            ViewBag.CurrentPageNumber = pageNumber;
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);

            // Base query
            var documents = from d in _context.TechnicalDocuments
                                        .Include(d => d.Category)
                                        .Include(d => d.Author)
                                        .Include(d => d.Tags)
                                        .OrderByDescending(d => d.CreatedDate)
                            select d;

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                documents = documents.Where(s =>
                    s.Title.Contains(searchString) ||
                    s.Content.Contains(searchString) ||
                    s.Tags.Any(t => t.Name.Contains(searchString))
                );
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                documents = documents.Where(d => d.CategoryId == categoryId.Value);
            }

            // Filter by tag
            if (!string.IsNullOrEmpty(tag))
            {
                documents = documents.Where(d => d.Tags.Any(t => t.Name == tag));
            }

            var totalCount = await documents.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            documents = documents.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("_DocumentListPartial", await documents.ToListAsync());
            }

            return View(await documents.ToListAsync());
        }

        /// <summary>
        /// Displays the details of a single technical document.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var technicalDocument = await _context.TechnicalDocuments
                                                .Include(d => d.Category)
                                                .Include(d => d.Author)
                                                .Include(d => d.Tags)
                                                .Include(d => d.Comments)
                                                    .ThenInclude(c => c.Author)
                                                .FirstOrDefaultAsync(m => m.Id == id);

            if (technicalDocument == null)
            {
                return NotFound();
            }

            technicalDocument.ViewCount++;
            _context.Update(technicalDocument);
            await _context.SaveChangesAsync();
            AddRecentView("Document", technicalDocument.Id);

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", technicalDocument.CategoryId); // For edit dropdown

            return View(technicalDocument);
        }

        /// <summary>
        /// Displays the form to create a new technical document.
        /// </summary>
        public async Task<IActionResult> Create()
        {
            var categories = await GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        /// <summary>
        /// Handles the submission of a new technical document.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TechnicalDocument technicalDocument, string tags)
        {
            ModelState.Remove("Tags");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    technicalDocument.AuthorId = user.Id;
                    technicalDocument.CreatedDate = DateTime.Now;

                    await UpdateTagsForDocumentAsync(technicalDocument, tags);

                    _context.Add(technicalDocument);
                    await _context.SaveChangesAsync();

                    var savedDoc = await _context.TechnicalDocuments
                                                 .Include(d => d.Author)
                                                 .FirstOrDefaultAsync(d => d.Id == technicalDocument.Id);
                    if (savedDoc != null)
                    {
                        await _notificationService.CreateNotificationsForNewDocumentAsync(savedDoc);
                    }

                    // Show success message and redirect to index
                    TempData["SuccessMessage"] = "Technical document was created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }

            var categories = await GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", technicalDocument.CategoryId);
            return View(technicalDocument);
        }

        /// <summary>
        /// Handles the submission of an edited technical document.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TechnicalDocument technicalDocument, string tags)
        {
            if (id != technicalDocument.Id)
            {
                return NotFound();
            }

            var docToUpdate = await _context.TechnicalDocuments
                                            .Include(d => d.Tags)
                                            .FirstOrDefaultAsync(m => m.Id == id);

            if (docToUpdate == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            bool isAuthor = user != null && user.Id == docToUpdate.AuthorId;
            bool isAdmin = User.IsInRole(RoleEnums.Admin.ToString());
            if (!isAuthor && !isAdmin)
            {
                return Forbid();
            }

            ModelState.Remove("Tags");

            if (ModelState.IsValid)
            {
                try
                {
                    docToUpdate.Title = technicalDocument.Title;
                    docToUpdate.Content = technicalDocument.Content;
                    docToUpdate.CategoryId = technicalDocument.CategoryId;

                    await UpdateTagsForDocumentAsync(docToUpdate, tags);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Technical document was updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TechnicalDocumentExists(technicalDocument.Id)) { return NotFound(); }
                    else { throw; }
                }
                return RedirectToAction(nameof(Details), new { id = technicalDocument.Id });
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", technicalDocument.CategoryId);
            return View("Details", docToUpdate);
        }

        /// <summary>
        /// Handles the deletion of a technical document and its associated comments.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var docToDelete = await _context.TechnicalDocuments
                                            .Include(d => d.Tags)
                                            .Include(d => d.Comments)
                                            .FirstOrDefaultAsync(d => d.Id == id);

            if (docToDelete != null)
            {
                var user = await _userManager.GetUserAsync(User);
                bool isAuthor = user != null && user.Id == docToDelete.AuthorId;
                bool isAdmin = User.IsInRole(RoleEnums.Admin.ToString());

                if (isAuthor || isAdmin)
                {
                    _context.Comments.RemoveRange(docToDelete.Comments);
                    docToDelete.Tags.Clear();
                    _context.TechnicalDocuments.Remove(docToDelete);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Document and related comments were deleted successfully.";
                }
                else
                {
                    return Forbid();
                }
            }

            return RedirectToAction(nameof(Index));
        }


        // --- PRIVATE HELPER METHODS ---

        /// <summary>
        /// A helper method to parse a tag string and update tags for a given document.
        /// </summary>
        private async Task UpdateTagsForDocumentAsync(TechnicalDocument document, string tags)
        {
            document.Tags.Clear();
            if (string.IsNullOrEmpty(tags)) return;

            List<string> tagNames;
            try
            {
                var tagObjects = JsonSerializer.Deserialize<List<TagifyTag>>(tags);
                if (tagObjects != null)
                {
                    tagNames = [.. tagObjects.Where(t => !string.IsNullOrWhiteSpace(t.value))
                                             .Select(t => t.value!)
                                             .Distinct()];
                }
                else
                {
                    tagNames = [];
                }
            }
            catch (JsonException)
            {
                tagNames = [.. tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct()];
            }

            foreach (var tagName in tagNames)
            {
                var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                if (existingTag != null)
                {
                    document.Tags.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName };
                    _context.Tags.Add(newTag);
                    document.Tags.Add(newTag);
                }
            }
        }

        /// <summary>
        /// Retrieves the list of categories, using an in-memory cache for performance.
        /// </summary>
        private async Task<List<Category>> GetCategoriesAsync()
        {
            string cacheKey = "CategoryList";
            if (!_cache.TryGetValue(cacheKey, out List<Category>? categories))
            {
                categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1));
                _cache.Set(cacheKey, categories, cacheEntryOptions);
            }
            return categories ?? [];
        }

        private bool TechnicalDocumentExists(int id)
        {
            return _context.TechnicalDocuments.Any(e => e.Id == id);
        }

        /// <summary>
        /// Adds a content item to the user's "Recently Viewed" list in the session.
        /// </summary>
        private void AddRecentView(string contentType, int contentId)
        {
            var recentViews = HttpContext.Session.Get<List<RecentViewItem>>("RecentViews") ?? [];
            recentViews.RemoveAll(item => item.ContentType == contentType && item.ContentId == contentId);
            recentViews.Insert(0, new RecentViewItem { ContentType = contentType, ContentId = contentId });
            var limitedViews = recentViews.Take(10).ToList();
            HttpContext.Session.Set("RecentViews", limitedViews);
        }
    }
}