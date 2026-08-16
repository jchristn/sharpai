namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SharpAI.Database.Interfaces;
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Handwritten-SQL implementation of RBAC role data access.
    /// </summary>
    internal sealed class RoleMethods : IRoleMethods
    {
        private const string _Cols = "guid, tenantguid, name, isbuiltin, active, isprotected, createdutc, lastupdateutc";
        private readonly DatabaseDriverBase _Db;

        public RoleMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public UserRole Create(UserRole role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            _Db.NonQuery(
                "INSERT INTO userroles (" + _Cols + ") VALUES (@guid, @tenantguid, @name, @isbuiltin, @active, @isprotected, @createdutc, @lastupdateutc)",
                Params(role));
            return role;
        }

        public UserRole Read(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM userroles WHERE guid = @guid", One("@guid", guid));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public UserRole GetBuiltInByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM userroles WHERE name = @name AND tenantguid IS NULL", One("@name", name));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public UserRole GetByName(string tenantGuid, string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            DataTable t = _Db.Query(
                "SELECT " + _Cols + " FROM userroles WHERE name = @name AND tenantguid = @tenantguid",
                new Dictionary<string, object> { { "@name", name }, { "@tenantguid", (object)tenantGuid } });
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public UserRole Update(UserRole role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            role.LastUpdateUtc = DateTime.UtcNow;
            _Db.NonQuery(
                "UPDATE userroles SET name = @name, isbuiltin = @isbuiltin, active = @active, isprotected = @isprotected, lastupdateutc = @lastupdateutc WHERE guid = @guid",
                Params(role));
            return role;
        }

        public void Delete(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Db.NonQuery("DELETE FROM userroles WHERE guid = @guid", One("@guid", guid));
        }

        public EnumerationResult<UserRole> Enumerate(string tenantGuid, EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            EnumerationResult<UserRole> result = new EnumerationResult<UserRole> { MaxResults = query.PageSize, Skip = query.Offset };
            Dictionary<string, object> p = new Dictionary<string, object> { { "@tenantguid", (object)tenantGuid } };
            long total = Convert.ToInt64(_Db.Scalar("SELECT COUNT(*) FROM userroles WHERE tenantguid = @tenantguid OR tenantguid IS NULL", p));
            result.TotalRecords = total;
            DataTable t = _Db.Query(
                "SELECT " + _Cols + " FROM userroles WHERE tenantguid = @tenantguid OR tenantguid IS NULL ORDER BY createdutc DESC " + _Db.BuildPaging(query.PageSize, query.Offset), p);
            List<UserRole> list = new List<UserRole>();
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

        private static Dictionary<string, object> Params(UserRole r)
        {
            return new Dictionary<string, object>
            {
                { "@guid", r.Guid },
                { "@tenantguid", (object)r.TenantGuid },
                { "@name", r.Name },
                { "@isbuiltin", r.IsBuiltIn ? 1 : 0 },
                { "@active", r.Active ? 1 : 0 },
                { "@isprotected", r.IsProtected ? 1 : 0 },
                { "@createdutc", DbRow.Iso(r.CreatedUtc) },
                { "@lastupdateutc", DbRow.Iso(r.LastUpdateUtc) }
            };
        }

        private static UserRole Map(DataRow row)
        {
            return new UserRole
            {
                Guid = DbRow.Str(row, "guid"),
                TenantGuid = DbRow.NullStr(row, "tenantguid"),
                Name = DbRow.Str(row, "name"),
                IsBuiltIn = DbRow.Bool(row, "isbuiltin"),
                Active = DbRow.Bool(row, "active"),
                IsProtected = DbRow.Bool(row, "isprotected"),
                CreatedUtc = DbRow.Dt(row, "createdutc"),
                LastUpdateUtc = DbRow.Dt(row, "lastupdateutc")
            };
        }
    }
}
