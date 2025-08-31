namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// ViewModel for the home page, containing lists of various content types to display.
    /// </summary>
    public class HomeViewModel
    {
        public List<Announcement> RecentAnnouncements { get; set; } = [];
        public List<BlogPost> RecentBlogPosts { get; set; } = [];
        public List<TechnicalDocument> RecentDocuments { get; set; } = [];
        public List<object> RecentlyViewedItems { get; set; } = [];
    }
}