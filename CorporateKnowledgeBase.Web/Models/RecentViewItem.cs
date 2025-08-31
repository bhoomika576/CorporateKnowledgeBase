namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    // A simple DTO (Data Transfer Object) to store information about a recently viewed item in the user's session.
    /// </summary>
    public class RecentViewItem
    {
        public string? ContentType { get; set; }
        public int ContentId { get; set; }
    }
}