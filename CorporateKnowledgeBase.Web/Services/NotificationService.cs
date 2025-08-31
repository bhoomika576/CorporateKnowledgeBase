using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CorporateKnowledgeBase.Web.Services
{
    public class NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : INotificationService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;

        public async Task CreateNotificationAsync(string userId, string message, string url)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(message))
            {
                return;
            }

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Url = url,
                CreatedDate = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationsForNewBlogPostAsync(BlogPost blogPost)
        {
            var postAuthor = await _context.Users.FindAsync(blogPost.AuthorId);
            var authorName = postAuthor?.FullName ?? "System"; // Safely get the author's name.

            // Selects users who want to receive blog notifications.
            var usersToNotify = await _userManager.Users
                .Where(u => u.Id != blogPost.AuthorId && u.NotifyOnNewBlogPost)
                .ToListAsync();

            var messageFormat = "'{0}' published a new blog post: '{1}'";

            foreach (var user in usersToNotify)
            {
                var message = string.Format(messageFormat, authorName, blogPost.Title);
                var url = $"/Blog/Details/{blogPost.Id}";
                await CreateNotificationAsync(user.Id, message, url);
            }
        }

        public async Task CreateNotificationsForNewDocumentAsync(TechnicalDocument document)
        {
            var docAuthor = await _context.Users.FindAsync(document.AuthorId);
            var authorName = docAuthor?.FullName ?? "System";

            // Selects users who want to receive document notifications.
            var usersToNotify = await _userManager.Users
                .Where(u => u.Id != document.AuthorId && u.NotifyOnNewDocument)
                .ToListAsync();

            var messageFormat = "'{0}' published a new technical document: '{1}'";

            foreach (var user in usersToNotify)
            {
                var message = string.Format(messageFormat, authorName, document.Title);
                var url = $"/Document/Details/{document.Id}";
                await CreateNotificationAsync(user.Id, message, url);
            }
        }

        public async Task CreateNotificationsForNewAnnouncementAsync(Announcement announcement)
        {
            // Selects users who want to receive announcement notifications.
            var usersToNotify = await _userManager.Users
                .Where(u => u.Id != announcement.AuthorId && u.NotifyOnNewAnnouncement)
                .ToListAsync();

            var messageFormat = "A new announcement was published: '{0}'";

            foreach (var user in usersToNotify)
            {
                var message = string.Format(messageFormat, announcement.Title);
                var url = $"/Announcement/Details/{announcement.Id}";
                await CreateNotificationAsync(user.Id, message, url);
            }
        }

        public async Task CreateNotificationForNewCommentAsync(Comment comment)
        {
            var commenter = await _context.Users.FindAsync(comment.AuthorId);
            if (commenter == null) return; // If commenter not found, exit.

            if (comment.BlogPostId.HasValue)
            {
                var blogPost = await _context.BlogPosts.Include(b => b.Author)
                                 .FirstOrDefaultAsync(b => b.Id == comment.BlogPostId.Value);

                // Checks if the post author wants to receive comment notifications.
                if (blogPost?.Author != null && comment.AuthorId != blogPost.AuthorId && blogPost.Author.NotifyOnNewComment)
                {
                    if (blogPost.AuthorId != null)
                    {
                        var messageFormat = "'{0}' commented on your post: '{1}'";
                        var message = string.Format(messageFormat, commenter.FullName, blogPost.Title);
                        var url = $"/Blog/Details/{blogPost.Id}?commentId={comment.Id}";
                        await CreateNotificationAsync(blogPost.AuthorId, message, url);
                    }
                }
            }
            else if (comment.TechnicalDocumentId.HasValue)
            {
                var document = await _context.TechnicalDocuments.Include(d => d.Author)
                                 .FirstOrDefaultAsync(d => d.Id == comment.TechnicalDocumentId.Value);

                // Checks if the document author wants to receive comment notifications.
                if (document?.Author != null && comment.AuthorId != document.AuthorId && document.Author.NotifyOnNewComment)
                {
                    if (document.AuthorId != null)
                    {
                        var messageFormat = "'{0}' commented on your document: '{1}'";
                        var message = string.Format(messageFormat, commenter.FullName, document.Title);
                        var url = $"/Document/Details/{document.Id}?commentId={comment.Id}";
                        await CreateNotificationAsync(document.AuthorId, message, url);
                    }
                }
            }
        }
    }
}