using System.ComponentModel.DataAnnotations;

namespace CorporateKnowledgeBase.Web.Models
{
    public class CreateCommentViewModel
    {
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public int? BlogPostId { get; set; }
        public int? TechnicalDocumentId { get; set; }
    }
}