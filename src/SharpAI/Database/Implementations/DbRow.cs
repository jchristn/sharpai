namespace SharpAI.Database.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Text.Json;

    /// <summary>
    /// Portable helpers for reading provider-agnostic <see cref="DataRow"/> values and formatting values
    /// for storage. Timestamps are stored as sortable ISO-8601 UTC text; booleans as 0/1 integers.
    /// </summary>
    internal static class DbRow
    {
        internal const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        internal static string Str(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? String.Empty : value.ToString();
        }

        internal static string NullStr(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? null : value.ToString();
        }

        internal static long Long(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
        }

        internal static bool Bool(DataRow row, string column)
        {
            return Long(row, column) != 0;
        }

        internal static double Dbl(DataRow row, string column)
        {
            object value = row[column];
            return value == null || value == DBNull.Value ? 0 : Convert.ToDouble(value);
        }

        internal static DateTime Dt(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value) return DateTime.UtcNow;
            if (value is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        internal static DateTime? NullDt(DataRow row, string column)
        {
            object value = row[column];
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        internal static string Iso(DateTime value)
        {
            return value.ToUniversalTime().ToString(TimeFormat);
        }

        internal static string Json(List<string> values)
        {
            return JsonSerializer.Serialize(values ?? new List<string>());
        }

        internal static List<string> StrList(DataRow row, string column)
        {
            string raw = NullStr(row, column);
            if (String.IsNullOrEmpty(raw)) return new List<string>();
            try
            {
                List<string> parsed = JsonSerializer.Deserialize<List<string>>(raw);
                return parsed ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        internal static object IsoOrNull(DateTime? value)
        {
            return value.HasValue ? (object)value.Value.ToUniversalTime().ToString(TimeFormat) : null;
        }
    }
}
