using System;

namespace Channels.Http2
{
    /// <summary>
    /// Represents the encoding options to use with this header
    /// </summary>
    /// <remarks>
    /// bits: 0-4 control indexing (8 options, 6 used)
    /// bits: 5-6 control name compression (4 options, 3 used)
    /// bits: 7-8 control value compression (4 options, 3 used)
    /// </remarks>
    [Flags]
    public enum HeaderOptions
    {
        /// <summary>
        /// Acts as <see cref="ExistingValue"/> if a matching header exists in the index;
        /// a pre-defined set of frequently used header names will be treated as <see cref="AddNewValue"/>
        /// otherwise acts as <see cref="NotIndexed"/>; the name may use pre-existing matching names
        /// from the lookup table
        /// </summary>
        IndexAutomatic = 0,
        /// <summary>
        /// The name and value to be used should be accessed as a pre-existing value from the
        /// lookup table
        /// </summary>
        IndexExistingValue = 1,
        /// <summary>
        /// The value to be used is specified and should not added to the index lookup table; the name
        /// may use pre-existing matching names from the lookup table
        /// </summary>
        IndexAddNewValue = 2,
        /// <summary>
        /// The value to be used is specified but should not be indexed; the name
        /// may use pre-existing matching names from the lookup table
        /// </summary>
        IndexNotIndexed = 3, // not a mistake; see remarks
        /// <summary>
        /// The value to be used is specified but should **never** be indexed, including
        /// by intermediaries; the name may use pre-existing matching names from the lookup table
        /// </summary>
        IndexNeverIndexed = 4, // not a mistake; see remarks
        /// <summary>
        /// Signals a change to the size of the dynamic table
        /// </summary>
        IndexResize = 5, // not a mistake; see remarks
        /// <summary>
        /// Mask to apply to query the "index" option
        /// </summary>
        IndexMask = 15,

        /// <summary>
        /// Names are compressed if that would be shorter, else not compressed
        /// </summary>
        NameCompressionAutomatic = 0,
        /// <summary>
        /// Names are compressed
        /// </summary>
        NameCompressionOn = 1 << 4,
        /// <summary>
        /// Names are not compressed
        /// </summary>
        NameCompressionOff = 2 << 4,
        /// <summary>
        /// Mask to apply to query the "name compression" option
        /// </summary>
        NameCompressionMask = 3 << 4,

        /// <summary>
        /// Values are compressed if that would be shorter, else not compressed
        /// </summary>
        ValueCompressionAutomatic = 0,
        /// <summary>
        /// Values are compressed
        /// </summary>
        ValueCompressionOn = 1 << 6,
        /// <summary>
        /// Values are not compressed
        /// </summary>
        ValueCompressionOff = 2 << 6,
        /// <summary>
        /// Mask to apply to query the "value compression" option
        /// </summary>
        ValueCompressionMask = 3 << 6,
    }
}
