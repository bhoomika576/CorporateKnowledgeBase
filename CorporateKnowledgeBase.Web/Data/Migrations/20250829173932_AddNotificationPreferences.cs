using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorporateKnowledgeBase.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewAnnouncement",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewBlogPost",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewComment",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewDocument",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyOnNewAnnouncement",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnNewBlogPost",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnNewComment",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnNewDocument",
                table: "AspNetUsers");
        }
    }
}
