using fbognini.Core.Utilities;

namespace fbognini.WebFramework.Plugins.DataTables
{
    public class DtOrderDir : ConstantClass
    {
        public static readonly DtOrderDir ASC = new DtOrderDir("asc");
        public static readonly DtOrderDir DESC = new DtOrderDir("desc");

        /// <summary>
        /// This constant constructor does not need to be called if the constant
        /// you are attempting to use is already defined as a static instance of 
        /// this class.
        /// This constructor should be used to construct constants that are not
        /// defined as statics, for instance if attempting to use a feature that is
        /// newer than the current version of the SDK.
        /// </summary>
        public DtOrderDir(string value)
            : base(value)
        {
        }

        /// <summary>
        /// Finds the constant for the unique value.
        /// </summary>
        /// <param name="value">The unique value for the constant</param>
        /// <returns>The constant for the unique value</returns>
        public static DtOrderDir FindValue(string value)
        {
            return FindValue<DtOrderDir>(value);
        }

        /// <summary>
        /// Utility method to convert strings to the constant class.
        /// </summary>
        /// <param name="value">The string value to convert to the constant class.</param>
        /// <returns></returns>
        public static implicit operator DtOrderDir(string value)
        {
            return FindValue(value);
        }
    }
}
