namespace SharpAI.Security
{
    using System;
    using System.Collections.Generic;

    using SharpAI.Database;

    /// <summary>
    /// Seeds the immutable, globally-visible built-in roles (and their permissions and role↔permission
    /// mappings) into the database if they are missing. Idempotent: a role that already exists is left
    /// untouched, so historical assignments keep resolving by name.
    /// </summary>
    public static class RbacSeeder
    {
        #region Public-Methods

        /// <summary>
        /// Seed any missing built-in roles.
        /// </summary>
        /// <param name="database">Initialized database driver. May not be null.</param>
        /// <returns>The number of roles created.</returns>
        public static int Seed(DatabaseDriverBase database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            int created = 0;

            foreach (BuiltInRoleDefinition definition in BuiltInRoles.Definitions())
            {
                UserRole existing = database.Roles.GetBuiltInByName(definition.Name);
                if (existing != null) continue;

                UserRole role = database.Roles.Create(new UserRole
                {
                    TenantGuid = null,
                    Name = definition.Name,
                    IsBuiltIn = true,
                    IsProtected = true
                });

                foreach (Permission permission in definition.Permissions)
                {
                    permission.TenantGuid = null;
                    permission.IsProtected = true;
                    database.Permissions.Create(permission);

                    database.RolePermissionMaps.Create(new RolePermissionMap
                    {
                        TenantGuid = null,
                        RoleGuid = role.Guid,
                        PermissionGuid = permission.Guid,
                        IsProtected = true
                    });
                }

                created++;
            }

            return created;
        }

        #endregion
    }
}
