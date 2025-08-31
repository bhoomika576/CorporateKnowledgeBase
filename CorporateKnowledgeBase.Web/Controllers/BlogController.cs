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
    /// Manages blog posts, including creation, editing, viewing, and listing with filters.
    /// </summary>
    [Authorize]
    public class BlogController(
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
        /// Displays a paginated and filterable list of blog posts.
        /// </summary>
        /// <param name="searchString">The text to search for in titles, content, and tags.</param>
        /// <param name="categoryId">The ID of the category to filter by.</param>
        /// <param name="tag">The name of the tag to filter by.</param>
        /// <param name="pageNumber">The current page number for pagination.</param>
        public async Task<IActionResult> Index(string searchString, int? categoryId, string tag, int pageNumber = 1)
        {
            // Fetch categories for the filter dropdown, using caching for performance.
            var categories = await GetCategoriesAsync();

            int pageSize = 10; // Number of posts per page.

            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentTag = tag;
            ViewBag.CurrentPageNumber = pageNumber;
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);

            // Start with all blog posts, including related data to avoid N+1 query issues.
            var blogPosts = from b in _context.BlogPosts
                                        .Include(b => b.Category)
                                        .Include(b => b.Author)
                                        .Include(b => b.Tags)
                                        .OrderByDescending(b => b.CreatedDate)
                            select b;

            // Apply filters based on search string, category, and tag.
            if (!string.IsNullOrEmpty(searchString))
            {
                blogPosts = blogPosts.Where(s =>
                    s.Title.Contains(searchString) ||
                    s.Content.Contains(searchString) ||
                    s.Tags.Any(t => t.Name.Contains(searchString))
                );
            }

            // Filter by category if provided.
            if (categoryId.HasValue)
            {
                blogPosts = blogPosts.Where(b => b.CategoryId == categoryId.Value);
            }

            // Filter by tag if provided.
            if (!string.IsNullOrEmpty(tag))
            {
                blogPosts = blogPosts.Where(b => b.Tags.Any(t => t.Name == tag));
            }

            // Calculate total pages for pagination.
            var totalCount = await blogPosts.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            blogPosts = blogPosts.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return PartialView("_BlogListPartial", await blogPosts.ToListAsync());
            }

            return View(await blogPosts.ToListAsync());
        }

        /// <summary>
        /// Displays the details of a single blog post, including its comments.
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Eagerly load related data to avoid N+1 query issues.
            var blogPost = await _context.BlogPosts
                                         .Include(b => b.Category)
                                         .Include(b => b.Author)
                                         .Include(b => b.Tags)
                                         .Include(b => b.Comments)
                                             .ThenInclude(c => c.Author)
                                         .FirstOrDefaultAsync(m => m.Id == id);

            if (blogPost == null)
            {
                return NotFound();
            }

            // Increment the view count and save changes.
            blogPost.ViewCount++;
            _context.Update(blogPost);
            await _context.SaveChangesAsync();
            AddRecentView("Blog", blogPost.Id); // Track recently viewed posts in session.

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);

            return View(blogPost);
        }

        /// <summary>
        /// Displays the form to create a new blog post.
        /// </summary>
        [Authorize(Roles = "Admin,Developer")]
        public async Task<IActionResult> Create()
        {
            // Fetch categories for the dropdown, using caching for performance.
            var categories = await GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        /// <summary>
        /// Handles the submission of a new blog post.
        /// </summary>
        [Authorize(Roles = "Admin,Developer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost blogPost, string tags)
        {
            ModelState.Remove("Tags"); // Tags are handled separately.

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Set the author and creation date.
                    blogPost.AuthorId = user.Id;
                    blogPost.CreatedDate = DateTime.Now;

                    await UpdateTagsForPostAsync(blogPost, tags); // Handle tags.

                    _context.Add(blogPost);
                    await _context.SaveChangesAsync();

                    // Notify users about the new blog post.
                    var savedPost = await _context.BlogPosts
                                                  .Include(b => b.Author)
                                                  .FirstOrDefaultAsync(b => b.Id == blogPost.Id);
                    if (savedPost != null)
                    {
                        await _notificationService.CreateNotificationsForNewBlogPostAsync(savedPost);
                    }

                    // Show success message and redirect to the list of blog posts.
                    TempData["SuccessMessage"] = "Blog post was created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }

            var categories = await GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", blogPost.CategoryId);
            return View(blogPost);
        }

        /// <summary>
        /// Handles the submission of an edited blog post.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost blogPost, string tags)
        {
            if (id != blogPost.Id) return NotFound();

            var postToUpdate = await _context.BlogPosts
                                             .Include(p => p.Tags)
                                             .FirstOrDefaultAsync(m => m.Id == id);

            if (postToUpdate == null) return NotFound();

            // Check if the current user is the author or an admin.
            var user = await _userManager.GetUserAsync(User);
            bool isAuthor = user != null && user.Id == postToUpdate.AuthorId;
            bool isAdmin = User.IsInRole(RoleEnums.Admin.ToString());

            if (!isAuthor && !isAdmin) return Forbid(); // Only authors or admins can edit.

            ModelState.Remove("Content");

            if (ModelState.IsValid)
            {
                try
                {
                    postToUpdate.Title = blogPost.Title;
                    postToUpdate.Content = blogPost.Content;
                    postToUpdate.CategoryId = blogPost.CategoryId;

                    await UpdateTagsForPostAsync(postToUpdate, tags);

                    await _context.SaveChangesAsync();

                    // Show success message and redirect to the details of the updated post.    
                    TempData["SuccessMessage"] = "Blog post was updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogPostExists(blogPost.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "This record was updated by another user. Please refresh and try again.");
                    }
                }
                return RedirectToAction(nameof(Details), new { id = blogPost.Id });
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
            return View("Details", postToUpdate);
        }

        /// <summary>
        /// Handles the deletion of a blog post and its associated comments.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var blogPost = await _context.BlogPosts
                                         .Include(b => b.Tags)
                                         .Include(b => b.Comments)
                                         .FirstOrDefaultAsync(b => b.Id == id);

            if (blogPost != null)
            {
                var user = await _userManager.GetUserAsync(User);
                bool isAuthor = user != null && user.Id == blogPost.AuthorId;
                bool isAdmin = User.IsInRole(RoleEnums.Admin.ToString());

                if (isAuthor || isAdmin)
                {
                    _context.Comments.RemoveRange(blogPost.Comments);
                    blogPost.Tags.Clear();
                    _context.BlogPosts.Remove(blogPost);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Blog post and related comments were deleted successfully.";
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
        /// A helper method to parse a tag string and update tags for a given blog post.
        /// It handles both creating new tags and linking existing ones.
        /// </summary>
        private async Task UpdateTagsForPostAsync(BlogPost blogPost, string tags)
        {
            blogPost.Tags.Clear();

            if (string.IsNullOrEmpty(tags)) return;

            // This helper method encapsulates the logic for processing tags from a string,
            // which can be either a JSON array from Tagify or a simple comma-separated list.

            List<string> tagNames = []; // To hold the parsed tag names.
            try
            {
                // Attempt to parse the tags as JSON (Tagify format).
                var tagObjects = JsonSerializer.Deserialize<List<TagifyTag>>(tags);
                if (tagObjects != null)
                {
                    tagNames = [.. tagObjects.Where(t => !string.IsNullOrWhiteSpace(t.value))
                     .Select(t => t.value!)
                     .Distinct()];
                }
            }
            catch (JsonException)
            {
                // Fallback for non-JSON tag formats, like simple comma-separated strings.
                tagNames = [.. tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct()]; // Split and trim.
            }

            foreach (var tagName in tagNames)
            {
                var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                if (existingTag != null)
                {
                    blogPost.Tags.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName };
                    _context.Tags.Add(newTag);
                    blogPost.Tags.Add(newTag);
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

        private bool BlogPostExists(int id)
        {
            return _context.BlogPosts.Any(e => e.Id == id);
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