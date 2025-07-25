﻿namespace SharpAI.Hosting
{
    using System;

    /// <summary>
    /// Represents a GGUF file with enhanced metadata including quantization information.
    /// </summary>
    public class GgufFileInfo : HuggingFaceModelFile
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        #region Public-Members

        /// <summary>
        /// Gets or sets the human-readable formatted file size.
        /// </summary>
        public string SizeFormatted { get; set; } = null;

        /// <summary>
        /// Gets or sets the detected quantization type (e.g., "Q4_K_M", "Q5_K_S").
        /// </summary>
        public string QuantizationType { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether this is the main model file (not a shard).
        /// </summary>
        public bool IsMainModel { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Initializes a new instance of the GgufFileInfo class.
        /// </summary>
        public GgufFileInfo()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Returns a string representation of the GGUF file information.
        /// </summary>
        /// <returns>A formatted string containing GGUF-specific information.</returns>
        public override string ToString()
        {
            return $"GGUF: {Path} ({SizeFormatted}) - {QuantizationType}";
        }

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}