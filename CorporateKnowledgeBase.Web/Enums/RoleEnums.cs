namespace CorporateKnowledgeBase.Web.Enums
{
    /// <summary>
    /// Defines the user roles used throughout the application.
    /// Using an enum prevents magic strings and reduces the risk of typos.
    /// </summary>
    public enum RoleEnums
    {
        /// <summary>
        /// The administrator with full access to the system.
        /// </summary>
        Admin,

        /// <summary>
        /// A content creator, typically a member of the development team.
        /// </summary>
        Developer,

        /// <summary>
        /// A standard user with read-only access to content, but can comment.
        /// </summary>
        Employee
    }
}