using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CorporateKnowledgeBase.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        
        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [Required]
        [StringLength(50)]
        public string? Surname { get; set; }

        // A computed property for the user's full name.
        [StringLength(101)]
        public string FullName => $"{Name} {Surname}";

        public string? ProfilePictureUrl { get; set; }


        // Navigation properties
        public virtual ICollection<BlogPost> BlogPosts { get; set; } = [];
        public virtual ICollection<TechnicalDocument> TechnicalDocuments { get; set; } = [];

        
        // Property to track the last login date.
        public DateTime? LastLoginDate { get; set; }

        
        // Navigation property for notifications.
        public virtual ICollection<Notification> Notifications { get; set; } = [];

        
        // Notification preferences.
        [DefaultValue(true)]
        public bool NotifyOnNewComment { get; set; } = true;

        [DefaultValue(true)]
        public bool NotifyOnNewBlogPost { get; set; } = true;

        [DefaultValue(true)]
        public bool NotifyOnNewDocument { get; set; } = true;

        [DefaultValue(true)]
        public bool NotifyOnNewAnnouncement { get; set; } = true;
    }
}