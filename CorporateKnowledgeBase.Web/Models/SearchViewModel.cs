namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// ViewModel for the search results page, containing the query, results, and pagination info.
    /// </summary>
    public class SearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchResultItem> Results { get; set; } = [];
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}