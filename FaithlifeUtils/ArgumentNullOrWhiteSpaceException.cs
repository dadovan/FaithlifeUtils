using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace FaithlifeUtils;

/// <summary>
/// An exception thrown if the argument is null or empty and that state is not allowed in the current context
/// </summary>
[Serializable]
public class ArgumentNullOrWhiteSpaceException : ArgumentException
{
    /// <summary>
    /// Initialized a new instance of the <see cref="ArgumentNullOrWhiteSpaceException"/> class
    /// </summary>
    /// <param name="paramName">The name of the argument which failed the test</param>
    /// <param name="message">Additional error details</param>
    public ArgumentNullOrWhiteSpaceException(string paramName, string message) : base(paramName, message) { }

    /// <summary>
    /// Initialized a new instance of the <see cref="ArgumentNullOrWhiteSpaceException"/> class from serialized data
    /// </summary>
    /// <param name="info">The object that holds the serialized object data</param>
    /// <param name="context">An object that describes the source or destination of the serialized data</param>
    protected ArgumentNullOrWhiteSpaceException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    /// <summary>
    /// Throws an <see cref="ArgumentNullOrWhiteSpaceException"/> if <see cref="argument"/> is null or whitespace
    /// </summary>
    /// <param name="argument">The value of the argument to test</param>
    /// <param name="paramName">The name of the argument to test</param>
    /// <exception cref="ArgumentNullOrWhiteSpaceException">Thrown if <see cref="argument"/> is null or whitespace</exception>
    public static void ThrowIfNullOrWhiteSpace(string? argument, [CallerArgumentExpression("argument")] string? paramName = default)
    {
        if (String.IsNullOrWhiteSpace(argument))
            throw new ArgumentNullOrWhiteSpaceException(paramName ?? String.Empty, "Value cannot be null or empty.");
    }
}
