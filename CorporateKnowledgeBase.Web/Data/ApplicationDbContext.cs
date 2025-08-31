using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CorporateKnowledgeBase.Web.Data
{
    /// <summary>
    /// The Entity Framework database context for the application.
    /// It includes DbSets for all application models and Identity tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<TechnicalDocument> TechnicalDocuments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the many-to-many relationship between BlogPost and Tag.
            // Using an explicit join table name is a good practice.
            builder.Entity<BlogPost>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.BlogPosts)
                .UsingEntity(j => j.ToTable("BlogPostTag"));

            // Configure the many-to-many relationship between TechnicalDocument and Tag.
            builder.Entity<TechnicalDocument>()
                .HasMany(d => d.Tags)
                .WithMany(t => t.TechnicalDocuments)
                .UsingEntity(j => j.ToTable("TagTechnicalDocument"));

            // Configure the default delete behavior to NoAction to prevent cascading deletes.
            // This helps maintain data integrity, especially in a system with interconnected data.
            foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}