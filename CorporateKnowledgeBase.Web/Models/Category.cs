using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// Represents a category that can be used to classify blog posts and documents.
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public required string Name { get; set; }

        // Navigation properties
        public virtual ICollection<BlogPost> BlogPosts { get; set; } = [];
        public virtual ICollection<TechnicalDocument> TechnicalDocuments { get; set; } = [];
    }
}