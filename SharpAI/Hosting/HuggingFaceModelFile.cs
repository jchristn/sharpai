namespace SharpAI.Hosting
{
    using System;

    /// <summary>
    /// Represents a file from a HuggingFace model repository with metadata.
    /// </summary>
    public class HuggingFaceModelFile
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        #region Public-Members

        /// <summary>
        /// Gets or sets the file path within the repository.
        /// </summary>
        public string Path { get; set; } = null;

        /// <summary>
        /// Gets or sets the file type ("file" or "directory").
        /// </summary>
        public string Type { get; set; } = null;

        /// <summary>
        /// Gets or sets the file size in bytes (null for directories).
        /// </summary>
        public long? Size { get; set; } = null;

        /// <summary>
        /// Gets or sets the last modified timestamp.
        /// </summary>
        public DateTime? LastModified { get; set; } = null;

        /// <summary>
        /// Gets or sets the Git object ID.
        /// </summary>
        public string Oid { get; set; } = null;

        /// <summary>
        /// Gets or sets the Large File Storage information.
        /// </summary>
        public string Lfs { get; set; } = null;

        /// <summary>
        /// Gets or sets the security scan status.
        /// </summary>
        public string SecurityStatus { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the HuggingFaceModelFile class.
        /// </summary>
        public HuggingFaceModelFile()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Returns a string representation of the HuggingFace model file.
        /// </summary>
        /// <returns>A formatted string containing path, type, and size information.</returns>
        public override string ToString()
        {
            return $"Path: {Path}, Type: {Type}, Size: {Size} bytes";
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}