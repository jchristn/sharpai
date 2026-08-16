namespace SharpAI.Security
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A built-in role definition: a role name plus the permissions it grants. Used to seed the immutable,
    /// globally-visible platform roles at startup.
    /// </summary>
    public class BuiltInRoleDefinition
    {
        #region Public-Members

        /// <summary>
        /// Role name.
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// The permissions this role grants. Never null.
        /// </summary>
        public List<Permission> Permissions
        {
            get
            {
                return _Permissions;
            }
            set
            {
                _Permissions = value ?? new List<Permission>();
            }
        }

        #endregion

        #region Private-Members

        private List<Permission> _Permissions = new List<Permission>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BuiltInRoleDefinition()
        {
        }

        #endregion
    }
}
