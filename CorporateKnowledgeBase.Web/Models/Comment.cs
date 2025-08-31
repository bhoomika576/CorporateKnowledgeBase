using System.ComponentModel.DataAnnotations;

namespace CorporateKnowledgeBase.Web.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "The comment cannot be longer than 1000 characters.")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // The user who authored the comment.
        [Required]
        public string AuthorId { get; set; } = string.Empty;
        public virtual ApplicationUser Author { get; set; } = null!;

        // Foreign key for the BlogPost this comment belongs to (if any).
        public int? BlogPostId { get; set; }
        public virtual BlogPost? BlogPost { get; set; }

        // Foreign key for the TechnicalDocument this comment belongs to (if any).
        public int? TechnicalDocumentId { get; set; }
        public virtual TechnicalDocument? TechnicalDocument { get; set; }
    }
}