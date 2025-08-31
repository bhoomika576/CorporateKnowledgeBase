namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// ViewModel for the user profile page, containing user details and lists of their created content.
    /// </summary>
    public class UserProfileViewModel
    {
        public ApplicationUser? User { get; set; }
        public ICollection<BlogPost>? BlogPosts { get; set; }
        public ICollection<TechnicalDocument>? TechnicalDocuments { get; set; }
    }
}