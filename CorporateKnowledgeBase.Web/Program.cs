using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Enums;
using CorporateKnowledgeBase.Web.Models;
using CorporateKnowledgeBase.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;

namespace CorporateKnowledgeBase.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //--------------------------------------------------------------------
            // 1. SERVICE REGISTRATION (Dependency Injection Container)
            //--------------------------------------------------------------------

            #region Database & Identity Services
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                            .AddRoles<IdentityRole>()
                            .AddEntityFrameworkStores<ApplicationDbContext>();
            #endregion


            #region Application Services
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            #endregion



            #region Session & Cookie Configuration
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.SlidingExpiration = true;
            });
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            #endregion


            //--------------------------------------------------------------------
            // 2. HTTP REQUEST PIPELINE CONFIGURATION
            //--------------------------------------------------------------------
            var app = builder.Build();

            // Configure pipeline for the development environment.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // The order of middleware is crucial.
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();


            // Map Controller and Page routes.
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();


            //--------------------------------------------------------------------
            // 3. SEED INITIAL DATA
            //--------------------------------------------------------------------
            // This block ensures that the necessary roles, admin user, and initial categories
            // exist in the database every time the application starts.
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var configuration = services.GetRequiredService<IConfiguration>();

                    await SeedRolesAsync(roleManager, logger);
                    await SeedAdminUserAsync(userManager, configuration, logger);
                    await SeedCategoriesAsync(context, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during data seeding.");
                }
            }

            // Run the application.
            app.Run();
        }

        #region Seeding Helper Methods
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            logger.LogInformation("Seeding roles...");
            foreach (var roleName in Enum.GetNames<RoleEnums>())
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation("Role '{RoleName}' created.", roleName);
                }
            }
        }

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
        {
            logger.LogInformation("Seeding admin user...");
            string adminEmail = "admin@knowledgebase.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Admin",
                    Surname = "User",
                    EmailConfirmed = true
                };

                string adminPassword = configuration["AdminUser:Password"] ?? throw new InvalidOperationException("AdminUser:Password not found in secrets.json.");
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, RoleEnums.Admin.ToString());
                    logger.LogInformation("Admin user created and assigned to Admin role.");
                }
                else
                {
                    logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding initial categories...");
            if (!await context.Categories.AnyAsync())
            {
                var categories = new Category[]
                {
                    new() { Name = "Backend" },
                    new() { Name = "Frontend" },
                    new() { Name = "Database" },
                    new() { Name = "General" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
                logger.LogInformation("Initial categories have been seeded.");
            }
        }
        #endregion
    }
}