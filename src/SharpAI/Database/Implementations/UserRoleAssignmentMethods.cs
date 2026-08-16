namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SharpAI.Database.Interfaces;
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Handwritten-SQL implementation of user role assignment data access.
    /// </summary>
    internal sealed class UserRoleAssignmentMethods : IUserRoleAssignmentMethods
    {
        private const string _Cols =
            "guid, tenantguid, userguid, roleguid, rolename, resourcescope, resourceguid, inheritstochildren, " +
            "active, isprotected, createdutc, lastupdateutc";

        private readonly DatabaseDriverBase _Db;

        public UserRoleAssignmentMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public UserRoleAssignment Create(UserRoleAssignment assignment)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            _Db.NonQuery(
                "INSERT INTO userroleassignments (" + _Cols + ") VALUES (@guid, @tenantguid, @userguid, @roleguid, @rolename, " +
                "@resourcescope, @resourceguid, @inheritstochildren, @active, @isprotected, @createdutc, @lastupdateutc)",
                Params(assignment));
            return assignment;
        }

        public UserRoleAssignment Read(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM userroleassignments WHERE guid = @guid", One("@guid", guid));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public List<UserRoleAssignment> GetForUser(string tenantGuid, string userGuid)
        {
            if (String.IsNullOrEmpty(tenantGuid)) throw new ArgumentNullException(nameof(tenantGuid));
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            DataTable t = _Db.Query(
                "SELECT " + _Cols + " FROM userroleassignments WHERE tenantguid = @tenantguid AND userguid = @userguid AND active = 1",
                new Dictionary<string, object> { { "@tenantguid", tenantGuid }, { "@userguid", userGuid } });
            List<UserRoleAssignment> list = new List<UserRoleAssignment>();
            foreach (DataRow row in t.Rows) list.Add(Map(row));
            return list;
        }

        public void Delete(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Db.NonQuery("DELETE FROM userroleassignments WHERE guid = @guid", One("@guid", guid));
        }

        public EnumerationResult<UserRoleAssignment> Enumerate(string tenantGuid, string userGuid, EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            EnumerationResult<UserRoleAssignment> result = new EnumerationResult<UserRoleAssignment> { MaxResults = query.PageSize, Skip = query.Offset };
            Dictionary<string, object> p = new Dictionary<string, object> { { "@tenantguid", tenantGuid }, { "@userguid", userGuid } };
            long total = Convert.ToInt64(_Db.Scalar("SELECT COUNT(*) FROM userroleassignments WHERE tenantguid = @tenantguid AND userguid = @userguid", p));
            result.TotalRecords = total;
            DataTable t = _Db.Query(
                "SELECT " + _Cols + " FROM userroleassignments WHERE tenantguid = @tenantguid AND userguid = @userguid ORDER BY createdutc DESC " + _Db.BuildPaging(query.PageSize, query.Offset), p);
            List<UserRoleAssignment> list = new List<UserRoleAssignment>();
            foreach (DataRow row in t.Rows) list.Add(Map(row));
            result.Objects = list;
            result.RecordsRemaining = Math.Max(0, total - ((long)query.Offset + list.Count));
            result.EndOfResults = result.RecordsRemaining <= 0;
            return result;
        }

        private static Dictionary<string, object> One(string key, object value)
        {
            return new Dictionary<string, object> { { key, value } };
        }

        private static Dictionary<string, object> Params(UserRoleAssignment a)
        {
            return new Dictionary<string, object>
            {
                { "@guid", a.Guid },
                { "@tenantguid", a.TenantGuid },
                { "@userguid", a.UserGuid },
                { "@roleguid", (object)a.RoleGuid },
                { "@rolename", (object)a.RoleName },
                { "@resourcescope", a.ResourceScope.ToString() },
                { "@resourceguid", (object)a.ResourceGuid },
                { "@inheritstochildren", a.InheritsToChildren ? 1 : 0 },
                { "@active", a.Active ? 1 : 0 },
                { "@isprotected", a.IsProtected ? 1 : 0 },
                { "@createdutc", DbRow.Iso(a.CreatedUtc) },
                { "@lastupdateutc", DbRow.Iso(a.LastUpdateUtc) }
            };
        }

        private static UserRoleAssignment Map(DataRow row)
        {
            UserRoleAssignment a = new UserRoleAssignment
            {
                Guid = DbRow.Str(row, "guid"),
                TenantGuid = DbRow.Str(row, "tenantguid"),
                UserGuid = DbRow.Str(row, "userguid"),
                RoleGuid = DbRow.NullStr(row, "roleguid"),
                RoleName = DbRow.NullStr(row, "rolename"),
                ResourceGuid = DbRow.NullStr(row, "resourceguid"),
                InheritsToChildren = DbRow.Bool(row, "inheritstochildren"),
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
