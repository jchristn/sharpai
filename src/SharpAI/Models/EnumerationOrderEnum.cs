namespace SharpAI.Models
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Enumeration order.
    /// </summary>
    public enum EnumerationOrderEnum
    {
        /// <summary>
        /// CreatedAscending.
        /// </summary>
        [EnumMember(Value = "CreatedAscending")]
        CreatedAscending,
        /// <summary>
        /// CreatedDescending.
        /// </summary>
        [EnumMember(Value = "CreatedDescending")]
        CreatedDescending,
        /// <summary>
        /// SizeAscending.
        /// </summary>
        [EnumMember(Value = "SizeAscending")]
        SizeAscending,
        /// <summary>
        /// SizeDescending.
        /// </summary>
        [EnumMember(Value = "SizeDescending")]
        SizeDescending,
        /// <summary>
        /// NameAscending.
        /// </summary>
        [EnumMember(Value = "NameAscending")]
        NameAscending,
        /// <summary>
        /// KeyDescending.
        /// </summary>
        [EnumMember(Value = "NameDescending")]
        NameDescending
    }
}
