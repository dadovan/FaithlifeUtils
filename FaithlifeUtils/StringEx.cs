using System;

namespace FaithlifeUtils;

/// <summary>
/// Holds extension methods for the <see cref="string" /> class
/// </summary>
public static class StringEx
{
    /// <summary>
    /// Case-insensitive <see cref="string" /> equality comparison (<see cref="StringComparison.InvariantCultureIgnoreCase" />)
    /// </summary>
    /// <param name="value1">The first <see cref="String" /> being compared</param>
    /// <param name="value2">The second <see cref="String" /> being compared</param>
    /// <returns>True if the <see cref="String" />s are equal (ignoring case)</returns>
    public static bool EqualsI(this string value1, string value2)
    {
        return value1.Equals(value2, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Case-insensitive test whether <see cref="value" /> exists in the <see cref="String" /> (<see cref="StringComparison.InvariantCultureIgnoreCase" />)
    /// </summary>
    /// <param name="input">The <see cref="String" /> being examined</param>
    /// <param name="value">The <see cref="String" /> to find</param>
    /// <returns>True if <see cref="value" /> is present in the <see cref="String" /></returns>
    public static bool ContainsI(this string input, string value)
    {
        return input.Contains(value, StringComparison.InvariantCultureIgnoreCase);
    }
}