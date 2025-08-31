using CorporateKnowledgeBase.Web.Areas.Admin.Models;
using CorporateKnowledgeBase.Web.Data;
using CorporateKnowledgeBase.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Manages users and their roles in the Admin area.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        /// <summary>
        /// Displays a list of all users.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                                          .OrderBy(u => u.Name)
                                          .ThenBy(u => u.Surname)
                                          .ToListAsync();
            return View(users);
        }


        /// <summary>
        /// Displays the role management page for a specific user.
        /// </summary>
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Roles = []
            };

            // Get all roles, but filter out any that might have a null name.
            var allRoles = await _roleManager.Roles
                                             .Where(r => r.Name != null)
                                             .ToListAsync();

            // Check all roles and see if the user has them.
            foreach (var role in allRoles)
            {
                viewModel.Roles.Add(new UserRoleViewModel
                {
                    // We can use the null-forgiving operator (!) here because we have already filtered out the nulls.
                    RoleName = role.Name!,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name!)
                });
            }
            TempData["InfoMessage"] = $"Now managing roles for user '{user.FullName}'.";
            return View(viewModel);
        }

        /// <summary>
        /// Handles the submission of updated roles for a user.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Kullanıcının mevcut rollerini al
            var userRoles = await _userManager.GetRolesAsync(user);

            // Seçilen rolleri mevcut rollerle karşılaştır ve ekle/kaldır
            foreach (var roleViewModel in model.Roles)
            {
                // Eğer rol seçilmişse ve kullanıcı bu rolde değilse, ekle.
                if (roleViewModel.IsSelected && !userRoles.Contains(roleViewModel.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, roleViewModel.RoleName);
                }
                // Eğer rol seçilmemişse ve kullanıcı bu roldeyse, kaldır.
                else if (!roleViewModel.IsSelected && userRoles.Contains(roleViewModel.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, roleViewModel.RoleName);
                }
            }

            TempData["SuccessMessage"] = $"Roles for user '{user.FullName}' were updated successfully.";
            return RedirectToAction(nameof(Index));
        }



    }
}