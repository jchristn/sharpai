namespace SharpAI.Database.Interfaces
{
    using System.Collections.Generic;

    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for credential scope assignments. Handwritten, provider-aware SQL.
    /// </summary>
    public interface ICredentialScopeAssignmentMethods
    {
        /// <summary>Create an assignment.</summary>
        /// <param name="assignment">Assignment.</param>
        /// <returns>The created assignment.</returns>
        CredentialScopeAssignment Create(CredentialScopeAssignment assignment);

        /// <summary>Read an assignment by identifier.</summary>
        /// <param name="guid">Assignment identifier.</param>
        /// <returns>The assignment, or null.</returns>
        CredentialScopeAssignment Read(string guid);

        /// <summary>Get the active assignments for a credential within a tenant.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="credentialGuid">Credential identifier.</param>
        /// <returns>Assignments; never null.</returns>
        List<CredentialScopeAssignment> GetForCredential(string tenantGuid, string credentialGuid);

        /// <summary>Delete an assignment by identifier.</summary>
        /// <param name="guid">Assignment identifier.</param>
        void Delete(string guid);
    }
}
