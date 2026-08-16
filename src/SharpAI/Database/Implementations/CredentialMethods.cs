namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SharpAI.Database.Interfaces;
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Handwritten-SQL implementation of credential data access. Enumeration is tenant-scoped.
    /// </summary>
    internal sealed class CredentialMethods : ICredentialMethods
    {
        private const string _Cols =
            "guid, userguid, tenantguid, name, accesskey, secretsha256, active, isprotected, " +
            "createdutc, lastupdateutc, lastusedutc, expiresutc";

        private readonly DatabaseDriverBase _Db;

        public CredentialMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Credential Create(Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            _Db.NonQuery(
                "INSERT INTO credentials (" + _Cols + ") VALUES (@guid, @userguid, @tenantguid, @name, @accesskey, " +
                "@secretsha256, @active, @isprotected, @createdutc, @lastupdateutc, @lastusedutc, @expiresutc)",
                Params(credential));
            return credential;
        }

        public Credential Read(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM credentials WHERE guid = @guid", One("@guid", guid));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public Credential GetByAccessKey(string accessKey)
        {
            if (String.IsNullOrEmpty(accessKey)) throw new ArgumentNullException(nameof(accessKey));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM credentials WHERE accesskey = @accesskey", One("@accesskey", accessKey));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public Credential Update(Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            credential.LastUpdateUtc = DateTime.UtcNow;
            _Db.NonQuery(
                "UPDATE credentials SET userguid = @userguid, tenantguid = @tenantguid, name = @name, accesskey = @accesskey, " +
                "secretsha256 = @secretsha256, active = @active, isprotected = @isprotected, lastupdateutc = @lastupdateutc, " +
                "lastusedutc = @lastusedutc, expiresutc = @expiresutc WHERE guid = @guid",
                Params(credential));
            return credential;
        }

        public void Delete(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            _Db.NonQuery("DELETE FROM credentials WHERE guid = @guid", One("@guid", guid));
        }

        public EnumerationResult<Credential> Enumerate(string tenantGuid, EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            EnumerationResult<Credential> result = new EnumerationResult<Credential> { MaxResults = query.PageSize, Skip = query.Offset };
            Dictionary<string, object> p = new Dictionary<string, object>();
            string where = String.Empty;
            if (!String.IsNullOrEmpty(tenantGuid)) { where = " WHERE tenantguid = @tenantguid"; p["@tenantguid"] = tenantGuid; }

            long total = Convert.ToInt64(_Db.Scalar("SELECT COUNT(*) FROM credentials" + where, p));
            result.TotalRecords = total;
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM credentials" + where + " ORDER BY createdutc DESC " + _Db.BuildPaging(query.PageSize, query.Offset), p);
            List<Credential> list = new List<Credential>();
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

        private static Dictionary<string, object> Params(Credential c)
        {
            return new Dictionary<string, object>
            {
                { "@guid", c.Guid },
                { "@userguid", c.UserGuid },
                { "@tenantguid", c.TenantGuid },
                { "@name", c.Name },
                { "@accesskey", c.AccessKey },
                { "@secretsha256", c.SecretSha256 },
                { "@active", c.Active ? 1 : 0 },
                { "@isprotected", c.IsProtected ? 1 : 0 },
                { "@createdutc", DbRow.Iso(c.CreatedUtc) },
                { "@lastupdateutc", DbRow.Iso(c.LastUpdateUtc) },
                { "@lastusedutc", DbRow.IsoOrNull(c.LastUsedUtc) },
                { "@expiresutc", DbRow.IsoOrNull(c.ExpiresUtc) }
            };
        }

        private static Credential Map(DataRow row)
        {
            return new Credential
            {
                Guid = DbRow.Str(row, "guid"),
                UserGuid = DbRow.Str(row, "userguid"),
                TenantGuid = DbRow.Str(row, "tenantguid"),
                Name = DbRow.Str(row, "name"),
                AccessKey = DbRow.Str(row, "accesskey"),
                SecretSha256 = DbRow.Str(row, "secretsha256"),
                Active = DbRow.Bool(row, "active"),
                IsProtected = DbRow.Bool(row, "isprotected"),
                CreatedUtc = DbRow.Dt(row, "createdutc"),
                LastUpdateUtc = DbRow.Dt(row, "lastupdateutc"),
                LastUsedUtc = DbRow.NullDt(row, "lastusedutc"),
                ExpiresUtc = DbRow.NullDt(row, "expiresutc")
            };
        }
    }
}
