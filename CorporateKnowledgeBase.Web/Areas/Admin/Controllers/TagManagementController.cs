using CorporateKnowledgeBase.Web.Areas.Admin.Models;
using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorporateKnowledgeBase.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Manages content tags in the Admin area.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TagManagementController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Displays a list of all tags with their usage counts.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // We calculate the total number of uses of each tag in both Blog and Docs..
            var tags = await _context.Tags
                .Select(tag => new TagViewModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    UsageCount = tag.BlogPosts.Count + tag.TechnicalDocuments.Count
                })
                .OrderByDescending(t => t.UsageCount)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return View(tags);
        }


        /// <summary>
        /// Displays the edit form for a specific tag.
        /// </summary>
        /// <param name="id"></param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        /// <summary>
        /// Handles the update of a tag, including merging if a tag with the same name exists.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Tag tagFromForm) // Parametre adını netleştirdik
        {
            if (id != tagFromForm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if there is another tag with the newly entered tag name
                var newTagName = tagFromForm.Name;
                var existingTagWithNewName = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name == newTagName && t.Id != id);

                if (existingTagWithNewName != null)
                {
                    // --- MERGE SCENARIO ---
                    // 1. Find the original tag that was edited (and will be deleted) from the database.
                    // This is the single, correct object that EF Core "follows."
                    var tagToDelete = await _context.Tags.FindAsync(id);
                    if (tagToDelete == null) return NotFound();

                    // 2. Find all content using the old tag (Blog and Document)
                    var blogPostsToUpdate = await _context.BlogPosts
                        .Include(b => b.Tags)
                        .Where(b => b.Tags.Any(t => t.Id == id))
                        .ToListAsync();

                    var docsToUpdate = await _context.TechnicalDocuments
                        .Include(d => d.Tags)
                        .Where(d => d.Tags.Any(t => t.Id == id))
                        .ToListAsync();

                    // 3. Update the content tags: Remove the old, add the new (already existing)
                    foreach (var post in blogPostsToUpdate)
                    {
                        post.Tags.Remove(tagToDelete);
                        if (!post.Tags.Contains(existingTagWithNewName))
                        {
                            post.Tags.Add(existingTagWithNewName);
                        }
                    }
                    foreach (var doc in docsToUpdate)
                    {
                        doc.Tags.Remove(tagToDelete);
                        if (!doc.Tags.Contains(existingTagWithNewName))
                        {
                            doc.Tags.Add(existingTagWithNewName);
                        }
                    }

                    // 4. Delete the old tag (the object we retrieved from the database) that is no longer used
                    _context.Tags.Remove(tagToDelete);
                }
                else
                {
                    // --- NORMAL NAME CHANGE SCENARIO ---
                    // We are updating the actual object we retrieved from the database, not the 'tagFromForm' object from the form.
                    var tagToUpdate = await _context.Tags.FindAsync(id);
                    if (tagToUpdate == null) return NotFound();

                    tagToUpdate.Name = newTagName;
                    _context.Update(tagToUpdate);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tag was updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(tagFromForm);
        }


        /// <summary>
        /// Handles the deletion of a tag and removes its associations from all content.
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // We retrieve the tag to be deleted, along with all the articles and documents attached to it.
            var tagToDelete = await _context.Tags
                                            .Include(t => t.BlogPosts)
                                            .Include(t => t.TechnicalDocuments)
                                            .FirstOrDefaultAsync(t => t.Id == id);

            if (tagToDelete != null)
            {
                // 1. Remove tag from all Blog Posts that this tag is linked to.
                foreach (var post in tagToDelete.BlogPosts.ToList())
                {
                    post.Tags.Remove(tagToDelete);
                }

                // 2. Remove the tag from all Technical Documents to which this tag is linked.
                foreach (var doc in tagToDelete.TechnicalDocuments.ToList())
                {
                    doc.Tags.Remove(tagToDelete);
                }

                // 3. Delete the tag itself, which is no longer attached to anything.
                _context.Tags.Remove(tagToDelete);

                // 4. Save all these changes (removing relationships and deleting the tag) to the database.
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tag '{tagToDelete.Name}' and all its associations were deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }


    }
}