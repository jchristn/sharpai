namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using SharpAI.Database.Interfaces;
    using SharpAI.Models;
    using SharpAI.Security;

    /// <summary>
    /// Handwritten-SQL implementation of the security audit log.
    /// </summary>
    internal sealed class AuditMethods : IAuditMethods
    {
        private const string _Cols =
            "guid, tenantguid, eventtype, principaltype, principalguid, method, path, ipaddress, " +
            "authresult, authzresult, denialreason, statuscode, createdutc";

        private readonly DatabaseDriverBase _Db;

        public AuditMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public void Create(AuditLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _Db.NonQuery(
                "INSERT INTO audit_log (" + _Cols + ") VALUES (@guid, @tenantguid, @eventtype, @principaltype, " +
                "@principalguid, @method, @path, @ipaddress, @authresult, @authzresult, @denialreason, @statuscode, @createdutc)",
                Params(entry));
        }

        public AuditLogEntry Read(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM audit_log WHERE guid = @guid", One("@guid", guid));
            return t.Rows.Count > 0 ? Map(t.Rows[0]) : null;
        }

        public EnumerationResult<AuditLogEntry> Enumerate(string tenantGuid, EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            EnumerationResult<AuditLogEntry> result = new EnumerationResult<AuditLogEntry> { MaxResults = query.PageSize, Skip = query.Offset };
            Dictionary<string, object> p = new Dictionary<string, object>();
            string where = String.Empty;
            if (!String.IsNullOrEmpty(tenantGuid)) { where = " WHERE tenantguid = @tenantguid"; p["@tenantguid"] = tenantGuid; }

            long total = Convert.ToInt64(_Db.Scalar("SELECT COUNT(*) FROM audit_log" + where, p));
            result.TotalRecords = total;
            DataTable t = _Db.Query("SELECT " + _Cols + " FROM audit_log" + where + " ORDER BY createdutc DESC " + _Db.BuildPaging(query.PageSize, query.Offset), p);
            List<AuditLogEntry> list = new List<AuditLogEntry>();
            foreach (DataRow row in t.Rows) list.Add(Map(row));
            result.Objects = list;
            result.RecordsRemaining = Math.Max(0, total - ((long)query.Offset + list.Count));
            result.EndOfResults = result.RecordsRemaining <= 0;
            return result;
        }

        public int Prune(DateTime olderThanUtc)
        {
            return _Db.NonQuery("DELETE FROM audit_log WHERE createdutc < @cutoff", One("@cutoff", DbRow.Iso(olderThanUtc)));
        }

        private static Dictionary<string, object> One(string key, object value)
        {
            return new Dictionary<string, object> { { key, value } };
        }

        private static Dictionary<string, object> Params(AuditLogEntry e)
        {
            return new Dictionary<string, object>
            {
                { "@guid", e.Guid },
                { "@tenantguid", (object)e.TenantGuid },
                { "@eventtype", e.EventType },
                { "@principaltype", e.PrincipalType.ToString() },
                { "@principalguid", (object)e.PrincipalGuid },
                { "@method", (object)e.Method },
                { "@path", (object)e.Path },
                { "@ipaddress", (object)e.IpAddress },
                { "@authresult", e.AuthResult ? 1 : 0 },
                { "@authzresult", e.AuthzResult ? 1 : 0 },
                { "@denialreason", (object)e.DenialReason },
                { "@statuscode", e.StatusCode },
                { "@createdutc", DbRow.Iso(e.CreatedUtc) }
            };
        }

        private static AuditLogEntry Map(DataRow row)
        {
            AuditLogEntry e = new AuditLogEntry
            {
                Guid = DbRow.Str(row, "guid"),
                TenantGuid = DbRow.NullStr(row, "tenantguid"),
                EventType = DbRow.Str(row, "eventtype"),
                PrincipalGuid = DbRow.NullStr(row, "principalguid"),
                Method = DbRow.NullStr(row, "method"),
                Path = DbRow.NullStr(row, "path"),
                IpAddress = DbRow.NullStr(row, "ipaddress"),
                AuthResult = DbRow.Bool(row, "authresult"),
                AuthzResult = DbRow.Bool(row, "authzresult"),
                DenialReason = DbRow.NullStr(row, "denialreason"),
                StatusCode = (int)DbRow.Long(row, "statuscode"),
                CreatedUtc = DbRow.Dt(row, "createdutc")
            };

            if (Enum.TryParse<PrincipalTypeEnum>(DbRow.Str(row, "principaltype"), true, out PrincipalTypeEnum pt)) e.PrincipalType = pt;
            return e;
        }
    }
}
