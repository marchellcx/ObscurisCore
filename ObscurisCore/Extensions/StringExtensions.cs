using LabExtended.Extensions;
using NiveraAPI.Utilities;
using NorthwoodLib.Pools;

using ObscurisCore.Utilities;

using UnityEngine;

namespace ObscurisCore.Extensions
{
    /// <summary>
    /// String extensions.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Gets a dictionary that maps emoji names to their corresponding Unicode characters.
        /// </summary>
        public static Dictionary<string, string> Emojis { get; } = new()
        {
            { "$EmojiChart", "📊" }
        };

        /// <summary>
        /// Attempts to parse a delimited string into an array of enum values of a specified type.
        /// </summary>
        /// <typeparam name="T">The enum type of elements in the resulting array. Must be a value type and an enumeration type.</typeparam>
        /// <param name="str">The input string containing delimited values to parse. If the string is null or empty, the method returns <see langword="false"/>.</param>
        /// <param name="array">When this method returns, contains the array of parsed enum values if parsing succeeds; otherwise, is set to <see langword="null"/>.</param>
        /// <returns>
        /// <see langword="true"/> if all parts of the input string are successfully parsed into enum values and the array is populated; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryParseEnumArray<T>(this string str, out T[] array) where T : struct, Enum
            => TryParseArray<T>(str, Enum.TryParse, out array);

        /// <summary>
        /// Attempts to parse a delimited string into an array of values of a specified type using a custom parsing function.
        /// </summary>
        /// <typeparam name="T">The type of elements in the resulting array. Must be a value type or an enumeration type.</typeparam>
        /// <param name="str">The input string containing delimited values to parse. If the string is null or empty, the method returns <see langword="false"/>.</param>
        /// <param name="tryParseDelegate">A delegate function that attempts to parse a string into a value of type <typeparamref name="T"/>.</param>
        /// <param name="array">When this method returns, contains the array of parsed values if parsing succeeds; otherwise, is set to <see langword="null"/>.</param>
        /// <returns>
        /// <see langword="true"/> if all parts of the input string are successfully parsed and the array is populated; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryParseArray<T>(this string str, TryParseDelegate<T> tryParseDelegate, out T[] array)
        {
            if (string.IsNullOrEmpty(str))
            {
                array = null!;
                return false;
            }

            var parts = str.SplitOutsideQuotes(',');
            
            array = new T[parts.Length];

            for (var i = 0; i < parts.Length; i++)
            {
                if (!tryParseDelegate(parts[i], out var result))
                    return false;

                array[i] = result;
            }

            return true;
        }

        /// <summary>
        /// Splits the input string into substrings based on the specified separator character, allowing the separator
        /// to be escaped with a backslash ('\').
        /// </summary>
        /// <remarks>A backslash ('\') escapes the following character, allowing separators and
        /// backslashes to be included in substrings. Consecutive backslashes are treated as escaping each other.
        /// Trailing escape characters are preserved as literal backslashes.</remarks>
        /// <param name="source">The input string to split. If <paramref name="source"/> is <see langword="null"/>, an empty array is
        /// returned.</param>
        /// <param name="separator">The character used to separate substrings. To include the separator as a literal in a substring, prefix it
        /// with a backslash ('\').</param>
        /// <returns>An array of substrings resulting from splitting the input string. The array will contain at least one
        /// element, even if the input is empty.</returns>
        public static string[] SplitEscaped(this string source, char separator)
        {
            if (source == null)
                return Array.Empty<string>();

            var result = ListPool<string>.Shared.Rent();
            var currentItem = StringBuilderPool.Shared.Rent();

            var escapeFlag = false;

            foreach (var currentChar in source)
            {
                if (escapeFlag)
                {
                    currentItem.Append(currentChar);
                    escapeFlag = false;
                    continue;
                }

                if (currentChar == '\\')
                {
                    escapeFlag = true;
                    continue;
                }

                if (currentChar == separator)
                {
                    result.Add(currentItem.ToString());
                    currentItem.Clear();
                    continue;
                }

                currentItem.Append(currentChar);
            }

            if (escapeFlag)
                currentItem.Append('\\');

            result.Add(currentItem.ToString());

            StringBuilderPool.Shared.Return(currentItem);
            return ListPool<string>.Shared.ToArrayReturn(result);
        }

        /// <summary>
        /// Splits the input string into substrings using the specified separator character, ignoring separators that
        /// appear within double-quoted sections.
        /// </summary>
        /// <remarks>Double quotes are used to define quoted sections; separators inside quotes are
        /// ignored. Escape characters (\) are handled according to <paramref name="preserveEscapeCharInQuotes"/>. This
        /// method does not support nested or escaped quotes.</remarks>
        /// <param name="source">The string to split. If <paramref name="source"/> is <see langword="null"/>, an empty array is returned.</param>
        /// <param name="separator">The character used to separate substrings outside of quoted sections.</param>
        /// <param name="trimSplits">Indicates whether to trim whitespace from each resulting substring. The default is <see langword="true"/>.</param>
        /// <param name="ignoreEmptyResults">Indicates whether to exclude empty substrings from the result. The default is <see langword="true"/>.</param>
        /// <param name="preserveEscapeCharInQuotes">Indicates whether to preserve the escape character (\) when it appears inside quoted sections. The default
        /// is <see langword="true"/>.</param>
        /// <returns>An array of substrings split from the input string. Substrings within double quotes are not split, and empty
        /// results are excluded if <paramref name="ignoreEmptyResults"/> is <see langword="true"/>.</returns>
        public static string[] SplitOutsideQuotes(this string source, char separator, bool trimSplits = true, bool ignoreEmptyResults = true, bool preserveEscapeCharInQuotes = true)
        {
            if (source == null)
                return Array.Empty<string>();

            var result = ListPool<string>.Shared.Rent();
            var currentItem = StringBuilderPool.Shared.Rent();

            var escapeFlag = false;
            var quotesOpen = false;

            foreach (var currentChar in source)
            {
                if (escapeFlag)
                {
                    currentItem.Append(currentChar);
                    escapeFlag = false;

                    continue;
                }

                if (currentChar == separator && !quotesOpen)
                {
                    var currentItemString = trimSplits
                        ? currentItem.ToString().Trim()
                        : currentItem.ToString();
                    
                    currentItem.Clear();

                    if (string.IsNullOrEmpty(currentItemString) && ignoreEmptyResults)
                        continue;

                    result.Add(currentItemString);
                    continue;
                }

                switch (currentChar)
                {
                    default:
                        currentItem.Append(currentChar);
                        break;

                    case '\\':
                        if (quotesOpen && preserveEscapeCharInQuotes)
                            currentItem.Append(currentChar);

                        escapeFlag = true;
                        break;

                    case '"':
                        // currentItem.Append(currentChar);
                        quotesOpen = !quotesOpen;
                        break;
                }
            }

            if (escapeFlag)
                currentItem.Append("\\");

            var lastCurrentItemString = trimSplits
                ? currentItem.ToString().Trim()
                : currentItem.ToString();

            if (!(string.IsNullOrEmpty(lastCurrentItemString) && ignoreEmptyResults))
                result.Add(lastCurrentItemString);

            StringBuilderPool.Shared.Return(currentItem);
            return ListPool<string>.Shared.ToArrayReturn(result);
        }
        
                /// <summary>
        /// Splits the input string into substrings using the specified separator character, ignoring separators that
        /// appear within double-quoted sections.
        /// </summary>
        /// <remarks>Double quotes are used to define quoted sections; separators inside quotes are
        /// ignored. Escape characters (\) are handled according to <paramref name="preserveEscapeCharInQuotes"/>. This
        /// method does not support nested or escaped quotes.</remarks>
        /// <param name="source">The string to split. If <paramref name="source"/> is <see langword="null"/>, an empty array is returned.</param>
        /// <param name="separator">The character used to separate substrings outside of quoted sections.</param>
        /// <param name="trimSplits">Indicates whether to trim whitespace from each resulting substring. The default is <see langword="true"/>.</param>
        /// <param name="ignoreEmptyResults">Indicates whether to exclude empty substrings from the result. The default is <see langword="true"/>.</param>
        /// <param name="preserveEscapeCharInQuotes">Indicates whether to preserve the escape character (\) when it appears inside quoted sections. The default
        /// is <see langword="true"/>.</param>
        /// <returns>An array of substrings split from the input string. Substrings within double quotes are not split, and empty
        /// results are excluded if <paramref name="ignoreEmptyResults"/> is <see langword="true"/>.</returns>
        public static string[] SplitOutsideQuotes(this string source, char separator, char quote, bool trimSplits = true, bool ignoreEmptyResults = true, bool preserveEscapeCharInQuotes = true)
        {
            if (source == null)
                return Array.Empty<string>();

            var result = ListPool<string>.Shared.Rent();
            var currentItem = StringBuilderPool.Shared.Rent();

            var escapeFlag = false;
            var quotesOpen = false;

            foreach (var currentChar in source)
            {
                if (escapeFlag)
                {
                    currentItem.Append(currentChar);
                    escapeFlag = false;

                    continue;
                }

                if (currentChar == separator && !quotesOpen)
                {
                    var currentItemString = trimSplits
                        ? currentItem.ToString().Trim()
                        : currentItem.ToString();
                    
                    currentItem.Clear();

                    if (string.IsNullOrEmpty(currentItemString) && ignoreEmptyResults)
                        continue;

                    result.Add(currentItemString);
                    continue;
                }

                if (currentChar == '\\')
                {
                    if (quotesOpen && preserveEscapeCharInQuotes)
                        currentItem.Append(currentChar);

                    escapeFlag = true;
                }
                else if (currentChar == quote)
                {
                    quotesOpen = !quotesOpen;
                }
                else
                {
                    currentItem.Append(currentChar);
                }
            }

            if (escapeFlag)
                currentItem.Append("\\");

            var lastCurrentItemString = trimSplits
                ? currentItem.ToString().Trim()
                : currentItem.ToString();

            if (!(string.IsNullOrEmpty(lastCurrentItemString) && ignoreEmptyResults))
                result.Add(lastCurrentItemString);

            StringBuilderPool.Shared.Return(currentItem);
            return ListPool<string>.Shared.ToArrayReturn(result);
        }

        /// <summary>
        /// Splits the input string into substrings using the specified separator characters, ignoring separators that
        /// appear within quoted sections.
        /// </summary>
        /// <remarks>Quoted sections are defined by double-quote (") characters. Escape characters are
        /// handled according to the value of <paramref name="preserveEscapeCharInQuotes"/>. This method is useful for
        /// parsing delimited data formats where quoted values may contain separator characters.</remarks>
        /// <param name="source">The input string to split. If <paramref name="source"/> is <see langword="null"/>, an empty array is
        /// returned.</param>
        /// <param name="separators">An array of characters to use as delimiters for splitting the string. Separators inside quoted sections are
        /// ignored.</param>
        /// <param name="trimSplits">Indicates whether to trim whitespace from each resulting substring. The default value is <see
        /// langword="true"/>.</param>
        /// <param name="ignoreEmptyResults">Indicates whether to exclude empty substrings from the result. The default value is <see langword="true"/>.</param>
        /// <param name="preserveEscapeCharInQuotes">Indicates whether to preserve escape characters (\) within quoted sections. The default value is <see
        /// langword="true"/>.</param>
        /// <returns>An array of substrings resulting from splitting the input string outside of quoted sections. The array is
        /// empty if <paramref name="source"/> is <see langword="null"/> or if no substrings are found.</returns>
        public static string[] SplitOutsideQuotes(this string source, char[] separators, bool trimSplits = true, bool ignoreEmptyResults = true, 
            bool preserveEscapeCharInQuotes = true)
        {
            if (source == null)
                return Array.Empty<string>();

            var result = ListPool<string>.Shared.Rent();
            var currentItem = StringBuilderPool.Shared.Rent();

            var escapeFlag = false;
            var quotesOpen = false;

            foreach (var currentChar in source)
            {
                if (escapeFlag)
                {
                    currentItem.Append(currentChar);
                    escapeFlag = false;

                    continue;
                }

                if (separators.Contains(currentChar) && !quotesOpen)
                {
                    var currentItemString = trimSplits
                        ? currentItem.ToString().Trim()
                        : currentItem.ToString();
                    currentItem.Clear();

                    if (string.IsNullOrEmpty(currentItemString) && ignoreEmptyResults)
                        continue;

                    result.Add(currentItemString);
                    continue;
                }

                switch (currentChar)
                {
                    default:
                        currentItem.Append(currentChar);
                        break;

                    case '\\':
                        if (quotesOpen && preserveEscapeCharInQuotes)
                            currentItem.Append(currentChar);

                        escapeFlag = true;
                        break;

                    case '"':
                        currentItem.Append(currentChar);
                        quotesOpen = !quotesOpen;
                        break;
                }
            }

            if (escapeFlag) 
                currentItem.Append("\\");

            var lastCurrentItemString = trimSplits 
                ? currentItem.ToString().Trim() 
                : currentItem.ToString();

            if (!(string.IsNullOrEmpty(lastCurrentItemString) && ignoreEmptyResults))
                result.Add(lastCurrentItemString);

            StringBuilderPool.Shared.Return(currentItem);
            return ListPool<string>.Shared.ToArrayReturn(result);
        }

        /// <summary>
        /// Attempts to parse a string representation of a 2D vector into a <see cref="Vector2"/> instance.
        /// </summary>
        /// <remarks>If the input string is null, empty, or contains invalid components, the corresponding
        /// values in <paramref name="result"/> are set to zero. Only the first two comma-separated values are
        /// considered as the X and Y components, respectively.</remarks>
        /// <param name="str">The string containing the vector components, separated by a comma (e.g., "1.0,2.0").</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="Vector2"/> value if parsing succeeded; otherwise,
        /// contains <see cref="Vector2.zero"/>.</param>
        /// <returns><see langword="true"/> if at least one component is successfully parsed; otherwise, <see langword="false"/>.</returns>
        public static bool TryParseVector2(this string str, out Vector2 result)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                result = Vector2.zero;
                return false;
            }

            var parts = str.Split(',');

            var x = 0f;
            var y = 0f;
            var z = 0f;

            var successX = parts.Length > 0 && float.TryParse(parts[0], out x);
            var successY = parts.Length > 1 && float.TryParse(parts[1], out y);

            result = new(
                successX ? x : 0f,
                successY ? y : 0f);

            return successX || successY;
        }

        /// <summary>
        /// Attempts to parse a string containing three comma-separated values into a <see cref="Vector3"/> instance.
        /// </summary>
        /// <remarks>Missing or invalid components are set to 0. Components are parsed in order: X, Y,
        /// then Z. Extra components beyond the third are ignored. Leading and trailing whitespace in each component is
        /// allowed.</remarks>
        /// <param name="str">The input string representing the vector components, with values separated by commas (e.g., "1.0,2.0,3.0").</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="Vector3"/> value if parsing succeeded; otherwise,
        /// contains <see cref="Vector3.zero"/>.</param>
        /// <returns>true if at least one component is successfully parsed; otherwise, false.</returns>
        public static bool TryParseVector3(this string str, out Vector3 result)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                result = Vector3.zero;
                return false;
            }

            var parts = str.Split(',');

            var x = 0f;
            var y = 0f;
            var z = 0f;

            var successX = parts.Length > 0 && float.TryParse(parts[0], out x);
            var successY = parts.Length > 1 && float.TryParse(parts[1], out y);
            var successZ = parts.Length > 2 && float.TryParse(parts[2], out z);

            result = new(
                successX ? x : 0f,
                successY ? y : 0f,
                successZ ? z : 0f);

            return successX || successY || successZ;
        }

        /// <summary>
        /// Attempts to parse a string representation of a quaternion into a <see cref="Quaternion"/> value.
        /// </summary>
        /// <remarks>Missing or invalid components are set to 0. If the input string is null, empty, or
        /// whitespace, the result is set to <see cref="Quaternion.identity"/> and the method returns false.</remarks>
        /// <param name="str">The string containing the quaternion components, separated by commas. Components are expected in the order:
        /// x, y, z, w.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="Quaternion"/> value if parsing succeeded;
        /// otherwise, contains <see cref="Quaternion.identity"/>.</param>
        /// <returns>true if at least one component is successfully parsed; otherwise, false.</returns>
        public static bool TryParseQuaternion(this string str, out Quaternion result)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                result = Quaternion.identity;
                return false;
            }

            var parts = str.Split(',');

            var x = 0f;
            var y = 0f;
            var z = 0f;
            var w = 0f;

            var successX = parts.Length > 0 && float.TryParse(parts[0], out x);
            var successY = parts.Length > 1 && float.TryParse(parts[1], out y);
            var successZ = parts.Length > 2 && float.TryParse(parts[2], out z);
            var successW = parts.Length > 3 && float.TryParse(parts[3], out w);

            result = new(
                successX ? x : 0f,
                successY ? y : 0f,
                successZ ? z : 0f,
                successW ? w : 0f);

            return successX || successY || successZ || successW;
        }

        /// <summary>
        /// Replaces all emoji codes in the string with their corresponding Unicode emoji characters.
        /// </summary>
        /// <remarks>Only emoji codes present in the predefined mapping will be replaced. Unrecognized
        /// codes are left unchanged.</remarks>
        /// <param name="str">The input string in which emoji codes will be replaced. Cannot be null.</param>
        /// <returns>A new string with all recognized emoji codes replaced by their Unicode emoji equivalents. If no emoji codes
        /// are found, the original string is returned.</returns>
        public static string ReplaceEmojis(this string str)
        {
            foreach (var pair in Emojis)
                str = str.Replace(pair.Key, pair.Value);

            return str;
        }
    }
}