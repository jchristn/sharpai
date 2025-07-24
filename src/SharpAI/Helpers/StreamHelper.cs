namespace SharpAI.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Stream helpers.
    /// </summary>
    public static class StreamHelper
    {
        /// <summary>
        /// Read a stream fully.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="position">Position from which to read.</param>
        /// <returns>Bytes.</returns>
        public static byte[] ReadFully(Stream stream, int position = 0)
        {
            if (stream == null || !stream.CanRead) return Array.Empty<byte>();

            stream.Seek(position, SeekOrigin.Begin);

            using (MemoryStream ms = new MemoryStream())
            {
                int bufferSize = 65536;
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                return ms.ToArray();
            }
        }
    }
}
