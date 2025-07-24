namespace SharpAI.Serialization
{
    using System;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// DateTime converter.
    /// </summary>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
#pragma warning disable CS8600

        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">JSON serializer options.</param>
        /// <returns>DateTime.</returns>
        public override DateTime Read(
                    ref Utf8JsonReader reader,
                    Type typeToConvert,
                    JsonSerializerOptions options)
        {
            string str = reader.GetString();

            DateTime val;
            if (DateTime.TryParse(str, out val)) return val;

            throw new FormatException("The JSON value '" + str + "' could not be converted to System.DateTime.");
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="dateTimeValue">Value.</param>
        /// <param name="options">JSON serializer options.</param>
        public override void Write(
            Utf8JsonWriter writer,
            DateTime dateTimeValue,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(dateTimeValue.ToString(
                "yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture));
        }

        private List<string> _AcceptedFormats = new List<string>
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-dd HH:mm:ss.ffffff",
            "yyyy-MM-ddTHH:mm:ss.ffffff",
            "yyyy-MM-ddTHH:mm:ss.fffffffK",
            "yyyy-MM-dd",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy hh:mm tt",
            "MM/dd/yyyy H:mm",
            "MM/dd/yyyy h:mm tt",
            "MM/dd/yyyy HH:mm:ss"
        };

#pragma warning restore CS8600
    }
}
