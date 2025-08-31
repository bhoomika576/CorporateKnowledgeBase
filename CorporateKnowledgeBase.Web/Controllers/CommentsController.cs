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
    /// Manages the creation of comments on content.
    /// </summary>
    [Authorize] // All actions require the user to be logged in.
    public class CommentsController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        INotificationService notificationService
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly INotificationService _notificationService = notificationService;

        /// <summary>
        /// Handles the creation of a new comment via an AJAX POST request.
        /// </summary>
        /// <param name="viewModel">The view model containing the comment data.</param>
        /// <returns>A partial view of the newly created comment on success, or a BadRequest on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCommentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var newComment = new Comment
                {
                    Content = viewModel.Content,
                    AuthorId = user.Id,
                    CreatedDate = DateTime.Now,
                    BlogPostId = viewModel.BlogPostId,
                    TechnicalDocumentId = viewModel.TechnicalDocumentId
                };

                _context.Comments.Add(newComment);
                await _context.SaveChangesAsync();

                // After saving, retrieve the comment again with its author to pass to the partial view.
                var savedComment = await _context.Comments
                                                 .Include(c => c.Author)
                                                 .FirstOrDefaultAsync(c => c.Id == newComment.Id);

                if (savedComment != null)
                {
                    await _notificationService.CreateNotificationForNewCommentAsync(savedComment);
                }

                // Return a partial view containing only the HTML for the new comment.
                return PartialView("~/Views/Shared/_CommentPartial.cshtml", savedComment);
            }

            // If the model is not valid, return a BadRequest with the validation errors.
            return BadRequest(ModelState);
        }
    }
}