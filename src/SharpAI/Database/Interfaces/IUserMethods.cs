namespace SharpAI.Database.Interfaces
{
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Data-access methods for users. Handwritten, provider-aware SQL. Enumeration is tenant-scoped.
    /// </summary>
    public interface IUserMethods
    {
        /// <summary>Create a user.</summary>
        /// <param name="user">User.</param>
        /// <returns>The created user.</returns>
        User Create(User user);

        /// <summary>Read a user by identifier.</summary>
        /// <param name="guid">User identifier.</param>
        /// <returns>The user, or null.</returns>
        User Read(string guid);

        /// <summary>Read a user by tenant-scoped email.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="email">Email address.</param>
        /// <returns>The user, or null.</returns>
        User GetByEmail(string tenantGuid, string email);

        /// <summary>Update a user.</summary>
        /// <param name="user">User.</param>
        /// <returns>The updated user.</returns>
        User Update(User user);

        /// <summary>Delete a user by identifier.</summary>
        /// <param name="guid">User identifier.</param>
        void Delete(string guid);

        /// <summary>Page through users within a tenant.</summary>
        /// <param name="tenantGuid">Tenant identifier.</param>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<User> Enumerate(string tenantGuid, EnumerationQuery query);
    }
}
