using System.ComponentModel.DataAnnotations;

namespace CorporateKnowledgeBase.Web.Models
{
    public class NotificationSettingsViewModel
    {
        [Display(Name = "When a new comment is posted on my content")]
        public bool NotifyOnNewComment { get; set; }

        [Display(Name = "When a new blog post is published")]
        public bool NotifyOnNewBlogPost { get; set; }

        [Display(Name = "When a new technical document is published")]
        public bool NotifyOnNewDocument { get; set; }

        [Display(Name = "When a new announcement is published")]
        public bool NotifyOnNewAnnouncement { get; set; }
    }
}