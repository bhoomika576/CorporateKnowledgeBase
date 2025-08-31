using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorporateKnowledgeBase.Web.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace CorporateKnowledgeBase.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Manages content categories in the Admin area.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryManagementController(ApplicationDbContext context, IMemoryCache cache) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IMemoryCache _cache = cache;

        /// <summary>
        /// Displays a list of all categories with their usage counts.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Select(cat => new CategoryViewModel
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    UsageCount = cat.BlogPosts.Count + cat.TechnicalDocuments.Count
                })
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        /// <summary>
        /// Displays the view for creating a new resource.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Handles the creation of a new category.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                _cache.Remove("CategoryList"); // Invalidate the cache
                TempData["SuccessMessage"] = $"Category '{category.Name}' was created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        /// <summary>
        /// Displays the edit view for a specific category.
        /// </summary>
        /// <param name="id"></param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        /// <summary>
        /// Handles the update of an existing category.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == category.Id)) return NotFound();
                    else throw;
                }
                await _context.SaveChangesAsync();
                _cache.Remove("CategoryList"); // Invalidate the cache
                TempData["SuccessMessage"] = $"Category '{category.Name}' was updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        /// <summary>
        /// Handles the deletion of a category.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                                         .Include(c => c.BlogPosts)
                                         .Include(c => c.TechnicalDocuments)
                                         .FirstOrDefaultAsync(c => c.Id == id);

            if (category != null)
            {
                // If the category is in use, prevent deletion and show an error message.
                if (category.BlogPosts.Count != 0 || category.TechnicalDocuments.Count != 0)
                {
                    TempData["ErrorMessage"] = $"Category '{category.Name}' cannot be deleted as it is currently in use.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{category.Name}' was deleted successfully.";
                _cache.Remove("CategoryList"); // Invalidate the cache
            }

            return RedirectToAction(nameof(Index));
        }
    }
}