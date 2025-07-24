namespace SharpAI.Serialization
{
    using System;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Text.Json;

    /// <summary>
    /// Exception converter.
    /// </summary>
    /// <typeparam name="TExceptionType">Exception type.</typeparam>
    public class ExceptionConverter<TExceptionType> : JsonConverter<TExceptionType>
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.

        /// <summary>
        /// Can convert.
        /// </summary>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <returns>True if convertible.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">JSON serializer options.</param>
        /// <returns>Instance.</returns>
        public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing exceptions is not allowed");
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="writer">Writer.</param>
        /// <param name="value">Value.</param>
        /// <param name="options">JSON serializer options.</param>
        public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
        {
            var serializableProperties = value.GetType()
                .GetProperties()
                .Select(uu => new { uu.Name, Value = uu.GetValue(value) })
                .Where(uu => uu.Name != nameof(Exception.TargetSite));

            if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            {
                serializableProperties = serializableProperties.Where(uu => uu.Value != null);
            }

            var propList = serializableProperties.ToList();

            if (propList.Count == 0)
            {
                // Nothing to write
                return;
            }

            writer.WriteStartObject();

            foreach (var prop in propList)
            {
                writer.WritePropertyName(prop.Name);
                JsonSerializer.Serialize(writer, prop.Value, options);
            }

            writer.WriteEndObject();
        }

#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }
}
