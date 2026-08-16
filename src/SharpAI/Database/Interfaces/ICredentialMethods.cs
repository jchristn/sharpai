namespace SharpAI.Database.Interfaces
{
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for credentials. Handwritten, provider-aware SQL. Enumeration is tenant-scoped.
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>Create a credential.</summary>
        /// <param name="credential">Credential.</param>
        /// <returns>The created credential.</returns>
        Credential Create(Credential credential);

        /// <summary>Read a credential by identifier.</summary>
        /// <param name="guid">Credential identifier.</param>
        /// <returns>The credential, or null.</returns>
        Credential Read(string guid);

        /// <summary>Read a credential by access key.</summary>
        /// <param name="accessKey">Access key.</param>
        /// <returns>The credential, or null.</returns>
        Credential GetByAccessKey(string accessKey);

        /// <summary>Update a credential.</summary>
        /// <param name="credential">Credential.</param>
        /// <returns>The updated credential.</returns>
        Credential Update(Credential credential);

        /// <summary>Delete a credential by identifier.</summary>
        /// <param name="guid">Credential identifier.</param>
        void Delete(string guid);

        /// <summary>Page through credentials within a tenant.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<Credential> Enumerate(string tenantGuid, EnumerationQuery query);
    }
}
