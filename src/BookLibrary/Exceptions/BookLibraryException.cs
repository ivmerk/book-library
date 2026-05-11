namespace BookLibrary.Exceptions;

/// <summary>
/// Root exception type for all errors raised by the <c>BookLibrary</c> assembly.
/// </summary>
/// <remarks>
/// Consumers can catch this single type to handle any library-level failure
/// (corrupted storage, I/O error, invalid file format, etc.) without depending
/// on lower-level exceptions like <see cref="System.IO.IOException"/> or
/// <see cref="System.Xml.XmlException"/>. The original cause is always preserved
/// via <see cref="Exception.InnerException"/>.
/// </remarks>
public class BookLibraryException : Exception
{
    /// <summary>Creates a new instance with a descriptive message.</summary>
    /// <param name="message">Human-readable description of the failure.</param>
    public BookLibraryException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a new instance wrapping an underlying cause.</summary>
    /// <param name="message">Human-readable description of the failure.</param>
    /// <param name="innerException">The underlying exception that triggered this failure.</param>
    public BookLibraryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

