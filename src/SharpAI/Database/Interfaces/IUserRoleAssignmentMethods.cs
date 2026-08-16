namespace SharpAI.Database.Interfaces
{
    using System.Collections.Generic;

    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for user role assignments. Handwritten, provider-aware SQL.
    /// </summary>
    public interface IUserRoleAssignmentMethods
    {
        /// <summary>Create an assignment.</summary>
        /// <param name="assignment">Assignment.</param>
        /// <returns>The created assignment.</returns>
        UserRoleAssignment Create(UserRoleAssignment assignment);

        /// <summary>Read an assignment by identifier.</summary>
        /// <param name="guid">Assignment identifier.</param>
        /// <returns>The assignment, or null.</returns>
        UserRoleAssignment Read(string guid);

        /// <summary>Get the active assignments for a user within a tenant.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="userGuid">User identifier.</param>
        /// <returns>Assignments; never null.</returns>
        List<UserRoleAssignment> GetForUser(string tenantGuid, string userGuid);

        /// <summary>Delete an assignment by identifier.</summary>
        /// <param name="guid">Assignment identifier.</param>
        void Delete(string guid);

        /// <summary>Page through assignments for a user within a tenant.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="userGuid">User identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<UserRoleAssignment> Enumerate(string tenantGuid, string userGuid, EnumerationQuery query);
    }
}
