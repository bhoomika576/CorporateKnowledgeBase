using CorporateKnowledgeBase.Web.Models;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.Services
{
    /// <summary>
    /// Defines the contract for a service that handles notification creation.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Creates and saves a notification for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user to notify.</param>
        /// <param name="message">The notification message content.</param>
        /// <param name="url">The URL the user will be redirected to upon clicking the notification.</param>
        Task CreateNotificationAsync(string userId, string message, string url);

        /// <summary>
        /// Creates notifications for relevant users when a new blog post is published.
        /// </summary>
        /// <param name="blogPost">The newly created blog post.</param>
        Task CreateNotificationsForNewBlogPostAsync(BlogPost blogPost);

        /// <summary>
        /// Creates notifications for relevant users when a new technical document is published.
        /// </summary>
        /// <param name="document">The newly created technical document.</param>
        Task CreateNotificationsForNewDocumentAsync(TechnicalDocument document);

        /// <summary>
        /// Creates notifications for relevant users when a new announcement is published.
        /// </summary>
        /// <param name="announcement">The newly created announcement.</param>
        Task CreateNotificationsForNewAnnouncementAsync(Announcement announcement);

        /// <summary>
        /// Creates a notification for the content author when a new comment is posted.
        /// </summary>
        /// <param name="comment">The newly created comment.</param>
        Task CreateNotificationForNewCommentAsync(Comment comment);
    }
}