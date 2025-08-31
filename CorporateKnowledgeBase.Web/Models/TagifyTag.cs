namespace CorporateKnowledgeBase.Web.Models
{
    /// <summary>
    /// A helper class to deserialize the JSON object coming from the Tagify.js frontend library.
    /// </summary>
    public class TagifyTag
    {
        public string? value { get; set; }
    }
}