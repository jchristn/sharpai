namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SharpAI.Database.Interfaces;
    using SharpAI.Security;

    /// <summary>
    /// Handwritten-SQL implementation of credential scope assignment data access.
    /// </summary>
    internal sealed class CredentialScopeAssignmentMethods : ICredentialScopeAssignmentMethods
    {
        private const string _Cols =
            "guid, tenantguid, credentialguid, roleguid, rolename, resourcescope, resourceguid, inheritstochildren, " +
            "permissions, resourcetypes, active, isprotected, createdutc, lastupdateutc";

        private readonly DatabaseDriverBase _Db;

        public CredentialScopeAssignmentMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public CredentialScopeAssignment Create(CredentialScopeAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            _Db.NonQuery(
                "INSERT INTO credentialscopeassignments (" + _Cols + ") VALUES (@guid, @tenantguid, @credentialguid, @roleguid, @rolename, " +
                "@resourcescope, @resourceguid, @inheritstochildren, @permissions, @resourcetypes, @active, @isprotected, @createdutc, @lastupdateutc)",
                Params(assignment));
            return assignment;
        }

        public CredentialScopeAssignment Read(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM credentialscopeassignments WHERE guid = @guid", One("@guid", guid));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public List<CredentialScopeAssignment> GetForCredential(string tenantGuid, string credentialGuid)
        {
            if (String.IsNullOrEmpty(tenantGuid)) throw new ArgumentNullException(nameof(tenantGuid));
            if (String.IsNullOrEmpty(credentialGuid)) throw new ArgumentNullException(nameof(credentialGuid));
            DataTable t = _Db.Query(
                "SELECT " + _Cols + " FROM credentialscopeassignments WHERE tenantguid = @tenantguid AND credentialguid = @credentialguid AND active = 1",
                new Dictionary<string, object> { { "@tenantguid", tenantGuid }, { "@credentialguid", credentialGuid } });
            List<CredentialScopeAssignment> list = new List<CredentialScopeAssignment>();
            foreach (DataRow row in t.Rows) list.Add(Map(row));
            return list;
        }

        public void Delete(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Db.NonQuery("DELETE FROM credentialscopeassignments WHERE guid = @guid", One("@guid", guid));
        }

        private static Dictionary<string, object> One(string key, object value)
        {
            return new Dictionary<string, object> { { key, value } };
        }

        private static Dictionary<string, object> Params(CredentialScopeAssignment a)
        {
            return new Dictionary<string, object>
            {
                { "@guid", a.Guid },
                { "@tenantguid", a.TenantGuid },
                { "@credentialguid", a.CredentialGuid },
                { "@roleguid", (object)a.RoleGuid },
                { "@rolename", (object)a.RoleName },
                { "@resourcescope", a.ResourceScope.ToString() },
                { "@resourceguid", (object)a.ResourceGuid },
                { "@inheritstochildren", a.InheritsToChildren ? 1 : 0 },
                { "@permissions", DbRow.Json(a.Permissions) },
                { "@resourcetypes", DbRow.Json(a.ResourceTypes) },
                { "@active", a.Active ? 1 : 0 },
                { "@isprotected", a.IsProtected ? 1 : 0 },
                { "@createdutc", DbRow.Iso(a.CreatedUtc) },
                { "@lastupdateutc", DbRow.Iso(a.LastUpdateUtc) }
            };
        }

        private static CredentialScopeAssignment Map(DataRow row)
        {
            CredentialScopeAssignment a = new CredentialScopeAssignment
            {
                Guid = DbRow.Str(row, "guid"),
                TenantGuid = DbRow.Str(row, "tenantguid"),
                CredentialGuid = DbRow.Str(row, "credentialguid"),
                RoleGuid = DbRow.NullStr(row, "roleguid"),
                RoleName = DbRow.NullStr(row, "rolename"),
                ResourceGuid = DbRow.NullStr(row, "resourceguid"),
                InheritsToChildren = DbRow.Bool(row, "inheritstochildren"),
                Permissions = DbRow.StrList(row, "permissions"),
                ResourceTypes = DbRow.StrList(row, "resourcetypes"),
                Active = DbRow.Bool(row, "active"),
                IsProtected = DbRow.Bool(row, "isprotected"),
                CreatedUtc = DbRow.Dt(row, "createdutc"),
                LastUpdateUtc = DbRow.Dt(row, "lastupdateutc")
            };

            if (Enum.TryParse<ResourceScopeEnum>(DbRow.Str(row, "resourcescope"), true, out ResourceScopeEnum scope)) a.ResourceScope = scope;
            return a;
        }
    }
}
