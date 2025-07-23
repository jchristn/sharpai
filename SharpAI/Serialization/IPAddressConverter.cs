namespace SharpAI.Serialization
{
    using System;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Text.Json;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;

    /// <summary>
    /// IP address converter.
    /// </summary>
    public class IPAddressConverter : JsonConverter<IPAddress>
    {
#pragma warning disable CS8600
#pragma warning disable CS8604

        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">JSON serializer options.</param>
        /// <returns>IPAddress.</returns>
        public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string str = reader.GetString();
            return IPAddress.Parse(str);
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="value">Value.</param>
        /// <param name="options">JSON serializer options.</param>
        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

#pragma warning restore CS8600
#pragma warning restore CS8604
    }
}
