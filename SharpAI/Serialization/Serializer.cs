namespace SharpAI.Serialization
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Serializer.
    /// </summary>
    public class Serializer
    {
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.

        #region Public-Members

        #endregion

        #region Private-Members

        private ExceptionConverter<Exception> _ExceptionConverter = new ExceptionConverter<Exception>();
        private NameValueCollectionConverter _NameValueCollectionConverter = new NameValueCollectionConverter();
        private DateTimeConverter _DateTimeConverter = new DateTimeConverter();
        private IPAddressConverter _IPAddressConverter = new IPAddressConverter();
        private StrictEnumConverterFactory _StrictEnumConverter = new StrictEnumConverterFactory();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Serializer()
        {
            InstantiateConverters();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Instantiation method to support fixups for various environments, e.g. Unity.
        /// </summary>
        public void InstantiateConverters()
        {
            try
            {
                Activator.CreateInstance<ExceptionConverter<Exception>>();
                Activator.CreateInstance<NameValueCollectionConverter>();
                Activator.CreateInstance<DateTimeConverter>();
                Activator.CreateInstance<IPAddressConverter>();
                Activator.CreateInstance<StrictEnumConverterFactory>();
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Instance.</returns>
        public T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

            options.Converters.Add(_ExceptionConverter);
            options.Converters.Add(_NameValueCollectionConverter);
            options.Converters.Add(_DateTimeConverter);
            options.Converters.Add(_IPAddressConverter);
            options.Converters.Add(_StrictEnumConverter);

            return JsonSerializer.Deserialize<T>(json, options);
        }

        /// <summary>
        /// Deserialize JSON to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="bytes">Bytes containing JSON.</param>
        /// <returns>Instance.</returns>
        public T DeserializeJson<T>(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 1) throw new ArgumentNullException(nameof(bytes));
            return DeserializeJson<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// Deserialize JSON from a file to an instance.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="filename">Filename.</param>
        /// <returns>Instance.</returns>
        public T DeserializeJsonFromFile<T>(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            string json = File.ReadAllText(filename);
            return DeserializeJson<T>(json);
        }

        /// <summary>
        /// Serialize object to JSON.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>JSON.</returns>
        public string SerializeJson(object obj, bool pretty = true)
        {
            if (obj == null) return null;

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // see https://github.com/dotnet/runtime/issues/43026
            options.Converters.Add(_ExceptionConverter);
            options.Converters.Add(_NameValueCollectionConverter);
            options.Converters.Add(_DateTimeConverter);
            options.Converters.Add(_IPAddressConverter);
            options.Converters.Add(_StrictEnumConverter);

            if (!pretty)
            {
                options.WriteIndented = false;
                return JsonSerializer.Serialize(obj, options);
            }
            else
            {
                options.WriteIndented = true;
                return JsonSerializer.Serialize(obj, options);
            }
        }

        /// <summary>
        /// Serialize obejct to JSON and write to a file.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        public void SerializeJsonToFile(string filename, object obj, bool pretty = true)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            string json = SerializeJson(obj, pretty);
            File.WriteAllText(filename, json);
        }

        /// <summary>
        /// Serialize an exception to JSON.
        /// </summary>
        /// <param name="e">Exception.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>JSON.</returns>
        [Obsolete("Exceptions should not be serialized.  Use .ToString() instead.")]
        public string SerializeJson(Exception e, bool pretty = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copy an object.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="o">Object.</param>
        /// <returns>Instance.</returns>
        public T CopyObject<T>(object o)
        {
            if (o == null) return default(T);
            string json = SerializeJson(o, false);
            T ret = DeserializeJson<T>(json);
            return ret;
        }

        /// <summary>
        /// Deserialize XML.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="bytes">XML data.</param>
        /// <returns>Instance.</returns>
        public T DeserializeXml<T>(byte[] bytes) where T : class
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            return DeserializeXml<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        /// Deserialize XML.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="xml">XML string.</param>
        /// <returns>Instance.</returns>
        public T DeserializeXml<T>(string xml) where T : class
        {
            if (String.IsNullOrEmpty(xml)) throw new ArgumentNullException(nameof(xml));

            // remove preamble if exists
            string byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            while (xml.StartsWith(byteOrderMarkUtf8, StringComparison.Ordinal))
            {
                xml = xml.Remove(0, byteOrderMarkUtf8.Length);
            }

            /*
             * 
             * This code respects the supplied namespace and validates it vs the model in code
             * 
             * 
            XmlSerializer xmls = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                return (T)xmls.Deserialize(ms);
            }
            */

            // The code that follows ignores namespaces

            T obj = null;

            using (TextReader textReader = new StringReader(xml))
            {
                using (XmlTextReader reader = new XmlTextReader(textReader))
                {
                    reader.Namespaces = false;
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    obj = (T)serializer.Deserialize(reader);
                }
            }

            return obj;
        }

        /// <summary>
        /// Serialize XML.
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="pretty">Pretty print.</param>
        /// <returns>XML string.</returns>
        public string SerializeXml(object obj, bool pretty = false)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            XmlSerializer xml = new XmlSerializer(obj.GetType());

            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = Encoding.GetEncoding("ISO-8859-1");
                settings.NewLineChars = Environment.NewLine;
                settings.ConformanceLevel = ConformanceLevel.Document;
                if (pretty) settings.Indent = true;

                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    xml.Serialize(new XmlWriterExtended(writer), obj);
                    byte[] bytes = stream.ToArray();
                    string ret = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                    // remove preamble if exists
                    string byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                    while (ret.StartsWith(byteOrderMarkUtf8, StringComparison.Ordinal))
                    {
                        ret = ret.Remove(0, byteOrderMarkUtf8.Length);
                    }

                    return ret;
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion

        #region Private-Classes

        private class XmlWriterExtended : XmlWriter
        {
            private XmlWriter baseWriter;

            public XmlWriterExtended(XmlWriter w)
            {
                baseWriter = w;
            }

            // Force WriteEndElement to use WriteFullEndElement
            public override void WriteEndElement() { baseWriter.WriteFullEndElement(); }

            public override void WriteFullEndElement()
            {
                baseWriter.WriteFullEndElement();
            }

            public override void Close()
            {
                baseWriter.Close();
            }

            public override void Flush()
            {
                baseWriter.Flush();
            }

            public override string LookupPrefix(string ns)
            {
                return (baseWriter.LookupPrefix(ns));
            }

            public override void WriteBase64(byte[] buffer, int index, int count)
            {
                baseWriter.WriteBase64(buffer, index, count);
            }

            public override void WriteCData(string text)
            {
                baseWriter.WriteCData(text);
            }

            public override void WriteCharEntity(char ch)
            {
                baseWriter.WriteCharEntity(ch);
            }

            public override void WriteChars(char[] buffer, int index, int count)
            {
                baseWriter.WriteChars(buffer, index, count);
            }

            public override void WriteComment(string text)
            {
                baseWriter.WriteComment(text);
            }

            public override void WriteDocType(string name, string pubid, string sysid, string subset)
            {
                baseWriter.WriteDocType(name, pubid, sysid, subset);
            }

            public override void WriteEndAttribute()
            {
                baseWriter.WriteEndAttribute();
            }

            public override void WriteEndDocument()
            {
                baseWriter.WriteEndDocument();
            }

            public override void WriteEntityRef(string name)
            {
                baseWriter.WriteEntityRef(name);
            }

            public override void WriteProcessingInstruction(string name, string text)
            {
                baseWriter.WriteProcessingInstruction(name, text);
            }

            public override void WriteRaw(string data)
            {
                baseWriter.WriteRaw(data);
            }

            public override void WriteRaw(char[] buffer, int index, int count)
            {
                baseWriter.WriteRaw(buffer, index, count);
            }

            public override void WriteStartAttribute(string prefix, string localName, string ns)
            {
                baseWriter.WriteStartAttribute(prefix, localName, ns);
            }

            public override void WriteStartDocument(bool standalone)
            {
                baseWriter.WriteStartDocument(standalone);
            }

            public override void WriteStartDocument()
            {
                baseWriter.WriteStartDocument();
            }

            public override void WriteStartElement(string prefix, string localName, string ns)
            {
                baseWriter.WriteStartElement(prefix, localName, ns);
            }

            public override WriteState WriteState
            {
                get { return baseWriter.WriteState; }
            }

            public override void WriteString(string text)
            {
                baseWriter.WriteString(text);
            }

            public override void WriteSurrogateCharEntity(char lowChar, char highChar)
            {
                baseWriter.WriteSurrogateCharEntity(lowChar, highChar);
            }

            public override void WriteWhitespace(string ws)
            {
                baseWriter.WriteWhitespace(ws);
            }
        }

        #endregion

#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    }
}