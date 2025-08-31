using CorporateKnowledgeBase.Web.Models;
using System.Collections.Generic;

namespace CorporateKnowledgeBase.Web.Areas.Admin.Models
{
    /// <summary>
    /// ViewModel for the admin dashboard, containing key statistics and activity lists.
    /// </summary>
    public class DashboardViewModel
    {
        // Existing Stats
        public int UserCount { get; set; }
        public int BlogPostCount { get; set; }
        public int DocumentCount { get; set; }
        public int TagCount { get; set; }

        // New Lists for Recent Activity
        public List<BlogPost> LatestBlogPosts { get; set; } = [];
        public List<ApplicationUser> LatestUsers { get; set; } = [];

        // New List for Popular Content
        public List<TechnicalDocument> MostViewedDocuments { get; set; } = [];

        // New Data for Chart
        public List<string> ChartLabels { get; set; } = [];
        public List<int> ChartData { get; set; } = [];
    }
}