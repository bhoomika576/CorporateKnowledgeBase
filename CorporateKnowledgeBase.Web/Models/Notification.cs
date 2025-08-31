using System.ComponentModel.DataAnnotations;

namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// Represents a single notification for a specific user.
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // Foreign key for the user to whom the notification is sent.
        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The content of the notification message.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The URL to navigate to when the notification is clicked.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// A flag indicating whether the notification has been read by the user.
        /// </summary>
        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}