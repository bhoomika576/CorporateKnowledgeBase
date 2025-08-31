using System.Collections.Generic;

namespace CorporateKnowledgeBase.Web.Areas.Admin.Models
{
    /// <summary>
    /// ViewModel for the Manage Roles page, holding the user's ID, name, and a list of available roles.
    /// </summary>
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<UserRoleViewModel> Roles { get; set; } = [];
    }

    /// <summary>
    /// Represents a single role in the role management checklist.
    /// </summary>
    public class UserRoleViewModel
    {
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}