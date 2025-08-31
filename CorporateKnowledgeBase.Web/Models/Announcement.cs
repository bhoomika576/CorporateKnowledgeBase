using System.ComponentModel.DataAnnotations;

namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// Represents an announcement, typically created by an administrator.
    /// </summary>
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public required string Title { get; set; }

        public required string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Foreign key for the author (ApplicationUser).
        public string? AuthorId { get; set; }

        // Navigation property
        public virtual ApplicationUser? Author { get; set; }
    }
}