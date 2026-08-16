namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    using SharpAI.Database.Interfaces;
    using SharpAI.Models;

    /// <summary>
    /// Handwritten-SQL implementation of the model registry methods. The SQL is portable ANSI executed
    /// through the driver's connection-agnostic helpers, with only paging supplied by the provider. Values
    /// are stored portably: GUIDs and timestamps as text, booleans as 0/1 integers.
    /// </summary>
    internal sealed class ModelRegistryMethods : IModelRegistryMethods
    {
        #region Private-Members

        private const string _AllColumns =
            "guid, modelname, parentmodelname, modelformat, modelfamily, contentlength, parametercount, " +
            "md5, sha1, sha256, sourceurl, parametersize, quantization, embeddings, completions, " +
            "modelcreationutc, createdutc";

        private static readonly string _TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        public ModelRegistryMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        public EnumerationResult<ModelFile> Enumerate(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();

            EnumerationResult<ModelFile> result = new EnumerationResult<ModelFile>
            {
                MaxResults = query.PageSize,
                Skip = query.Offset
            };

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string where = BuildWhere(query, parameters);

            long total = Convert.ToInt64(_Db.Scalar("SELECT COUNT(*) FROM model_files" + where, parameters));
            result.TotalRecords = total;

            string orderBy = OrderByClause(query.Order);
            string paging = _Db.BuildPaging(query.PageSize, query.Offset);

            DataTable table = _Db.Query("SELECT " + _AllColumns + " FROM model_files" + where + " " + orderBy + " " + paging, parameters);
            result.Objects = MapAll(table);

            long consumed = (long)query.Offset + result.Objects.Count;
            result.RecordsRemaining = Math.Max(0, total - consumed);
            result.EndOfResults = result.RecordsRemaining <= 0;

            if (result.Objects.Count > 0 && result.RecordsRemaining > 0)
            {
                result.ContinuationToken = result.Objects[result.Objects.Count - 1].GUID;
            }

            result.IterationsRequired = 1;
            return result;
        }

        public ModelFile GetByGuid(Guid guid)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@guid", guid.ToString() } };
            DataTable table = _Db.Query("SELECT " + _AllColumns + " FROM model_files WHERE guid = @guid", parameters);
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        public ModelFile GetByName(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@modelname", name } };
            DataTable table = _Db.Query("SELECT " + _AllColumns + " FROM model_files WHERE modelname = @modelname", parameters);
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        public List<ModelFile> GetMany(List<Guid> guids)
        {
            List<ModelFile> results = new List<ModelFile>();
            if (guids == null || guids.Count == 0) return results;

            List<Guid> distinct = guids.Distinct().ToList();
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            List<string> placeholders = new List<string>();
            for (int i = 0; i < distinct.Count; i++)
            {
                string key = "@g" + i;
                placeholders.Add(key);
                parameters[key] = distinct[i].ToString();
            }

            DataTable table = _Db.Query(
                "SELECT " + _AllColumns + " FROM model_files WHERE guid IN (" + String.Join(", ", placeholders) + ")",
                parameters);
            return MapAll(table);
        }

        public bool ExistsByGuid(Guid guid)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@guid", guid.ToString() } };
            long count = Convert.ToInt64(_Db.Scalar("SELECT COUNT(*) FROM model_files WHERE guid = @guid", parameters));
            return count > 0;
        }

        public ModelFile Add(ModelFile modelFile)
        {
            if (modelFile == null) throw new ArgumentNullException(nameof(modelFile));

            ModelFile existing = GetByName(modelFile.Name);
            if (existing != null) return existing;

            Dictionary<string, object> parameters = ToParameters(modelFile);
            _Db.NonQuery(
                "INSERT INTO model_files (" + _AllColumns + ") VALUES (" +
                "@guid, @modelname, @parentmodelname, @modelformat, @modelfamily, @contentlength, @parametercount, " +
                "@md5, @sha1, @sha256, @sourceurl, @parametersize, @quantization, @embeddings, @completions, " +
                "@modelcreationutc, @createdutc)",
                parameters);

            return modelFile;
        }

        public ModelFile Update(ModelFile modelFile)
        {
            if (modelFile == null) throw new ArgumentNullException(nameof(modelFile));

            Dictionary<string, object> parameters = ToParameters(modelFile);
            _Db.NonQuery(
                "UPDATE model_files SET " +
                "modelname = @modelname, parentmodelname = @parentmodelname, modelformat = @modelformat, " +
                "modelfamily = @modelfamily, contentlength = @contentlength, parametercount = @parametercount, " +
                "md5 = @md5, sha1 = @sha1, sha256 = @sha256, sourceurl = @sourceurl, parametersize = @parametersize, " +
                "quantization = @quantization, embeddings = @embeddings, completions = @completions, " +
                "modelcreationutc = @modelcreationutc, createdutc = @createdutc WHERE guid = @guid",
                parameters);

            return modelFile;
        }

        public void Delete(Guid guid)
        {
            if (!ExistsByGuid(guid)) throw new KeyNotFoundException("The specified object does not exist.");

            Dictionary<string, object> parameters = new Dictionary<string, object> { { "@guid", guid.ToString() } };
            _Db.NonQuery("DELETE FROM model_files WHERE guid = @guid", parameters);
        }

        #endregion

        #region Private-Methods

        private Dictionary<string, object> ToParameters(ModelFile m)
        {
            return new Dictionary<string, object>
            {
                { "@guid", m.GUID.ToString() },
                { "@modelname", m.Name },
                { "@parentmodelname", (object)m.ParentModel },
                { "@modelformat", m.Format },
                { "@modelfamily", m.Family },
                { "@contentlength", m.ContentLength },
                { "@parametercount", m.ParameterCount },
                { "@md5", m.MD5Hash },
                { "@sha1", (object)m.SHA1Hash },
                { "@sha256", (object)m.SHA256Hash },
                { "@sourceurl", (object)m.SourceUrl },
                { "@parametersize", (object)m.ParameterSize },
                { "@quantization", (object)m.Quantization },
                { "@embeddings", m.Embeddings ? 1 : 0 },
                { "@completions", m.Completions ? 1 : 0 },
                { "@modelcreationutc", (object)(m.ModelCreationUtc.HasValue ? m.ModelCreationUtc.Value.ToUniversalTime().ToString(_TimeFormat) : null) },
                { "@createdutc", m.CreatedUtc.ToUniversalTime().ToString(_TimeFormat) }
            };
        }

        private List<ModelFile> MapAll(DataTable table)
        {
            List<ModelFile> list = new List<ModelFile>();
            foreach (DataRow row in table.Rows) list.Add(MapRow(row));
            return list;
        }

        private ModelFile MapRow(DataRow row)
        {
            ModelFile m = new ModelFile
            {
                GUID = Guid.Parse(GetString(row, "guid")),
                Name = GetString(row, "modelname"),
                ParentModel = GetNullableString(row, "parentmodelname"),
                Format = GetString(row, "modelformat"),
                Family = GetString(row, "modelfamily"),
                ContentLength = GetLong(row, "contentlength"),
                ParameterCount = GetLong(row, "parametercount"),
                MD5Hash = GetString(row, "md5"),
                SHA1Hash = GetNullableString(row, "sha1"),
                SHA256Hash = GetNullableString(row, "sha256"),
                SourceUrl = GetNullableString(row, "sourceurl"),
                ParameterSize = GetNullableString(row, "parametersize"),
                Quantization = GetNullableString(row, "quantization"),
                Embeddings = GetLong(row, "embeddings") != 0,
                Completions = GetLong(row, "completions") != 0,
                ModelCreationUtc = GetNullableDateTime(row, "modelcreationutc"),
                CreatedUtc = GetDateTime(row, "createdutc")
            };
            return m;
        }

        private string BuildWhere(EnumerationQuery query, Dictionary<string, object> parameters)
        {
            List<string> clauses = new List<string>();

            if (!String.IsNullOrEmpty(query.Name))
            {
                clauses.Add("modelname = @f_name");
                parameters["@f_name"] = query.Name;
            }

            if (!String.IsNullOrEmpty(query.Family))
            {
                clauses.Add("modelfamily = @f_family");
                parameters["@f_family"] = query.Family;
            }

            if (!String.IsNullOrEmpty(query.Quantization))
            {
                clauses.Add("quantization = @f_quant");
                parameters["@f_quant"] = query.Quantization;
            }

            if (!String.IsNullOrEmpty(query.Format))
            {
                clauses.Add("modelformat = @f_format");
                parameters["@f_format"] = query.Format;
            }

            if (query.CreatedAfter.HasValue)
            {
                clauses.Add("createdutc > @f_ca");
                parameters["@f_ca"] = query.CreatedAfter.Value.ToUniversalTime().ToString(_TimeFormat);
            }

            if (query.CreatedBefore.HasValue)
            {
                clauses.Add("createdutc < @f_cb");
                parameters["@f_cb"] = query.CreatedBefore.Value.ToUniversalTime().ToString(_TimeFormat);
            }

            return clauses.Count > 0 ? " WHERE " + String.Join(" AND ", clauses) : String.Empty;
        }

        private string OrderByClause(EnumerationOrderEnum ordering)
        {
            switch (ordering)
            {
                case EnumerationOrderEnum.SizeAscending: return "ORDER BY contentlength ASC";
                case EnumerationOrderEnum.SizeDescending: return "ORDER BY contentlength DESC";
                case EnumerationOrderEnum.NameAscending: return "ORDER BY modelname ASC";
                case EnumerationOrderEnum.NameDescending: return "ORDER BY modelname DESC";
                case EnumerationOrderEnum.CreatedAscending: return "ORDER BY createdutc ASC";
                case EnumerationOrderEnum.CreatedDescending:
                default: return "ORDER BY createdutc DESC";
            }
        }

        private static string GetString(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? String.Empty : value.ToString();
        }

        private static string GetNullableString(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? null : value.ToString();
        }

        private static long GetLong(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
        }

        private static DateTime GetDateTime(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value) return DateTime.UtcNow;
            if (value is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        private static DateTime? GetNullableDateTime(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        #endregion
    }
}
