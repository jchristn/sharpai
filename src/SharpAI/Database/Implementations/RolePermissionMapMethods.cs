namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SharpAI.Database.Interfaces;
    using SharpAI.Security;

    /// <summary>
    /// Handwritten-SQL implementation of role↔permission mapping data access.
    /// </summary>
    internal sealed class RolePermissionMapMethods : IRolePermissionMapMethods
    {
        private const string _Cols = "guid, tenantguid, roleguid, permissionguid, active, isprotected, createdutc, lastupdateutc";
        private readonly DatabaseDriverBase _Db;

        public RolePermissionMapMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public RolePermissionMap Create(RolePermissionMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            _Db.NonQuery(
                "INSERT INTO rolepermissionmaps (" + _Cols + ") VALUES (@guid, @tenantguid, @roleguid, @permissionguid, @active, @isprotected, @createdutc, @lastupdateutc)",
                Params(map));
            return map;
        }

        public List<RolePermissionMap> GetByRole(string roleGuid)
        {
            if (String.IsNullOrEmpty(roleGuid)) throw new ArgumentNullException(nameof(roleGuid));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM rolepermissionmaps WHERE roleguid = @roleguid", One("@roleguid", roleGuid));
            List<RolePermissionMap> list = new List<RolePermissionMap>();
            foreach (DataRow row in t.Rows) list.Add(Map(row));
            return list;
        }

        public void Delete(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Db.NonQuery("DELETE FROM rolepermissionmaps WHERE guid = @guid", One("@guid", guid));
        }

        private static Dictionary<string, object> One(string key, object value)
        {
            return new Dictionary<string, object> { { key, value } };
        }

        private static Dictionary<string, object> Params(RolePermissionMap m)
        {
            return new Dictionary<string, object>
            {
                { "@guid", m.Guid },
                { "@tenantguid", (object)m.TenantGuid },
                { "@roleguid", m.RoleGuid },
                { "@permissionguid", m.PermissionGuid },
                { "@active", m.Active ? 1 : 0 },
                { "@isprotected", m.IsProtected ? 1 : 0 },
                { "@createdutc", DbRow.Iso(m.CreatedUtc) },
                { "@lastupdateutc", DbRow.Iso(m.LastUpdateUtc) }
            };
        }

        private static RolePermissionMap Map(DataRow row)
        {
            return new RolePermissionMap
            {
                Guid = DbRow.Str(row, "guid"),
                TenantGuid = DbRow.NullStr(row, "tenantguid"),
                RoleGuid = DbRow.Str(row, "roleguid"),
                PermissionGuid = DbRow.Str(row, "permissionguid"),
                Active = DbRow.Bool(row, "active"),
                IsProtected = DbRow.Bool(row, "isprotected"),
                CreatedUtc = DbRow.Dt(row, "createdutc"),
                LastUpdateUtc = DbRow.Dt(row, "lastupdateutc")
            };
        }
    }
}
