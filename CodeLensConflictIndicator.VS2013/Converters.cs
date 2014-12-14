using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CodeLens.ConflictIndicator
{
    /// <summary>
    /// Converter to convert string with multiple lines into single line
    /// </summary>
    [ValueConversion(typeof(object), typeof(string))]
    public class SingleLineTextConverter : IValueConverter
    {
        private const int ArtificialLimit = 2048;
        // This regex converts any two or more whitespaces or any one or more tab, return, newline, into a single space               
        private static Regex singleLineRegex = new Regex(@"([\t\r\n]+|\s{2,})", RegexOptions.Compiled);

        /// <summary>
        /// Convert string with multiple spaces (tabs, line breaks, spaces) to a string with only single space.
        /// This converter also arbitrarily truncates really long strings, as the point is to be shown in a small amount of space
        /// </summary>
        /// <param name="value">The original string</param>
        /// <param name="targetType">Output type - Must be string</param>
        /// <param name="parameter">Not used</param>
        /// <param name="culture">Caller culture</param>
        /// <returns>String with line breaks removed, string.Empty if value is not of type string</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strVal = value as string;

            if (strVal == null)
            {
                return string.Empty;
            }

            // first trim any leading/trailing space the easy way
            strVal = strVal.Trim();

            // before hitting the regex, artificially truncate any insanely long string.  the whole point of the converter is to fit it on a single small line
            if (strVal.Length > ArtificialLimit)
            {
                strVal = strVal.Substring(0, ArtificialLimit);
            }

            // An alternative Regex string would be to use "\s+" which would catch any one or more whitespace, however, this would replace single spaces with spaces,
            // so it would do work when it wasn't necessary.
            return singleLineRegex.Replace(strVal, " ");
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>Do Nothing</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debug.Fail("Not implemented");
            return Binding.DoNothing;
        }
    }

}
