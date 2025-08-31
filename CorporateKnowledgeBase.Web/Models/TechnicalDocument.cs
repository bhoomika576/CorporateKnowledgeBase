using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// Represents a technical document, typically containing code snippets, tutorials, or guides.
    /// </summary>
    public class TechnicalDocument
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The title of the technical document.
        /// </summary>
        [StringLength(200)]
        public required string Title { get; set; }

        /// <summary>
        /// The main content of the document, expected to be in Markdown format.
        /// </summary>
        public required string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Foreign key for the author (ApplicationUser).
        public string? AuthorId { get; set; }

        // Foreign key for the category.
        public int CategoryId { get; set; }

        /// <summary>
        /// The number of times the document has been viewed.
        /// </summary>
        public int ViewCount { get; set; } = 0;

        // Navigation properties
        public virtual Category? Category { get; set; }
        public virtual ApplicationUser? Author { get; set; }
        public virtual ICollection<Tag> Tags { get; set; } = [];
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}