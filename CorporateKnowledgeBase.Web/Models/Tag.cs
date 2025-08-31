using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// Represents a tag that can be associated with multiple content types like blog posts and documents.
    /// </summary>
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public required string Name { get; set; }

        // Navigation properties for many-to-many relationships.
        public virtual ICollection<BlogPost> BlogPosts { get; set; } = [];
        public virtual ICollection<TechnicalDocument> TechnicalDocuments { get; set; } = [];
    }
}