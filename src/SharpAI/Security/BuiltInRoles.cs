namespace SharpAI.Security
{
    using System.Collections.Generic;

    /// <summary>
    /// The catalog of built-in RBAC roles seeded at database initialization and available to every tenant.
    /// Built-in roles carry a null tenant identifier, are marked built-in and protected, and are immutable
    /// through tenant REST APIs (clone before customizing).
    /// </summary>
    public static class BuiltInRoles
    {
        #region Public-Members

        /// <summary>Tenant-wide administrator: all operations on all resource types. Inherits to children.</summary>
        public const string TenantAdmin = "TenantAdmin";

        /// <summary>Security administrator: Admin on security/tenancy resources.</summary>
        public const string SecurityAdmin = "SecurityAdmin";

        /// <summary>Read-only auditor across security and audit surfaces.</summary>
        public const string Auditor = "Auditor";

        /// <summary>Editor: read/write/delete on domain data resources.</summary>
        public const string Editor = "Editor";

        /// <summary>Viewer: read-only on domain data resources.</summary>
        public const string Viewer = "Viewer";

        /// <summary>Minimal tenant-presence role (self-read).</summary>
        public const string TenantMember = "TenantMember";

        #endregion

        #region Public-Methods

        /// <summary>
        /// The built-in role definitions, in seed order.
        /// </summary>
        /// <returns>Definitions; never null.</returns>
        public static List<BuiltInRoleDefinition> Definitions()
        {
            List<BuiltInRoleDefinition> definitions = new List<BuiltInRoleDefinition>();

            definitions.Add(new BuiltInRoleDefinition
            {
                Name = TenantAdmin,
                Permissions = new List<Permission>
                {
                    Permit("TenantAdmin all", new List<string> { ResourceTypes.All }, new List<string> { OperationTypeEnum.All.ToString() })
                }
            });

            List<string> securityResources = new List<string>
            {
                ResourceTypes.User, ResourceTypes.Credential, ResourceTypes.Session, ResourceTypes.Role,
                ResourceTypes.Permission, ResourceTypes.Assignment, ResourceTypes.Audit, ResourceTypes.Tenant, ResourceTypes.Admin
            };

            definitions.Add(new BuiltInRoleDefinition
            {
                Name = SecurityAdmin,
                Permissions = new List<Permission>
                {
                    Permit("SecurityAdmin manage", securityResources, new List<string> { OperationTypeEnum.Admin.ToString(), OperationTypeEnum.Read.ToString(), OperationTypeEnum.Write.ToString() })
                }
            });

            definitions.Add(new BuiltInRoleDefinition
            {
                Name = Auditor,
                Permissions = new List<Permission>
                {
                    Permit("Auditor read", securityResources, new List<string> { OperationTypeEnum.Read.ToString() })
                }
            });

            List<string> dataResources = new List<string>
            {
                ResourceTypes.Model, ResourceTypes.Inference, ResourceTypes.RequestHistory
            };

            definitions.Add(new BuiltInRoleDefinition
            {
                Name = Editor,
                Permissions = new List<Permission>
                {
                    Permit("Editor read", dataResources, new List<string> { OperationTypeEnum.Read.ToString() }),
                    Permit("Editor write", dataResources, new List<string> { OperationTypeEnum.Write.ToString() }),
                    Permit("Editor execute", new List<string> { ResourceTypes.Inference }, new List<string> { OperationTypeEnum.Execute.ToString() })
                }
            });

            definitions.Add(new BuiltInRoleDefinition
            {
                Name = Viewer,
                Permissions = new List<Permission>
                {
                    Permit("Viewer read", dataResources, new List<string> { OperationTypeEnum.Read.ToString() })
                }
            });

            definitions.Add(new BuiltInRoleDefinition
            {
                Name = TenantMember,
                Permissions = new List<Permission>
                {
                    Permit("TenantMember self read", new List<string> { ResourceTypes.User }, new List<string> { OperationTypeEnum.Read.ToString() })
                }
            });

            return definitions;
        }

        #endregion

        #region Private-Methods

        private static Permission Permit(string name, List<string> resourceTypes, List<string> operationTypes)
        {
            return new Permission
            {
                TenantGuid = null,
                Name = name,
                ResourceTypes = resourceTypes,
                OperationTypes = operationTypes,
                Effect = PermissionEffectEnum.Permit,
                IsProtected = true
            };
        }

        #endregion
    }
}
