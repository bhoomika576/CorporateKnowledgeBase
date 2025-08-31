namespace CorporateKnowledgeBase.Web.Areas.Admin.Models
{
    /// <summary>
    /// ViewModel for displaying category information in the admin area, including usage count.
    /// </summary>
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }
}