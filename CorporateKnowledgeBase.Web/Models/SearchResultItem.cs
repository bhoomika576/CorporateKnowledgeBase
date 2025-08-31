namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// Represents a single, standardized item in a search results list.
    /// It's used to display results from different sources (blogs, documents, etc.) uniformly.
    /// </summary>
    public class SearchResultItem
    {
        public string Title { get; set; } = string.Empty;
        public string ContentSnippet { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}